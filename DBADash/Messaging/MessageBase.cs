using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DBADash.Messaging
{
    // Define a base class for message types
    public abstract class MessageBase : IMessage
    {
        [JsonIgnore] public int SemaphoreTimeout { get; set; } = 2000;

        /// <summary>
        /// Set by the message processor (service broker or SQS relay) before <see cref="Process"/> is
        /// called.  Allows a long-running message to send intermediate reply messages back to the GUI
        /// (e.g. per-instance progress for a batched collection).  Null when not supported.
        /// </summary>
        [JsonIgnore] public Func<ResponseMessage, Task> ProgressReporter { get; set; }

        /// <summary>
        /// Sends an intermediate reply back to the originating GUI if a <see cref="ProgressReporter"/>
        /// has been configured.  Safe to call when no reporter is set (no-op).
        /// </summary>
        protected Task ReportProgressAsync(ResponseMessage progress) =>
            ProgressReporter?.Invoke(progress) ?? Task.CompletedTask;

        /// <summary>
        /// When true the message is processed without acquiring the shared message-processing semaphore, so a
        /// long-running message (e.g. an ad-hoc XE trace that can run for minutes) doesn't tie up one of the
        /// limited message threads.  Such messages are responsible for bounding their own concurrency.
        /// </summary>
        [JsonIgnore] public virtual bool RunOutsideConcurrencyLimit => false;

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
            var jsonObject = JObject.Parse(SerializeString());
            jsonObject["__type"] = GetType().AssemblyQualifiedName;

            // Non-ASCII characters are escaped as messages must be ASCII - see ReadMessageJson
            using var sw = new StringWriter();
            using (var writer = new JsonTextWriter(sw) { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii })
            {
                jsonObject.WriteTo(writer);
            }

            // Compress the payload
            return Encoding.ASCII.GetBytes(sw.ToString()).Compress();
        }

        /// <summary>
        /// Message types the service accepts, by full name: the concrete <see cref="MessageBase"/> types in this assembly.
        /// Messaging.SendMessageFromGUIToService lists the same types with the roles that can send them.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Type> MessageTypes = typeof(MessageBase).Assembly.GetExportedTypes()
            .Where(t => t.IsSubclassOf(typeof(MessageBase)) && !t.IsAbstract)
            .ToDictionary(t => t.FullName, StringComparer.Ordinal);

        /// <summary>
        /// Deserializes a message from the GUI.  Messaging.SendMessageFromGUIToService has checked the user can send this
        /// type of message, so the message is rejected if it could be read differently here - see <see cref="ReadMessageJson"/>
        /// and <see cref="ParseMessageObject"/>.
        /// </summary>
        public static MessageBase Deserialize(byte[] compressedData)
        {
            var json = ReadMessageJson(compressedData);
            var type = GetMessageType(ParseMessageObject(json));
            return (MessageBase)JsonConvert.DeserializeObject(json, type);
        }

        /// <summary>
        /// Decompresses a message, which must be ASCII (tab, line feed, carriage return and printable characters) as in
        /// Messaging.SendMessageFromGUIToService.  The repository reads the message in its own code page, so other
        /// characters could be read differently there.  <see cref="Serialize"/> escapes them.
        /// </summary>
        public static string ReadMessageJson(byte[] compressedData)
        {
            var data = compressedData.Decompress();
            if (data.Any(b => b is not (0x09 or 0x0A or 0x0D or (>= 0x20 and <= 0x7E))))
            {
                throw new ArgumentException("Invalid message.  Messages must be ASCII.");
            }
            return Encoding.ASCII.GetString(data);
        }

        /// <summary>
        /// Parses a message as a JSON object, rejecting anything that could be read more than one way: content after the
        /// object, or a property repeated.  Properties are matched to the message ignoring case, so repeats are found
        /// ignoring case.  Property names must be identifiers (letters, digits and underscores) so they're matched the
        /// same way here and in the repository - SQL's = ignores trailing spaces, so "EmbeddedScript " would match
        /// EmbeddedScript there but not here.
        /// </summary>
        public static JObject ParseMessageObject(string json)
        {
            using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            var jsonObject = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read())
            {
                throw new ArgumentException("Invalid message.  Unexpected content after the message.");
            }
            if (jsonObject.Properties().Any(p => p.Name.Length == 0 || !p.Name.All(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_')))
            {
                throw new ArgumentException("Invalid message.  Property names must be identifiers.");
            }
            var duplicate = jsonObject.Properties().GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException($"Invalid message.  Duplicate property {duplicate.Key}.");
            }
            return jsonObject;
        }

        /// <summary>
        /// The message type named by __type.  It's assembly qualified but only the type name is used: the type must be one
        /// of <see cref="MessageTypes"/>, so a message can't make the service create any other type.
        /// </summary>
        public static Type GetMessageType(JObject jsonObject)
        {
            var typeName = jsonObject["__type"] is JValue { Type: JTokenType.String } value ? (string)value : null;
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("Type not found in message.");
            }
            var name = typeName.Split(',')[0].Trim(' ');
            return MessageTypes.TryGetValue(name, out var type)
                ? type
                : throw new ArgumentException($"Type {typeName} is not a message type.");
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