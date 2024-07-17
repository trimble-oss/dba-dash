using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    // Define a base class for message types
    public abstract class MessageBase : IMessage
    {
        public abstract Task<DataSet> Process(CollectionConfig cfg, Guid handle);

        public string SerializeString() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public int Lifetime { get; set; } = 300;

        public bool IsExpired => DateTime.UtcNow - Created > TimeSpan.FromSeconds(Lifetime);

        public DBADashAgent CollectAgent { get; set; }
        public DBADashAgent ImportAgent { get; set; }

        public byte[] Serialize()
        {
            var typeName = GetType().AssemblyQualifiedName;
            var json = SerializeString();
            var jsonObject = JObject.Parse(json);
            jsonObject["__type"] = typeName;
            json = jsonObject.ToString();

            // Compress the payload
            return Encoding.UTF8.GetBytes(json).Compress();
        }

        public static MessageBase Deserialize(byte[] compressedData)
        {
            var json = Encoding.UTF8.GetString(compressedData.Decompress());
            var jsonObject = JObject.Parse(json);
            var typeName = jsonObject["__type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("Type not found in message.");
            }
            var type = Type.GetType(typeName);
            return type == null
                ? throw new ArgumentException($"Type {typeName} not found.")
                : (MessageBase)JsonConvert.DeserializeObject(json, type);
        }

        protected void ThrowIfExpired()
        {
            if (IsExpired)
            {
                throw new Exception("Message expired");
            }
        }

    }
}