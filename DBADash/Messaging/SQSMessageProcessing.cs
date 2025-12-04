using Amazon.SQS;
using Amazon.SQS.Model;
using AsyncKeyedLock;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public class SQSMessageProcessing
    {
        private readonly CollectionConfig Config;
        private readonly AmazonSQSClient _sqsClient;
        private const int clearMessageVisibilityTimeout = 0; //ms
        private const int messageVisibilityTimeout = 10000; //ms
        private const int delayAfterReceivingMessageForDifferentAgent = 1000; // ms
        private const int delayBetweenMessages = 100; // ms
        private const int errorDelay = 1000; // ms
        private const int MaxDegreeOfParallelism = 2;
        private readonly AsyncKeyedLocker<string> _semaphores = new(o => o.MaxCount = MaxDegreeOfParallelism);

        private static readonly ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1)
            })
        .Build();

        public SQSMessageProcessing(CollectionConfig config)
        {
            Config = config;
            _sqsClient = AWSTools.GetSQSClient(config.AWSProfile, config.AccessKey, config.GetSecretKey(), config.ServiceSQSQueueUrl);

        }

        /// <summary>
        /// Listen to messages from SQS Queue and process them
        /// </summary>
        /// <param name="DBADashAgentIdentifier">Identifier for this service.  Allows multiple services to share the same SQS queue - messages not intended for this agent are ignored.  A separate queue for each service is advised.</param>
        /// <returns></returns>
        public async Task ProcessSQSQueue(string DBADashAgentIdentifier)
        {
            Log.Information("Listening to messages from SQS Queue sent to {identifier}", DBADashAgentIdentifier);
            if (string.IsNullOrEmpty(Config.ServiceSQSQueueUrl)) return;
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = Config.ServiceSQSQueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20, // Enable long polling
                MessageAttributeNames = new List<string> { "All" },
                MessageSystemAttributeNames = new List<string> { MessageSystemAttributeName.SentTimestamp }
            };
            while (true)
            {
                try
                {
                    var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest).ConfigureAwait(false);

                    if (receiveMessageResponse is { Messages.Count: > 0 })
                    {
                        foreach (var message in receiveMessageResponse.Messages)
                        {
                            try
                            {
                                //  Validations
                                if (!ValidateMessageAttributes(message, DBADashAgentIdentifier, out var reason,
                                        out var messageType, out var handle, out var destinationConnectionHash, out var notForThisAgent, out var deleteMessage, out string replySQS, out var replyAgent))
                                {
                                    Log.Warning("Invalid message: {reason}", reason);
                                    if (notForThisAgent) // Message is for a different agent, change visibility so it can be processed immediately
                                    {
                                        await AWSTools.ChangeMessageVisibilityAsync(_sqsClient, Config.ServiceSQSQueueUrl,
                                            message.ReceiptHandle, clearMessageVisibilityTimeout);
                                        await Task.Delay(delayAfterReceivingMessageForDifferentAgent);
                                    }
                                    if (deleteMessage) // Message failed validation but should be deleted. e.g. Older than 60min
                                    {
                                        await AWSTools.DeleteMessageAsync(_sqsClient, Config.ServiceSQSQueueUrl,
                                                                                       message.ReceiptHandle);
                                    }
                                    continue;
                                }
                                await AWSTools.ChangeMessageVisibilityAsync(_sqsClient, Config.ServiceSQSQueueUrl,
                                    message.ReceiptHandle, messageVisibilityTimeout);

                                // Remove the message from the queue.  Avoid poison messages by doing this first as we don't need 100% reliable processing.  Retry logic is used for fault tolerance.
                                await AWSTools.DeleteMessageAsync(_sqsClient, Config.ServiceSQSQueueUrl,
                                    message.ReceiptHandle);

                                if (messageType == "REPLY") // Reply message sent from remote service
                                {
                                    await ProcessReply(message, handle, destinationConnectionHash);
                                }
                                else // A message for this service to action
                                {
                                    // Process on a separate thread
                                    _ = ProcessMessageAsync(message, DBADashAgentIdentifier, handle,
                                        destinationConnectionHash, replySQS, replyAgent);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Handle any exceptions that occurred during processing
                                Log.Error(ex, $"Error processing message: {message.Body}");
                                await Task.Delay(errorDelay); // Extra delay if error occurs to avoid burning CPU cycles
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error receiving messages from SQS Queue");
                    await Task.Delay(errorDelay); // Extra delay if error occurs to avoid burning CPU cycles
                }

                await Task.Delay(delayBetweenMessages); // Wait a small amount of time before checking for more messages to avoid burning CPU cycles (shouldn't be required)
            }
        }

        private static bool ValidateMessageAttributes(Message message, string expectedAgent, out string reason, out string messageType, out Guid handle, out string destinationConnectionHash, out bool notForThisAgent, out bool deleteMessage, out string replySQS, out string replyAgent)
        {
            reason = string.Empty;
            messageType = string.Empty;
            handle = Guid.Empty;
            destinationConnectionHash = string.Empty;
            notForThisAgent = false;
            deleteMessage = true;
            replySQS = string.Empty;
            replyAgent = string.Empty;

            /* Remove old messages regardless of target service.  This will prevent constant re-processing of old messages */
            if (!message.Attributes.TryGetValue(MessageSystemAttributeName.SentTimestamp, out var sentTimestamp))
            {
                reason = "Message does not contain a SentTimestamp attribute.";
                return false;
            }
            if (sentTimestamp != null)
            {
                var sentTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(sentTimestamp))
                    .DateTime;
                if (sentTime < DateTime.UtcNow.AddMinutes(-60))
                {
                    reason = "Message is older than 60 minutes.";
                    return false;
                }
            }
            else
            {
                reason = "Message sent timestamp is NULL";
                return false;
            }

            if (!message.MessageAttributes.TryGetValue("DBADashToIdentifier", out var targetAgent))
            {
                reason = "Message does not contain a DBADashToIdentifier attribute.";
                deleteMessage = false;
                return false;
            }
            /* Might be using a shared queue.  Keep the message. Visibility timeout adjusted to allow other service to pick up the message. */
            if (targetAgent.StringValue != expectedAgent)
            {
                reason = $"Message is not intended for this agent.  Expected {expectedAgent} but received {targetAgent.StringValue}.  This is expected when using a shared queue.";
                notForThisAgent = true;
                deleteMessage = false;
                return false;
            }

            if (!message.MessageAttributes.TryGetValue("MessageType", out var messageTypeAttribute))
            {
                reason = "Message does not contain a MessageType attribute.";

                return false;
            }
            messageType = messageTypeAttribute.StringValue;

            if (!message.MessageAttributes.TryGetValue("Handle", out var handleString))
            {
                reason = "Message does not contain a Handle attribute.";
                return false;
            }
            handle = Guid.Parse(handleString.StringValue);
            if (!message.MessageAttributes.TryGetValue("DestinationConnectionHash", out var destinationConnectionHashAttribute))
            {
                reason = "Message does not contain a DestinationConnectionHash attribute.";
                return false;
            }
            if (!message.MessageAttributes.TryGetValue("ReplySQSQueue", out var replySQSAttribute))
            {
                reason = "Message does not contain a ReplySQSQueue attribute.";
                return false;
            }
            if (!message.MessageAttributes.TryGetValue("DBADashFromIdentifier", out var sourceAgent))
            {
                reason = "Message does not contain a DBADashFromIdentifier attribute.";
                return false;
            }
            replySQS = replySQSAttribute.StringValue;
            replyAgent = sourceAgent.StringValue;
            destinationConnectionHash = destinationConnectionHashAttribute.StringValue;
            return true;
        }

        /// <summary>
        /// Process message from the SQS queue.  e.g. trigger a collection
        /// </summary>
        /// <param name="message">SQS message containing MessageBase payload</param>
        /// <param name="DBADashAgentIdentifier">Target DBA Dash agent for this message</param>
        /// <param name="handle">Service broker conversation handle</param>
        /// <param name="destinationConnectionHash">Hash of destination connection so we know where the message came from when there are multiple destinations</param>
        /// <param name="replySQS">SQS queue to send reply messages to</param>
        /// <param name="replyAgent">Identifier for agent to reply messages to</param>
        /// <returns></returns>
        private async Task ProcessMessageAsync(Message message, string DBADashAgentIdentifier, Guid handle, string destinationConnectionHash, string replySQS, string replyAgent)
        {
            var msg = await ValidateMessage(message, DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent);
            if (msg == null) return;

            // Implementations of MessageBase will process the message and return a DataSet or null
            try
            {
                if (msg is CancellationMessage cancellationMessage) // Process cancellation message immediately without waiting for semaphore
                {
                    await cancellationMessage.Process(Config, handle, CancellationToken.None);
                    await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                        ResponseMessage.ResponseTypes.Failure, "Message Cancelled").ConfigureAwait(false);
                    return;
                }
                using var semaphore = await _semaphores.LockOrNullAsync(destinationConnectionHash, msg.SemaphoreTimeout);
                if (semaphore is null) // Semaphore used to limit concurrent processing per connection
                {
                    Log.Warning("Semaphore timeout for message {id} of type {type} with handle {handle}.  Service is busy.", msg.Id, msg.GetType().ToString(), handle);
                    // Semaphore timed out, service is busy
                    await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                        ResponseMessage.ResponseTypes.Failure, "Service is busy.  Try again later.").ConfigureAwait(false);

                    return;
                }
                await DoProcessMessageAsync(msg, DBADashAgentIdentifier, handle, destinationConnectionHash,
                    replySQS,
                    replyAgent);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing message with handle {handle}", handle);
                await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                    ResponseMessage.ResponseTypes.Failure, ex.Message).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run the processing task for the message.  Send a reply message to the source agent when complete
        /// </summary>
        /// <param name="msg">MessageBase object.  Process method is called to execute the task</param>
        /// <param name="DBADashAgentIdentifier">Target DBA Dash agent for this message</param>
        /// <param name="handle">Service broker conversation handle</param>
        /// <param name="destinationConnectionHash">Hash of destination connection so we know where the message came from when there are multiple destinations</param>
        /// <param name="replySQS">SQS queue to send reply messages to</param>
        /// <param name="replyAgent">Identifier for agent to reply messages to</param>
        /// <returns></returns>
        private async Task DoProcessMessageAsync(MessageBase msg, string DBADashAgentIdentifier, Guid handle, string destinationConnectionHash, string replySQS, string replyAgent)
        {
            // Errors handled by the caller
            Log.Information("Send message receipt for handle {handle} & process message", handle);
            await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                ResponseMessage.ResponseTypes.Progress, "Message Received");

            var ds = await msg.ProcessWithCancellation(Config, handle);
            string messageDataPath = null;
            if (ds != null)
            {
                // DataSet might be large, so write to S3 and send the path in the reply message
                var fileName = $"{handle}.message";
                Log.Debug("Writing message to S3 {filename}", fileName);
                messageDataPath = msg.CollectAgent.S3Path.AppendToUrl(fileName);
                await DestinationHandling.WriteS3Async(ds, msg.CollectAgent.S3Path, fileName, Config);
            }

            // Send a reply message to the source agent
            await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                ResponseMessage.ResponseTypes.Success, "Completed", messageDataPath).ConfigureAwait(false);
        }

        private async Task SendReplyMessage(string DBADashAgentIdentifier, Guid handle,
            string destinationConnectionHash, string replySQS, string replyAgent,
            ResponseMessage.ResponseTypes responseType, string message, string messageDataPath = null)
        {
            var payload = CreateResponsePayload(responseType, message, messageDataPath);
            await AWSTools.SendSQSMessageAsync(Config, Convert.ToBase64String(payload),
                DBADashAgentIdentifier, replyAgent, handle, replySQS, "REPLY", destinationConnectionHash);
        }

        /// <summary>
        /// Deserialize the message payload and check if it is expired
        /// </summary>
        /// <param name="message">SQS message</param>
        /// <param name="DBADashAgentIdentifier">Target DBA Dash agent for this message</param>
        /// <param name="handle">Service broker conversation handle</param>
        /// <param name="destinationConnectionHash">Hash of destination connection so we know where the message came from when there are multiple destinations</param>
        /// <param name="replySQS">SQS queue to send reply messages to</param>
        /// <param name="replyAgent">Identifier for agent to reply messages to</param>
        /// <returns>Deserialized MessageBase object. null if message is expired or couldn't be deserialized.</returns>
        private async Task<MessageBase> ValidateMessage(Message message, string DBADashAgentIdentifier, Guid handle, string destinationConnectionHash, string replySQS, string replyAgent)
        {
            MessageBase msg;
            try
            {
                // Acknowledge the message is received before we start processing
                Log.Debug($"Received message: {message.MessageId}");
                var payloadBin = Convert.FromBase64String(message.Body);
                msg = MessageBase.Deserialize(payloadBin);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Message with handle {handle} couldn't be deserialized", handle);
                await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                    ResponseMessage.ResponseTypes.Failure, "Message couldn't be read");
                return null;
            }
            if (msg.IsExpired) // Check if the message is expired - if so, send a failure message
            {
                Log.Error("Message with handle {handle} created at {Created} is expired.", handle, msg.Created);

                await SendReplyMessage(DBADashAgentIdentifier, handle, destinationConnectionHash, replySQS, replyAgent,
                    ResponseMessage.ResponseTypes.Failure, "Message is Expired");

                return null;
            }
            else
            {
                return msg;
            }
        }

        private static byte[] CreateResponsePayload(ResponseMessage.ResponseTypes responseType, string message, string messageDataPath = null)
        {
            var responseMessage = new ResponseMessage
            {
                Type = responseType,
                Message = message,
                MessageDataPath = messageDataPath
            };
            return responseMessage.Serialize();
        }

        /// <summary>
        /// Process the reply message from the remote service.  Send data back to the SQL repository DB & notification back to GUI via service broker
        /// </summary>
        /// <param name="message"></param>
        /// <param name="handle">Service broker conversation handle to reply to</param>
        /// <param name="destinationConnectionHash">Hash of destination connection so we know which repository database to send the reply to if there are multiple destinations</param>
        /// <returns></returns>
        private async Task ProcessReply(Message message, Guid handle, string destinationConnectionHash)
        {
            if (Config.DestinationConnection.Type != DBADashConnection.ConnectionType.SQL)
            {
                Log.Error("Destination is not SQL");
            }
            var destination = Config.GetDestination(destinationConnectionHash);

            var responseMessage = ResponseMessage.Deserialize(Convert.FromBase64String(message.Body));

            await responseMessage.DownloadData(Config);
            if (responseMessage.Data != null && responseMessage.Data.Tables.Contains("DBADash"))
            {
                Log.Debug("Writing data to SQL");

                await DestinationHandling.WriteDBAsync(responseMessage.Data, destination.ConnectionString,
                        Config);

                if (responseMessage.Exception == null && responseMessage.Data.Tables.Contains("Errors") && responseMessage.Data.Tables["Errors"]!.Rows.Count > 0)
                {
                    var dtErrors = responseMessage.Data.Tables["Errors"];
                    var sbErrors = new StringBuilder();
                    responseMessage.Type = ResponseMessage.ResponseTypes.Failure;
                    try
                    {
                        dtErrors.Rows.Cast<DataRow>().Select(row => (string)row["ErrorMessage"]).ToList()
                            .ForEach(errorMessage => { sbErrors.AppendLine(errorMessage); });
                    }
                    catch (Exception ex)
                    {
                        sbErrors.AppendLine("One of more errors occurred.");
                        sbErrors.AppendLine(ex.Message);
                    }

                    responseMessage.Exception = new Exception(sbErrors.ToString());
                }
                responseMessage.Data = null;
            }

            var payloadBin = responseMessage.Serialize();

            await pipeline.ExecuteAsync(async _ =>
            {
                try
                {
                    await MessageProcessing.SendReplyMessage(handle, payloadBin, destination.ConnectionString);
                }
                catch (SqlException ex) when (ex.Number == 8426) // The conversation handle "%.*ls" is not found.
                {
                    Log.Warning(
                        "The conversation handle {handle} was not found.  The conversation might have ended & this message is processing out of order.",
                        handle);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error sending reply message for handle {handle}", handle);
                    throw;
                }
            });

            switch (responseMessage.Type)
            {
                case ResponseMessage.ResponseTypes.Success:
                    Log.Information("Message with handle {handle} processed successfully by remote service.", handle);
                    await MessageProcessing.EndConversation(handle, Config.DestinationConnection.ConnectionString);
                    break;

                case ResponseMessage.ResponseTypes.Progress:
                    Log.Information("Message with handle {handle} is in progress on remote service. {message}", handle, responseMessage.Message);
                    break;

                case ResponseMessage.ResponseTypes.Failure:
                    Log.Error("Message with handle {handle} failed on remote service: {message}", handle, responseMessage.Message);
                    await MessageProcessing.EndConversation(handle, Config.DestinationConnection.ConnectionString);
                    break;
            }
        }
    }
}