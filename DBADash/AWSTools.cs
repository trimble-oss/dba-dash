using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.SQS;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace DBADash
{
    public class AWSTools
    {
        private static readonly ConcurrentDictionary<string, RegionEndpoint> RegionCache = new();

        public static AWSCredentials GetAWSCredentialsFromProfile(string profileName)
        {
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            if (credentialProfileStoreChain.TryGetAWSCredentials(profileName, out AWSCredentials defaultCredentials))
                return defaultCredentials;
            else
                throw new AmazonClientException("Unable to find profile in CredentialProfileStoreChain.");
        }

        public static AWSCredentials GetCredentials(string profile, string accessKey, string secretKey)
        {
            AWSCredentials cred;
            if (accessKey != null && secretKey != null && accessKey.Length > 0 && secretKey.Length > 0)
            {
                cred = new BasicAWSCredentials(accessKey, secretKey);
            }
            else if (profile is { Length: > 0 })
            {
                try
                {
                    cred = GetAWSCredentialsFromProfile(profile);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting AWS creds from profile {profile}", profile);
                    throw;
                }
            }
            else
            {
                cred = new InstanceProfileAWSCredentials();
            }
            return cred;
        }

        public static async Task<AmazonS3Client> GetAWSClientAsync(string profile, string accessKey, string secretKey, AmazonS3Uri uri)
        {
            var cred = GetCredentials(profile, accessKey, secretKey);
            if (!RegionCache.TryGetValue(uri.Bucket, out var AWSRegion))
            {
                using var tempClient = new AmazonS3Client(cred, RegionEndpoint.EUWest2);
                try
                {
                    var location = await tempClient.GetBucketLocationAsync(uri.Bucket);
                    AWSRegion = RegionEndpoint.GetBySystemName(location.Location.ToString());
                }
                catch (AmazonS3Exception ex) when (ex.Message == "Access Denied")
                {
                    AWSRegion = uri.Region ?? RegionEndpoint.USEast1;
                    Log.Warning(ex, "Insufficient permissions to get bucket location for {bucket}.  Grant s3:GetBucketLocation. Region set to {region} (Uri parsing or default)", uri.Bucket, AWSRegion.SystemName);
                }
                catch (Exception ex)
                {
                    AWSRegion = uri.Region ?? RegionEndpoint.USEast1;
                    Log.Warning(ex, "Unable to get bucket location using GetBucketLocationAsync for {bucket}.  Region set to {region} (Uri parsing or default)", uri.Bucket,AWSRegion.SystemName);
                }

                RegionCache.TryAdd(uri.Bucket, AWSRegion);
            }

            return new AmazonS3Client(cred, AWSRegion);
        }

        public static AmazonSQSClient GetSQSClient(string profile,string accessKey, string secretKey, string queueUrl)
        {
            if (!RegionCache.TryGetValue(queueUrl, out var region))
            {
                region = GetRegionForQueue(queueUrl);
                RegionCache.TryAdd(queueUrl, region);
            }

            return GetSQSClient(profile, accessKey, secretKey, region);
        }

        public static AmazonSQSClient GetSQSClient(string profile, string accessKey, string secretKey, RegionEndpoint region)
        {

            var cred = GetCredentials(profile, accessKey, secretKey);

            return new AmazonSQSClient(cred, region);
        }

        public static RegionEndpoint GetRegionForQueue(string queueUrl)
        {
            if (RegionCache.TryGetValue(queueUrl, out var AWSRegion)) return AWSRegion;
            // Extract the region from the queue URL
            try
            {
                var uri = new Uri(queueUrl);
                var region = uri.Host.Split('.')[1];
                AWSRegion = RegionEndpoint.GetBySystemName(region);
                RegionCache.TryAdd(queueUrl, AWSRegion);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting region for queue {queueUrl}", queueUrl);
                throw;
            }
            return AWSRegion;
        }

        public static async Task ChangeMessageVisibilityAsync(AmazonSQSClient sqsClient, string queueUrl, string receiptHandle, int visibilityTimeout)
        {
            var request = new ChangeMessageVisibilityRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle,
                VisibilityTimeout = visibilityTimeout // Set to 0 to make the message immediately visible
            };

            await sqsClient.ChangeMessageVisibilityAsync(request);
        }

        public static async Task DeleteMessageAsync(AmazonSQSClient sqsClient, string queueUrl, string receiptHandle)
        {
            var deleteMessageRequest = new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle
            };

            await sqsClient.DeleteMessageAsync(deleteMessageRequest);
        }

        private static readonly ConcurrentDictionary<RegionEndpoint, IAmazonSQS> _clients = new();

        public static IAmazonSQS GetOrCreateClient(RegionEndpoint region,CollectionConfig cfg)
        {
            var client =GetSQSClient(cfg.AWSProfile, cfg.AccessKey, cfg.GetSecretKey(), region);
            return _clients.GetOrAdd(region,client);
        }


        public static async Task SendSQSMessageAsync(CollectionConfig cfg,string messageBody, string fromIdentifier, string toIdentifier, Guid handle, string toQueue,string messageType,string destinationConnectionHash)
        {
            var region = AWSTools.GetRegionForQueue(toQueue);
            var client = GetOrCreateClient(region,cfg);

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = toQueue,
                MessageBody = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "DBADashFromIdentifier", new MessageAttributeValue { DataType = "String", StringValue = fromIdentifier } },
                    { "DBADashToIdentifier", new MessageAttributeValue { DataType = "String", StringValue = toIdentifier } },
                    { "ReplySQSQueue", new MessageAttributeValue { DataType = "String", StringValue = cfg.ServiceSQSQueueUrl }},
                    { "DestinationConnectionHash", new MessageAttributeValue {DataType = "String", StringValue = destinationConnectionHash }},
                    { "Handle", new MessageAttributeValue { DataType = "String", StringValue = handle.ToString() } },
                    { "MessageType", new MessageAttributeValue { DataType = "String", StringValue = messageType} },
                }
            };

            await client.SendMessageAsync(sendMessageRequest);
        }

    }
}
