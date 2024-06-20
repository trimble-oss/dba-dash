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
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DocumentFormat.OpenXml.VariantTypes;
using System.Windows.Interop;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Amazon.Runtime.Internal.Transform;

namespace DBADashGUI.Messaging
{
    internal static class CollectionMessaging
    {
        private const int RecentTriggerThresholdSeconds = 30;
        private static int PendingRequests = 0;
        private const int PendingRequestsThreshold = 10;
        private static readonly Dictionary<(string instanceName, string type), DateTime> LastTriggeredTimes = new();
        private static readonly object LockObject = new();
        private const int CollectionDialogLifetime = 600;

        private static List<CollectionType> RecentlyTriggeredExcludedList = new()
        {
            CollectionType.RunningQueries, CollectionType.AvailabilityGroups, CollectionType.AvailabilityReplicas,
            CollectionType.DatabasesHADR
        };

        public static async Task TriggerCollection(string connectionID, CollectionType type, int collectAgentID, int importAgentID, ISetStatus control)
        {
            await TriggerCollection(connectionID, new List<string>() { Enum.GetName(type) }, collectAgentID, importAgentID, control);
        }

        public static async Task TriggerCollection(string connectionID, string type, int collectAgentID, int importAgentID, ISetStatus control)
        {
            await TriggerCollection(connectionID, new List<string>() { type }, collectAgentID, importAgentID, control);
        }

        public static async Task TriggerCollection(string connectionID, List<string> types, int collectAgentID, int importAgentID,
            ISetStatus control)
        {
            if (PendingRequests >= PendingRequestsThreshold)
            {
                control.SetStatus("Too many pending requests", null, DashColors.Fail);
                return;
            }

            foreach (var type in types)
            {
                if (IsRecentlyTriggered(connectionID, type) && !RecentlyTriggeredExcludedList.Exists(ct => Enum.GetName(ct) == type))
                {
                    control.SetStatus($"Collection {type} already triggered recently for {connectionID}", null, DashColors.Fail);
                    return;
                }
                else if (type is "QueryText" or "QueryPlans")
                {
                    control.SetStatus($"Collection {type} is collected as part of RunningQueries collection and can't be triggered manually", null, DashColors.Warning);
                    return;
                }
            }

            var typesString = string.Join(", ", types.Select(s => s.ToString()));
            var messageBase = $"{typesString} collection for {connectionID}: ";
            var collectAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, collectAgentID);
            var importAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, importAgentID);
            var x = new CollectionMessage(types, connectionID) { CollectAgent = collectAgent, ImportAgent = importAgent };

            var payload = x.Serialize();
            var messageGroup = Guid.NewGuid();
            await MessageProcessing.SendMessageToService(payload, importAgentID, messageGroup, Common.ConnectionString, CollectionDialogLifetime);
            control.SetStatus(messageBase + "SENT", "", DashColors.Information);
            foreach (var type in types)
            {
                UpdateLastTriggeredTime(connectionID, type);
            }
            IncrementPendingRequests();
            await Task.Run(() => ReceiveReply(messageGroup, messageBase, control));
        }

        public static async Task TriggerCollection(int InstanceID, List<CollectionType> types, ISetStatus control)
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
            await TriggerCollection(connectionID, types.Select(Enum.GetName).ToList(), collectAgentID, importAgentID, control);
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

        public static async Task ReceiveReply(Guid group, string messageBase, ISetStatus control)
        {
            ArgumentNullException.ThrowIfNull(messageBase);
            ArgumentNullException.ThrowIfNull(control);

            try
            {
                var completed = false;
                while (!completed)
                {
                    await using var cn = new SqlConnection(Common.ConnectionString);
                    await using var cmd = new SqlCommand("Messaging.ReceiveReplyFromServiceToGUI", cn)
                    { CommandType = CommandType.StoredProcedure, CommandTimeout = 0 };
                    cmd.Parameters.AddWithValue("@ConversationGroupID", group);
                    await cn.OpenAsync();
                    await using var rdr = await cmd.ExecuteReaderAsync();
                    if (!rdr.Read()) continue;
                    var handle = (Guid)rdr["conversation_handle"];
                    var type = (string)rdr["message_type_name"];

                    var reply = rdr["message_body"] == DBNull.Value ? null : (byte[])rdr["message_body"];

                    completed = await ProcessMessageAsync(type, reply, handle, messageBase, control);
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

        private static async Task<bool> ProcessMessageAsync(string type, byte[] reply, Guid handle, string messageBase, ISetStatus control)
        {
            switch (type)
            {
                case MessageProcessing.EndDialogMessageType:
                    await MessageProcessing.EndConversation(handle, Common.ConnectionString);
                    return true;

                case MessageProcessing.ErrorMessageType:
                    await ProcessErrorMessageAsync(reply, handle, messageBase, control);
                    return true;

                case MessageProcessing.ReplyMessageType:
                    return await ProcessReplyMessageAsync(reply, messageBase, control);

                default:
                    control.SetStatus($"{messageBase}Unknown message type: {type}", null, DashColors.Fail);
                    return false;
            }
        }

        private static async Task ProcessErrorMessageAsync(byte[] reply, Guid handle, string messageBase, ISetStatus control)
        {
            var message = reply == null ? "{null}" : Encoding.Unicode.GetString(reply);
            control.SetStatus($"{messageBase}{message}", message, DashColors.Fail);
            await MessageProcessing.EndConversation(handle, Common.ConnectionString);
        }

        private static async Task<bool> ProcessReplyMessageAsync(byte[] reply, string messageBase, ISetStatus control)
        {
            if (reply == null || reply.Length == 0)
            {
                control.SetStatus($"{messageBase}NULL reply", "Reply was NULL", DashColors.Fail);
                return false;
            }

            var msg = ResponseMessage.Deserialize(reply);
            var color = msg.Type switch
            {
                ResponseMessage.ResponseTypes.Success => DashColors.Success,
                ResponseMessage.ResponseTypes.Failure => DashColors.Fail,
                ResponseMessage.ResponseTypes.Progress => DashColors.Information,
                _ => Color.Black
            };

            control.SetStatus($"{messageBase}{msg.Message}", msg.Exception?.ToString(), color);

            if (msg.Type == ResponseMessage.ResponseTypes.Success)
            {
                await Task.Delay(100);
                control.RefreshData();
            }

            return false;
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