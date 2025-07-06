using AsyncKeyedLock;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly int MessageThreads;
        private readonly AsyncNonKeyedLocker MessageSemaphore;

        private readonly AsyncRetryPolicy AgentIDRetryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(60),
                (exception, retryCount, calculatedWaitDuration) =>
                {
                    Log.Error(exception,
                        $"Error getting Agent Id.  Retrying in {calculatedWaitDuration.TotalSeconds} seconds. (Retry count: {retryCount})");
                });

        public MessageProcessing(CollectionConfig config)
        {
            Config = config;
            Agent = DBADashAgent.GetCurrent();
            MessageThreads = config.MessageThreads ?? DefaultMessageThreads;
            MessageSemaphore = new(MessageThreads);
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
                Log.Error("Unable to get Agent Id for {Destination}", d.ConnectionForPrint);
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

        public async Task Process(DBADashConnection dest, int agentID)
        {
            while (true)
            {
                try
                {
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

                            _ = Task.Run(() => ProcessMessage(dest, handle, type, message));
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
            // ReSharper disable once FunctionNeverReturns
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

        public async Task ProcessMessage(DBADashConnection dest, Guid handle, string type, byte[] message)
        {
            bool endConversation = true;

            try
            {
                var msg = MessageBase.Deserialize(message);
                if (msg.IsExpired)
                {
                    Log.Error("Message {Id} of type {type} with handle {handle} created at {Created} is expired.", msg.Id, type, handle, msg.Created);
                    await SendReplyMessage(handle,
                        (new ResponseMessage()
                        { Type = ResponseMessage.ResponseTypes.Failure, Message = "Message is Expired." }).Serialize(),
                        dest.ConnectionString);
                }
                else if (msg.CollectAgent.AgentIdentifier != DBADashAgent.GetCurrent().AgentIdentifier)
                {
                    Log.Information("Message {Id} with handle {handle} needs to be relayed to the remote service {host} via {sqs}", msg.Id, handle, msg.CollectAgent.AgentHostName, msg.CollectAgent.ServiceSQSQueueUrl);
                    // The message needs to be relayed to the remote agent via SQS queue
                    await SendReplyMessage(handle,
                                               (new ResponseMessage()
                                               { Type = ResponseMessage.ResponseTypes.Progress, Message = "Message relay to remote service" }).Serialize(),
                                                                      dest.ConnectionString);

                    //get hash of dest - pass it in the sqs message so it can be passed back and we know which destination
                    await AWSTools.SendSQSMessageAsync(Config, Convert.ToBase64String(msg.Serialize()), DBADashAgent.GetCurrent().AgentIdentifier, msg.CollectAgent.AgentIdentifier, handle, msg.CollectAgent.ServiceSQSQueueUrl, SendMessageType, dest.Hash);
                    endConversation = false;
                }
                else
                {
                    // Message is for this agent.  Process the message

                    if (msg is CancellationMessage)
                    {
                        // Cancellation message received - process immediately
                        await msg.Process(Config, handle, CancellationToken.None);
                        await SendReplyMessage(handle,
                            (new ResponseMessage()
                            { Type = ResponseMessage.ResponseTypes.Failure, Message = "Cancelled", Data = null }).Serialize(),
                            dest.ConnectionString);
                        return;
                    }
                    // Semaphore to limit the number of messages processed at once
                    using var messageLock = await MessageSemaphore.LockOrNullAsync(msg.SemaphoreTimeout);
                    if (messageLock is null)
                    {
                        // Semaphore timed out - send a message back to the service to try again later
                        Log.Warning("Semaphore timed out processing message {id} of type {MessageType} with handle {handle}", msg.Id, type, handle);
                        await SendReplyMessage(handle,
                            (new ResponseMessage()
                            { Type = ResponseMessage.ResponseTypes.Failure, Message = "Service is busy processing other requests.  Please try again later.", Data = null }).Serialize(),
                            dest.ConnectionString);
                    }
                    else
                    {
                        Log.Information("Processing message {id} of type {MessageType} with handle {handle}", msg.Id, type,
                            handle);

                        var ds = await msg.ProcessWithCancellation(Config, msg.Id);
                        await SendReplyMessage(handle,
                            (new ResponseMessage()
                            { Type = ResponseMessage.ResponseTypes.Success, Message = "Completed", Data = ds })
                            .Serialize(),
                            dest.ConnectionString);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing message of type {MessageType} with handle {handle}", type, handle);
                await SendReplyMessage(handle, (new ResponseMessage() { Type = ResponseMessage.ResponseTypes.Failure, Message = ex.Message, Exception = ex }).Serialize(), dest.ConnectionString);
            }
            finally
            {
                if (endConversation)
                {
                    await EndConversation(handle, dest.ConnectionString);
                }
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
            cmd.Parameters.AddWithValue("@Lifetime", LifeTime);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}