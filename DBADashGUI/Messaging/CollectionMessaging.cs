using DBADash.Messaging;
using DBADash;
using DBADashGUI.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Serilog;

namespace DBADashGUI.Messaging
{
    internal static class CollectionMessaging
    {
        private const int RecentTriggerThresholdSeconds = 30;
        private static int PendingRequests;
        private const int PendingRequestsThreshold = 10;
        private static readonly Dictionary<(string instanceName, string type), DateTime> LastTriggeredTimes = new();
        private static readonly object LockObject = new();
        private const int CollectionDialogLifetime = 600;

        private static readonly List<CollectionType> RecentlyTriggeredExcludedList = new()
        {
            CollectionType.RunningQueries, CollectionType.AvailabilityGroups, CollectionType.AvailabilityReplicas,
            CollectionType.DatabasesHADR, CollectionType.RunningJobs
        };

        public static async Task TriggerCollection(string connectionID, CollectionType type, int collectAgentID, int importAgentID, ISetStatus control)
        {
            await TriggerCollection(connectionID, new List<string>() { Enum.GetName(type) }, collectAgentID, importAgentID, control);
        }

        public static async Task TriggerCollection(string connectionID, List<CollectionType> types, int collectAgentID, int importAgentID, ISetStatus control)
        {
            await TriggerCollection(connectionID, types.Select(Enum.GetName).ToList(), collectAgentID, importAgentID, control);
        }

        public static async Task TriggerCollection(string connectionID, string type, int collectAgentID, int importAgentID, ISetStatus control)
        {
            await TriggerCollection(connectionID, new List<string>() { type }, collectAgentID, importAgentID, control);
        }

        public static async Task TriggerCollection(string connectionID, List<string> types, int collectAgentID, int importAgentID,
            ISetStatus control, string db = null)
        {
            if (PendingRequests >= PendingRequestsThreshold)
            {
                control.SetStatus("Too many pending requests", null, DashColors.Fail);
                return;
            }
            var key = db == null ? connectionID : connectionID + "\\" + db;
            foreach (var type in types)
            {
                if (IsRecentlyTriggered(key, type) && !RecentlyTriggeredExcludedList.Exists(ct => Enum.GetName(ct) == type))
                {
                    control.SetStatus($"Collection {type} already triggered recently for {key}", null, DashColors.Fail);
                    return;
                }
                else if (type is "QueryText" or "QueryPlans")
                {
                    control.SetStatus($"Collection {type} is collected as part of RunningQueries collection and can't be triggered manually", null, DashColors.Warning);
                    return;
                }
            }

            var typesString = string.Join(", ", types.Select(s => s.ToString()));
            var messageBase = $"{typesString} collection for {key}: ";

            var collectAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, collectAgentID);
            var importAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, importAgentID);
            var message = new CollectionMessage(types, connectionID) { CollectAgent = collectAgent, ImportAgent = importAgent, DatabaseName = db, Lifetime = CollectionDialogLifetime };

            IncrementPendingRequests();
            await Task.Run(() => SendAndReceiveReply(message, importAgentID, key, messageBase, control));
        }

        public static async Task TriggerCollection(int InstanceID, List<CollectionType> types, ISetStatus control, string db = null)
        {
            var row = CommonData.Instances.AsEnumerable().FirstOrDefault(i => (int)i["InstanceID"] == InstanceID);
            if (row == null) return;
            var connectionID = (string)row["ConnectionID"];
            var MessagingEnabled = (bool)row["MessagingEnabled"];
            var importAgentID = (int)row["ImportAgentID"];
            var collectAgentID = (int)row["CollectAgentID"];
            if (!MessagingEnabled)
            {
                MessageBox.Show("Messaging is not enabled for this instance", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await TriggerCollection(connectionID, types.Select(Enum.GetName).ToList(), collectAgentID, importAgentID, control, db);
        }

        public static async Task TriggerCollection(int InstanceID, CollectionType type, ISetStatus control)
        {
            await TriggerCollection(InstanceID, new List<CollectionType>() { type }, control);
        }

        public static bool IsMessagingEnabled(int InstanceID)
        {
            var row = CommonData.Instances.AsEnumerable().FirstOrDefault(i => (int)i["InstanceID"] == InstanceID);
            return row != null && (bool)row["MessagingEnabled"];
        }

        private static async Task SendAndReceiveReply(CollectionMessage message, int importAgentID, string key, string messageBase, ISetStatus control)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(messageBase);
            ArgumentNullException.ThrowIfNull(control);

            try
            {
                var group = Guid.NewGuid();
                message.Id = group; // Track the message by its conversation group so it can be cancelled and logged correctly.
                await MessageProcessing.SendMessageToService(message.Serialize(), importAgentID, group, Common.ConnectionString, message.Lifetime);

                // Only record the trigger once the message has actually been sent - a failed send shouldn't
                // block retries for RecentTriggerThresholdSeconds when nothing was collected.
                foreach (var type in message.CollectionTypes)
                {
                    UpdateLastTriggeredTime(key, type);
                }
                control.SetStatus(messageBase + "SENT", "", DashColors.Information);

                var completed = false;
                while (!completed)
                {
                    var reply = await ReceiveReply(group, message.Lifetime * 1000);
                    switch (reply.Type)
                    {
                        case ResponseMessage.ResponseTypes.Failure:
                            control.SetStatus(messageBase + reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                            completed = true;
                            break;

                        case ResponseMessage.ResponseTypes.Warning:
                            // The collection(s) aren't scheduled for this instance so nothing was run.  Offer to
                            // run them anyway; if confirmed, re-send the same message forced and keep listening on
                            // the new conversation rather than treating the warning as the end of the operation.
                            if (reply.DisabledCollections is { Count: > 0 } && !message.IgnoreDisabledSchedule &&
                                PromptRunUnscheduled(control, reply.DisabledCollections))
                            {
                                // The service ends the conversation after the warning.  Drain its end-of-dialog
                                // message so it isn't orphaned in the broker queue, but fire-and-forget so we
                                // don't delay the re-send - it runs on the old group and handles its own errors,
                                // so it won't interfere with replies received on the new conversation below.
                                _ = DrainConversationAsync(group, message.Lifetime);
                                message.IgnoreDisabledSchedule = true;
                                group = Guid.NewGuid();
                                message.Id = group; // New conversation - retrack under the new group for cancellation/logging.
                                await MessageProcessing.SendMessageToService(message.Serialize(), importAgentID, group, Common.ConnectionString, message.Lifetime);
                                control.SetStatus(messageBase + "running unscheduled collection(s)...", null, DashColors.Information);
                                break; // completed stays false - continue receiving on the re-sent conversation
                            }
                            // Show the warning but keep listening (completed stays false) so the service's
                            // end-of-dialog is received and the conversation closed rather than orphaning it.
                            control.SetStatus(messageBase + reply.Message, null, DashColors.Warning);
                            break;

                        case ResponseMessage.ResponseTypes.EndConversation:
                            completed = true;
                            break;

                        case ResponseMessage.ResponseTypes.Progress:
                            control.SetStatus(messageBase + reply.Message, null, DashColors.Information);
                            break;

                        case ResponseMessage.ResponseTypes.Success:
                            control.SetStatus(messageBase + reply.Message, null, DashColors.Success);
                            control.RefreshData();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                control.SetStatus(messageBase + "Error processing replies: " + ex.Message, ex.ToString(),
                    DashColors.Fail);
            }
            finally
            {
                DecrementPendingRequests();
            }
        }

        /// <summary>
        /// Prompts the user (on the UI thread) to confirm running collection(s) that aren't scheduled for the
        /// target instance.  Returns true if the user chose to run them anyway.
        /// </summary>
        private static bool PromptRunUnscheduled(ISetStatus control, List<string> disabledCollections)
        {
            bool Confirm()
            {
                var list = string.Join(", ", disabledCollections);
                var text = disabledCollections.Count == 1
                    ? $"The {list} collection is not scheduled for this instance, so it was not run.\n\nRun it anyway?"
                    : $"The following collections are not scheduled for this instance, so they were not run:\n\n{list}\n\nRun them anyway?";
                return MessageBox.Show(text, "Collection Not Scheduled", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
            }

            if (control is Control c && c.InvokeRequired)
            {
                return (bool)c.Invoke((Func<bool>)Confirm);
            }
            return Confirm();
        }

        /// <summary>
        /// Receives any remaining messages on a conversation the service has finished with (e.g. after a
        /// terminal Warning reply) up to the end-of-dialog, which <see cref="ReceiveReply"/> acknowledges by
        /// ending the conversation.  Stops the end-of-dialog broker message being left orphaned in the queue.
        /// </summary>
        private static async Task DrainConversationAsync(Guid group, int lifetimeSeconds)
        {
            try
            {
                while (true)
                {
                    var reply = await ReceiveReply(group, lifetimeSeconds * 1000);
                    if (reply.Type is ResponseMessage.ResponseTypes.EndConversation
                        or ResponseMessage.ResponseTypes.Failure)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error draining conversation {group} after warning", group);
            }
        }

        private static async Task<BrokerResponse> ReceiveReplyFromServiceBroker(Guid group, int timeout)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("Messaging.ReceiveReplyFromServiceToGUI", cn)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = 0 };
            cmd.Parameters.AddWithValue("@ConversationGroupID", group);
            cmd.Parameters.AddWithValue("@Timeout", timeout);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!rdr.Read()) throw new Exception("No results");
            return new BrokerResponse()
            {
                Handle = (Guid)rdr["conversation_handle"],
                Type = (string)rdr["message_type_name"],
                Payload = rdr["message_body"] == DBNull.Value ? null : (byte[])rdr["message_body"]
            };
        }

        public static async Task<ResponseMessage> ReceiveReply(Guid group, int timeout)
        {
            var reply = await ReceiveReplyFromServiceBroker(group, timeout);
            var message = string.Empty;
            switch (reply.Type)
            {
                case MessageProcessing.ErrorMessageType:
                    {
                        try
                        {
                            message = Encoding.Unicode.GetString(reply.Payload ?? Array.Empty<byte>());
                            message = ServiceBrokerXMLErrorToSimpleString(message);
                            await MessageProcessing.EndConversation(reply.Handle, Common.ConnectionString);
                            return new ResponseMessage()
                            { Message = message, Type = ResponseMessage.ResponseTypes.Failure };
                        }
                        catch (Exception ex)
                        {
                            return new ResponseMessage()
                            { Message = message + ex.Message, Type = ResponseMessage.ResponseTypes.Failure };
                        }
                    }
                case MessageProcessing.EndDialogMessageType:
                    try
                    {
                        await MessageProcessing.EndConversation(reply.Handle, Common.ConnectionString);
                        return new ResponseMessage() { Type = ResponseMessage.ResponseTypes.EndConversation };
                    }
                    catch (Exception ex)
                    {
                        return new ResponseMessage() { Message = ex.ToString(), Type = ResponseMessage.ResponseTypes.EndConversation };
                    }

                case MessageProcessing.ReplyMessageType:
                    {
                        var msg = ResponseMessage.Deserialize(reply.Payload);
                        return msg;
                    }
                default:
                    return new ResponseMessage()
                    { Message = $"Unknown message type: {reply.Type}", Type = ResponseMessage.ResponseTypes.Failure };
            }
        }

        public static string ServiceBrokerXMLErrorToSimpleString(string xml)
        {
            try
            {
                var _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                if (xml.StartsWith(_byteOrderMarkUtf8))
                {
                    xml = xml.Remove(0, _byteOrderMarkUtf8.Length);
                }
                var errorElement = XElement.Parse(xml);
                XNamespace ns = "http://schemas.microsoft.com/SQL/ServiceBroker/Error";
                var codeElement = errorElement.Element(ns + "Code");
                var descriptionElement = errorElement.Element(ns + "Description");

                // If we have a code & description, return a formatted string
                if (codeElement != null && descriptionElement != null)
                {
                    // Return the concatenated Code and Description
                    return $"{codeElement.Value} {descriptionElement.Value}";
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error parsing XML error message");
                // If an exception occurs (e.g., invalid XML), return the full text
            }

            // Return the full text if elements are missing or an exception occurred
            return xml;
        }

        private static bool IsRecentlyTriggered(string instanceName, string typeName)
        {
            lock (LockObject)
            {
                // Check if the combination exists in the dictionary
                if (!LastTriggeredTimes.TryGetValue((instanceName, typeName), out DateTime lastTriggeredTime))
                    return false; // Combination not found in the dictionary
                // Calculate the time elapsed since the last trigger
                var elapsed = DateTime.Now - lastTriggeredTime;
                return elapsed.TotalSeconds < RecentTriggerThresholdSeconds;
            }
        }

        private static void UpdateLastTriggeredTime(string instanceName, string type)
        {
            lock (LockObject)
            {
                // Update or add the combination to the dictionary with the current time
                LastTriggeredTimes[(instanceName, type)] = DateTime.Now;
            }
        }

        public static void IncrementPendingRequests()
        {
            Interlocked.Increment(ref PendingRequests);
        }

        public static void DecrementPendingRequests()
        {
            Interlocked.Decrement(ref PendingRequests);
        }
    }
}