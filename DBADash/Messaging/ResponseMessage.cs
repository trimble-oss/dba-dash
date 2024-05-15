using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DBADash.Messaging
{
    public class ResponseMessage
    {
        public enum ResponseTypes
        {
            Progress,
            Failure,
            Success
        }

        public ResponseTypes Type { get; set; }

        public Exception Exception { get; set; }

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
                using var s3Cli = await AWSTools.GetAWSClientAsync(Config.AWSProfile, Config.AccessKey, Config.GetSecretKey(), uri);
               
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