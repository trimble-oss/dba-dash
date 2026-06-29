using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public class ResponseMessage
    {
        public enum ResponseTypes
        {
            Progress,
            Failure,
            Success,
            EndConversation,
            Cancellation,
            Warning
        }

        public ResponseTypes Type { get; set; }

        private Exception _exception;

        public Exception Exception
        {
            get => _exception;
            set
            {
                try
                {
                    // Check exception can be serialized #1284.
                    JsonConvert.DeserializeObject<Exception>(JsonConvert.SerializeObject(value));
                    _exception = value;
                }
                catch
                {
                    _exception = new Exception(value.ToString()); // Create a new exception that can be serialized if needed
                }
            }
        }

        public string Message { get; set; }

        public string MessageDataPath { get; set; }

        /// <summary>
        /// Populated on a <see cref="ResponseTypes.Warning"/> reply when the message was skipped because the
        /// requested collection(s) are disabled (no schedule) for the instance.  Carries the disabled
        /// collection names so the GUI can offer to re-run them with <see cref="CollectionMessage.IgnoreDisabledSchedule"/>.
        /// </summary>
        public List<string> DisabledCollections { get; set; }

        /// <summary>
        /// Optional per-instance progress payload.  Populated when a batched message (e.g.
        /// <see cref="MultiCollectionMessage"/>) reports the completion of a single instance so the
        /// GUI can tick instances off as they complete rather than waiting for the whole batch.
        /// </summary>
        public CollectionProgress CollectionProgress { get; set; }

        /// <summary>
        /// On a terminal reply (Success/Failure/Warning), the number of Progress messages that were
        /// sent before this one.  The SQS relay uses this to wait for all Progress messages to arrive
        /// before ending the Service Broker conversation, compensating for out-of-order SQS delivery.
        /// Null when not applicable (e.g. single-instance messages or the local/non-relay path).
        /// </summary>
        public int? ExpectedProgressCount { get; set; }

        [JsonIgnore] public DataSet Data { get; set; }

        public string DataString
        {
            get
            {
                if (Data == null)
                {
                    return null;
                }

                using var stream = new StringWriter();
                Data.WriteXml(stream, XmlWriteMode.WriteSchema);
                return stream.ToString();
            }
            set
            {
                if (value == null)
                {
                    Data = null;
                }
                else
                {
                    using var stream = new StringReader(value);
                    Data = new DataSet();
                    Data.ReadXml(stream, XmlReadMode.ReadSchema);
                }
            }
        }

        public async Task DownloadData(CollectionConfig Config)
        {
            if (!string.IsNullOrWhiteSpace(MessageDataPath))
            {
                Log.Information("Downloading message data from {path}", MessageDataPath);
                var uri = new Amazon.S3.Util.AmazonS3Uri(MessageDataPath);
                using var s3Cli = await AWSTools.GetS3ClientForEndpointAsync(Config.AWSProfile, Config.AccessKey, Config.GetSecretKey(), MessageDataPath);

                using var s3Obj = await s3Cli.GetObjectAsync(uri.Bucket, uri.Key);

                await using var responseStream = s3Obj.ResponseStream;

                var ds = new DataSet();
                ds.ReadXml(responseStream);
                Data = ds;
                Log.Information("Downloaded succeeded, removing data from bucket {path}", MessageDataPath);
                await s3Cli.DeleteObjectAsync(uri.Bucket, uri.Key);
            }
        }

        public byte[] Serialize() => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this, Formatting.Indented)).Compress();

        public static ResponseMessage Deserialize(byte[] compressedData)
        {
            var json = Encoding.UTF8.GetString(compressedData.Decompress());
            return JsonConvert.DeserializeObject<ResponseMessage>(json);
        }
    }

    /// <summary>
    /// Lightweight per-instance status carried on a <see cref="ResponseMessage"/> so the GUI can
    /// update a single instance row while a batched collection is still running.
    /// </summary>
    public class CollectionProgress
    {
        public int InstanceID { get; set; }

        /// <summary>"Success", "Failed" or "Warning" (e.g. the collection was skipped as it is disabled).</summary>
        public string Status { get; set; }

        /// <summary>Optional detail (e.g. an error message when the instance failed).</summary>
        public string Detail { get; set; }

        /// <summary>
        /// When <see cref="Status"/> is "Warning" because the collection(s) are disabled (no schedule) for
        /// this instance, the disabled collection names so the GUI can offer to re-run them forced.
        /// </summary>
        public List<string> DisabledCollections { get; set; }
    }
}