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

        private static readonly ResiliencePipeline AgentIDRetry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = int.MaxValue, // Forever retry
                Delay = TimeSpan.FromSeconds(60),
                OnRetry = args =>
                {
                    Log.Error(args.Outcome.Exception,
                        "Error getting Agent Id.  Retrying in {RetryDelaySeconds} seconds. (Retry count: {RetryCount})", args.RetryDelay.TotalSeconds, args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();


        public MessageProcessing(CollectionConfig config)
        {
            Config = config;
            Agent = DBADashAgent.GetCurrent();
            MessageThreads = config.MessageThreads ?? DefaultMessageThreads;
            MessageSemaphore = new(MessageThreads);
        }

        public async Task ScheduleMessaging()
        {
            if (Config.EnableMessaging)
            {
                Log.Information("Setting up messaging for destinations. {MessageThreads} threads", MessageThreads);
            }
            else
            {
                Log.Information("Disable Messaging for destinations");
            }
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
            await AgentIDRetry.Execute(() =>
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
                            Log.Information("Received message of type {MessageType} with handle {Handle}", type, handle);
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
                    Log.Error("Error message received with handle {Handle}: {MessageText}", handle, messageText);
                }
                else
                {
                    Log.Error("Unknown message type {MessageType} with message {MessageBody}", type, message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling message type {MessageType} with {Handle}", type, handle);
            }
            finally
            {
                await EndConversation(handle, connectionString);
            }
        }

        public async Task ProcessMessage(DBADashConnection dest, Guid handle, string type, byte[] message)
        {
            bool endConversation = true;
            using var logHandle = LogScope.Handle(handle);
            // Set once the message is read, and kept until the end - through the handlers below, so a skipped or
            // failed message's warning or error carries them too
            IDisposable logMessage = null;
            IDisposable logConnection = null;

            try
            {
                var msg = MessageBase.Deserialize(message);
                logMessage = LogScope.Message(msg.Id);
                logConnection = LogScope.Connection((msg as IConnectionMessage)?.ConnectionID);
                if (msg.IsExpired)
                {
                    Log.Error("Message {MessageId} of type {MessageType} with handle {Handle} created at {Created} is expired.", msg.Id, type, handle, msg.Created);
                    await SendReplyMessage(handle,
                        (new ResponseMessage()
                        { Type = ResponseMessage.ResponseTypes.Failure, Message = "Message is Expired." }).Serialize(),
                        dest.ConnectionString);
                }
                else if (msg.CollectAgent.AgentIdentifier != DBADashAgent.GetCurrent().AgentIdentifier)
                {
                    // Relay to the queue and S3 path the agent registered in the repository, not the ones in the message - the message could name any
                    await ApplyRegisteredRelayAsync(msg.CollectAgent, dest.ConnectionString);
                    // The remote service knows its instances by the ConnectionIDs they have before a source's prefix is added
                    msg = ConnectionIDPrefix.PrepareForRelay(msg, Config.GetSourceConnectionsSnapshot());

                    Log.Information("Message {MessageId} with handle {Handle} needs to be relayed to the remote service {Host} via {Sqs}", msg.Id, handle, msg.CollectAgent.AgentHostName, msg.CollectAgent.ServiceSQSQueueUrl);
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

                    if (msg is HeartbeatMessage)
                    {
                        // Heartbeat received - process immediately (never queue a keep-alive behind a busy service).
                        await msg.Process(Config, handle, CancellationToken.None);
                        await SendReplyMessage(handle,
                            (new ResponseMessage()
                            { Type = ResponseMessage.ResponseTypes.Success, Message = "Heartbeat", Data = null }).Serialize(),
                            dest.ConnectionString);
                        return;
                    }

                    if (msg.RunOutsideConcurrencyLimit)
                    {
                        // Long-running messages (e.g. an ad-hoc XE trace) must not hold one of the limited
                        // message-processing slots for their whole duration.  They run outside the semaphore -
                        // concurrency is bounded by the message itself (e.g. one trace per instance).
                        await ProcessAndReply(msg, handle, dest, type);
                        return;
                    }

                    // Semaphore to limit the number of messages processed at once
                    using var messageLock = await MessageSemaphore.LockOrNullAsync(msg.SemaphoreTimeout);
                    if (messageLock is null)
                    {
                        // Semaphore timed out - send a message back to the service to try again later
                        Log.Warning("Semaphore timed out processing message {MessageId} of type {MessageType} with handle {Handle}", msg.Id, type, handle);
                        await SendReplyMessage(handle,
                            (new ResponseMessage()
                            { Type = ResponseMessage.ResponseTypes.Failure, Message = "Service is busy processing other requests.  Please try again later.", Data = null }).Serialize(),
                            dest.ConnectionString);
                    }
                    else
                    {
                        await ProcessAndReply(msg, handle, dest, type);
                    }
                }
            }
            catch (CollectionScheduleDisabledException ex)
            {
                // Requested collections are disabled for the instance - report as a warning, not an error.
                Log.Warning("Message of type {MessageType} with handle {Handle} skipped: {Reason}", type, handle, ex.Message);
                await SendReplyMessage(handle, (new ResponseMessage() { Type = ResponseMessage.ResponseTypes.Warning, Message = ex.Message, DisabledCollections = ex.DisabledCollections, ConfigurationDisabledCollections = ex.ConfigurationDisabledCollections }).Serialize(), dest.ConnectionString);
            }
            catch (UnknownCollectionTypeException ex)
            {
                // None of the requested collections are valid for the instance - report as a warning, not an
                // error.  No disabled list: unlike unscheduled collections there's nothing to offer to re-run.
                Log.Warning("Message of type {MessageType} with handle {Handle} skipped: {Reason}", type, handle, ex.Message);
                await SendReplyMessage(handle, (new ResponseMessage() { Type = ResponseMessage.ResponseTypes.Warning, Message = ex.Message }).Serialize(), dest.ConnectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing message of type {MessageType} with handle {Handle}", type, handle);
                await SendReplyMessage(handle, (new ResponseMessage() { Type = ResponseMessage.ResponseTypes.Failure, Message = ex.Message, Exception = ex }).Serialize(), dest.ConnectionString);
            }
            finally
            {
                if (endConversation)
                {
                    await EndConversation(handle, dest.ConnectionString);
                }

                // In the reverse of the order they were added
                logConnection?.Dispose();
                logMessage?.Dispose();
            }
        }

        /// <summary>
        /// Processes a message (with cancellation support and progress streaming) and sends the terminal Success
        /// reply.  Shared by the semaphore-bounded path and the bypass path for <see
        /// cref="MessageBase.RunOutsideConcurrencyLimit"/> messages.  Exceptions propagate to the caller's handlers.
        /// </summary>
        private async Task ProcessAndReply(MessageBase msg, Guid handle, DBADashConnection dest, string type)
        {
            Log.Information("Processing message {MessageId} of type {MessageType} with handle {Handle}", msg.Id, type, handle);

            // Allow the message to stream intermediate replies back to the GUI (e.g. per-instance
            // progress for a batched collection).  Local path - data (if any) is serialized inline.
            msg.ProgressReporter = progress => SendReplyMessage(handle, progress.Serialize(), dest.ConnectionString);

            var ds = await msg.ProcessWithCancellation(Config, msg.Id);
            await SendReplyMessage(handle,
                (new ResponseMessage()
                { Type = ResponseMessage.ResponseTypes.Success, Message = "Completed", Data = ds })
                .Serialize(),
                dest.ConnectionString);
        }

        /// <summary>
        /// Sets the SQS queue and S3 path of a remote agent a message is relayed to from the repository (dbo.DBADashAgentRelay_Get),
        /// replacing the ones in the message.  Throws if the agent hasn't registered a queue.
        /// </summary>
        private static async Task ApplyRegisteredRelayAsync(DBADashAgent agent, string connectionString)
        {
            var (queueUrl, s3Path) = await GetRegisteredRelayAsync(agent.AgentIdentifier, connectionString);
            if (string.IsNullOrEmpty(queueUrl))
            {
                throw new Exception($"Unable to relay the message.  The remote service {agent.AgentHostName} hasn't registered an SQS queue in the repository.");
            }
            agent.ServiceSQSQueueUrl = queueUrl;
            agent.S3Path = s3Path;
        }

        /// <summary>The SQS queue and S3 path a remote agent registered in the repository (dbo.DBADashAgentRelay_Get).  Nulls if it isn't registered.</summary>
        internal static async Task<(string QueueUrl, string S3Path)> GetRegisteredRelayAsync(string agentIdentifier, string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.DBADashAgentRelay_Get", cn) { CommandType = CommandType.StoredProcedure };
            // Stored truncated to 22 characters (the identifier's base64 padding)
            cmd.Parameters.Add("@AgentIdentifier", SqlDbType.Char, 22).Value = agentIdentifier.Length > 22 ? agentIdentifier[..22] : agentIdentifier;
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            if (!await rdr.ReadAsync()) return (null, null);
            return (rdr["ServiceSQSQueueUrl"] as string, rdr["S3Path"] as string);
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

        public static async Task SendMessageToService(byte[] payload, int agentID, Guid messageGroup, string connectionString, int LifeTime, CancellationToken cancellationToken = default)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("Messaging.SendMessageFromGUIToService", cn) { CommandType = CommandType.StoredProcedure };
            await cn.OpenAsync(cancellationToken);
            cmd.Parameters.AddWithValue("@Payload", payload);
            var handle = Guid.NewGuid();
            cmd.Parameters.AddWithValue("@InitDlgHandle", handle);
            cmd.Parameters.AddWithValue("@ConversationGroup", messageGroup);
            cmd.Parameters.AddWithValue("@DBADashAgentID", agentID);
            cmd.Parameters.AddWithValue("@Lifetime", LifeTime);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}