using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public byte[] Serialize() => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this, Formatting.Indented)).Compress();

        public static ResponseMessage Deserialize(byte[] compressedData)
        {
            var json = Encoding.UTF8.GetString(compressedData.Decompress());
            return JsonConvert.DeserializeObject<ResponseMessage>(json);
        }
    }
}