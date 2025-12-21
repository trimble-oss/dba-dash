using Newtonsoft.Json;
using Serilog;
using System;
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
            Cancellation
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
}