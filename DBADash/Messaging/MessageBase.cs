using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DBADash.Messaging
{
    // Define a base class for message types
    public abstract class MessageBase : IMessage
    {
        [JsonIgnore] public int SemaphoreTimeout { get; set; } = 2000;

        public abstract Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken);

        /// <summary>
        /// Generates a CancellationTokenSource and associates it with the message ID.  Passes the token to the Process method and removes when the method completes.  The user can send a cancellation request which will look up the token source by Id and cancel it.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public async Task<DataSet> ProcessWithCancellation(CollectionConfig cfg, Guid handle)
        {
            // Create a CancellationTokenSource and track it
            using var cts = new CancellationTokenSource();
            CancellationTokenManager.Add(Id, cts);

            try
            {
                // Call the abstract ProcessCore method where derived classes implement their logic
                return await Process(cfg, handle, cts.Token);
            }
            finally
            {
                // Ensure the CancellationTokenSource is removed from the manager
                CancellationTokenManager.Remove(Id);
            }
        }

        // Unique identifier for the message (Service Broker Message Group).  Used to track the message to allow cancellation.
        public Guid Id { get; set; }

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