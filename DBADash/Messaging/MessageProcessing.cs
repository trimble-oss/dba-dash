using DBADash;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.RuntimeDependencies;
using Azure;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Octokit;
using Polly;
using Serilog;
using DBADash.Messaging;
using Polly.Retry;
using static Azure.Core.HttpHeader;
using static System.Net.WebRequestMethods;
using static System.Net.Mime.MediaTypeNames;

namespace DBADash.Messaging
{
    public class MessageProcessing
    {
        private readonly CollectionConfig Config;
        public const string EndDialogMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog";
        public const string SendMessageType = "//dbadash.com/DBADashService/Send";
        public const string ErrorMessageType = "http://schemas.microsoft.com/SQL/ServiceBroker/Error";
        public const string ReplyMessageType = "//dbadash.com/DBADashService/Reply";
        private readonly DBADashAgent Agent;
        private const int DefaultMessageThreads = 2;
        private int MessageThreads;

        private readonly AsyncRetryPolicy AgentIDRetryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(60),
                (exception, retryCount, calculatedWaitDuration) =>
                {
                    Log.Error(exception,
                        $"Error getting Agent ID.  Retrying in {calculatedWaitDuration.TotalSeconds} seconds. (Retry count: {retryCount})");
                });

        public MessageProcessing(CollectionConfig config)
        {
            Config = config;
            Agent = DBADashAgent.GetCurrent(Config.ServiceName);
            MessageThreads = config.MessageThreads ?? DefaultMessageThreads;
            MessageSemaphore = new SemaphoreSlim(MessageThreads);
        }

        public async Task ScheduleMessaging()
        {
            Log.Information(Config.EnableMessaging ? $"Setting up messaging for destinations. {MessageThreads} threads" : "Disable Messaging for destinations");
            var tasks = Config.AllDestinations
                .Where(dest => dest.Type == DBADashConnection.ConnectionType.SQL)
                .Select(ScheduleMessagingForDestination);
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting up messaging for destinations");
            }
        }

        private async Task ScheduleMessagingForDestination(DBADashConnection d)
        {
            var agentID = 0;
            await AgentIDRetryPolicy.ExecuteAsync(() =>
            {
                agentID = Agent.GetDBADashAgentID(d.ConnectionString);
                return Task.CompletedTask;
            });

            if (agentID == 0)
            {
                Log.Error("Unable to get Agent ID for {Destination}", d.ConnectionForPrint);
                return;
            }
            try
            {
                Log.Information(
                    Config.EnableMessaging
                        ? "Setting up messaging for {Destination}"
                        : "Disable Messaging for {Destination}", d.ConnectionForPrint);

                await SetupMessaging(agentID, d.ConnectionString, Config.EnableMessaging);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting up messaging for {Destination}", d.ConnectionForPrint);
                return;
            }

            if (Config.EnableMessaging)
            {
                // Configure separate thread to process messages for each destination if messaging is enabled
                _ = Task.Run(() => Process(d, agentID));
            }
        }

        private readonly SemaphoreSlim MessageSemaphore;

        public async Task Process(DBADashConnection dest, int agentID)
        {
            while (true)
            {
                try
                {
                    await WaitForCapacity();
                    await using var cn = new SqlConnection(dest.ConnectionString);
                    await cn.OpenAsync();
                    await using var cmd = new SqlCommand("Messaging.ReceiveMessageToService", cn)
                    { CommandType = CommandType.StoredProcedure, CommandTimeout = 0 };
                    cmd.Parameters.AddWithValue("@DBADashAgentID", agentID);
                    await using var rdr = await cmd.ExecuteReaderAsync();
                    if (rdr.Read())
                    {
                        var handle = (Guid)rdr["conversation_handle"];
                        var message = rdr["message_body"] == DBNull.Value ? null : (byte[])rdr["message_body"];
                        var type = (string)rdr["message_type_name"];
                        if (type == SendMessageType)
                        {
                            Log.Information("Received message of type {MessageType} with handle {handle}", type, handle);
                            await SendReplyMessage(handle,
                                (new ResponseMessage()
                                { Type = ResponseMessage.ResponseTypes.Progress, Message = "Message Received" })
                                .Serialize(),
                                dest.ConnectionString);

                            _ = ProcessMessage(dest, handle, type, message);
                        }
                        else
                        {
                            await HandleOtherMessageTypes(handle, type, message, dest.ConnectionString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing messages for {Destination}", dest.ConnectionForPrint);
                }
                await Task.Delay(100);
            }
        }

        private static async Task HandleOtherMessageTypes(Guid handle, string type, byte[] message, string connectionString)
        {
            try
            {
                if (type == ErrorMessageType) // Probably dialog exceeded lifetime
                {
                    var messageText = Encoding.Unicode.GetString(message);
                    Log.Error("Error message received with handle {handle}: {message}", handle, messageText);
                }
                else
                {
                    Log.Error("Unknown message type {type} with message {message}", type, message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling message type {type} with {handle}", type, handle);
            }
            finally
            {
                await EndConversation(handle, connectionString);
            }
        }

        private async Task WaitForCapacity()
        {
            if (MessageSemaphore.CurrentCount > 0) return;
            Log.Warning("Message processing is currently at capacity.  Waiting for a slot to open.");
            while (MessageSemaphore.CurrentCount == 0)
            {
                await Task.Delay(200);
            }
        }

        public async Task ProcessMessage(DBADashConnection dest, Guid handle, string type, byte[] message)
        {
            try
            {
                await MessageSemaphore.WaitAsync(); // Limit the number of messages processed at once

                Log.Information("Processing message of type {MessageType} with handle {handle}", type, handle);
                var msg = MessageBase.Deserialize(message);
                if (msg.IsExpired)
                {
                    Log.Error("Message with handle {handle} created at {Created} is expired.", type, handle, msg.Created);
                    await SendReplyMessage(handle,
                        (new ResponseMessage()
                        { Type = ResponseMessage.ResponseTypes.Failure, Message = "Message is Expired." }).Serialize(),
                        dest.ConnectionString);
                }
                else
                {
                    await msg.Process(Config, handle);
                    await SendReplyMessage(handle,
                        (new ResponseMessage()
                        { Type = ResponseMessage.ResponseTypes.Success, Message = "Completed" }).Serialize(),
                        dest.ConnectionString);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing message of type {MessageType} with handle {handle}", type, handle);
                await SendReplyMessage(handle, (new ResponseMessage() { Type = ResponseMessage.ResponseTypes.Failure, Message = ex.Message, Exception = ex }).Serialize(), dest.ConnectionString);
            }
            finally
            {
                MessageSemaphore.Release();
                await EndConversation(handle, dest.ConnectionString);
            }
        }

        public static async Task EndConversation(Guid handle, string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("END CONVERSATION @handle", cn) { CommandType = CommandType.Text };
            cmd.Parameters.AddWithValue("@handle", handle);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task SetupMessaging(int agentID, string connectionString, bool EnableMessaging)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("Messaging.Setup", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DBADashAgentID", agentID);
            cmd.Parameters.AddWithValue("@Enable", EnableMessaging);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task SendReplyMessage(Guid handle, byte[] message, string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("Messaging.SendReplyFromServiceToGUI", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Payload", message);
            cmd.Parameters.AddWithValue("@RecvReqDlgHandle", handle);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task SendMessageToService(byte[] payload, int agentID, Guid messageGroup, string connectionString, int LifeTime)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("Messaging.SendMessageFromGUIToService", cn) { CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync();
            cmd.Parameters.AddWithValue("@Payload", payload);
            var handle = Guid.NewGuid();
            cmd.Parameters.AddWithValue("@InitDlgHandle", handle);
            cmd.Parameters.AddWithValue("@ConversationGroup", messageGroup);
            cmd.Parameters.AddWithValue("@DBADashAgentID", agentID);
            cmd.Parameters.AddWithValue("@LifeTime", LifeTime);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}