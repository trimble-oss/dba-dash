using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>
    /// The ConnectionID prefix a service adds to what it imports from an S3 or folder source, and removes from messages it
    /// relays to the remote service that writes there.  See <see cref="ConnectionIDPrefix"/>.
    /// </summary>
    [TestClass]
    public class ConnectionIDPrefixTests
    {
        private const string Prefix = "CustomerA:";
        private const string AgentS3Path = "s3://dbadash-bucket/customer-a";

        private static DataSet CollectedDataSet(string connectionID = "SRV-SQL01")
        {
            var ds = new DataSet();
            var dt = new DataTable("DBADash");
            dt.Columns.Add("ConnectionID", typeof(string));
            dt.Columns.Add("Instance", typeof(string));
            dt.Rows.Add(connectionID, "SRV-SQL01");
            ds.Tables.Add(dt);
            return ds;
        }

        private static DBADashSource PrefixedS3Source(string path = "https://dbadash-bucket.s3.eu-west-1.amazonaws.com/customer-a/") =>
            new(path) { ConnectionIDPrefix = Prefix };

        private static DBADashAgent RemoteAgent(string s3Path = AgentS3Path) =>
            new() { AgentHostName = "REMOTE01", S3Path = s3Path, ServiceSQSQueueUrl = "https://sqs.eu-west-1.amazonaws.com/123456789012/remote01" };

        [TestMethod]
        public void OnlyS3AndFolderSourcesHaveAPrefix()
        {
            Assert.AreEqual(Prefix, new DBADashSource("s3://bucket/path") { ConnectionIDPrefix = Prefix }.ConnectionIDPrefix);
            Assert.AreEqual(Prefix, new DBADashSource(@"\\server\share\dbadash") { ConnectionIDPrefix = Prefix }.ConnectionIDPrefix);
            Assert.IsNull(new DBADashSource("Data Source=.;Integrated Security=True") { ConnectionIDPrefix = Prefix }.ConnectionIDPrefix);
        }

        [TestMethod]
        public void PrefixIsSavedWithTheSource()
        {
            var json = JsonConvert.SerializeObject(PrefixedS3Source());
            Assert.AreEqual(Prefix, JsonConvert.DeserializeObject<DBADashSource>(json)!.ConnectionIDPrefix);
        }

        [TestMethod]
        public void SqlSourceDoesNotSaveAPrefix()
        {
            var json = JsonConvert.SerializeObject(new DBADashSource("Data Source=.;Integrated Security=True") { ConnectionIDPrefix = Prefix });
            Assert.IsFalse(json.Contains(Prefix), json);
        }

        [TestMethod]
        public void Validate()
        {
            Assert.IsNull(ConnectionIDPrefix.Validate(null));
            Assert.IsNull(ConnectionIDPrefix.Validate(""));
            Assert.IsNull(ConnectionIDPrefix.Validate("CustomerA|"));
            Assert.IsNotNull(ConnectionIDPrefix.Validate(" CustomerA:"));
            Assert.IsNotNull(ConnectionIDPrefix.Validate("CustomerA: "));
            Assert.IsNotNull(ConnectionIDPrefix.Validate("   "));
            Assert.IsNotNull(ConnectionIDPrefix.Validate(new string('x', ConnectionIDPrefix.MaxLength + 1)));
        }

        [TestMethod]
        public void ApplyPrefixesTheConnectionIDAndRecordsThePrefix()
        {
            var ds = CollectedDataSet();
            ConnectionIDPrefix.Apply(ds, Prefix);

            var row = ds.Tables["DBADash"]!.Rows[0];
            Assert.AreEqual("CustomerA:SRV-SQL01", row["ConnectionID"]);
            Assert.AreEqual("SRV-SQL01", row["Instance"], "Only the ConnectionID is prefixed");
            Assert.AreEqual(Prefix, row[ConnectionIDPrefix.AppliedColumnName]);
        }

        /// <summary>
        /// Agents are registered by host and service name, so the prefix on the service name keeps the collecting agent
        /// separate for each source - its own CollectAgentID (offline incidents) and its own S3 path and SQS queue.
        /// </summary>
        [TestMethod]
        public void ApplyScopesTheCollectingAgentToTheSource()
        {
            var ds = CollectedDataSet();
            ds.Tables["DBADash"]!.Columns.Add("AgentServiceName", typeof(string));
            ds.Tables["DBADash"]!.Rows[0]["AgentServiceName"] = "DBADashService";

            ConnectionIDPrefix.Apply(ds, Prefix);

            Assert.AreEqual("CustomerA:DBADashService", ds.Tables["DBADash"]!.Rows[0]["AgentServiceName"]);
        }

        [TestMethod]
        public void ApplyScopesTheCollectingAgentOfAFileWithoutAServiceName()
        {
            // An older service doesn't include one, and the importer registers it as {DBADashService}
            var ds = CollectedDataSet();

            ConnectionIDPrefix.Apply(ds, Prefix);

            Assert.AreEqual("CustomerA:{DBADashService}", ds.Tables["DBADash"]!.Rows[0]["AgentServiceName"]);
        }

        [TestMethod]
        public void ApplyChangesNothingIfTheAgentServiceNameWouldBeTooLong()
        {
            var ds = CollectedDataSet();
            ds.Tables["DBADash"]!.Columns.Add("AgentServiceName", typeof(string));
            ds.Tables["DBADash"]!.Rows[0]["AgentServiceName"] = new string('x', 256 - Prefix.Length + 1);

            Assert.ThrowsExactly<InvalidOperationException>(() => ConnectionIDPrefix.Apply(ds, Prefix));
            Assert.AreEqual("SRV-SQL01", ds.Tables["DBADash"]!.Rows[0]["ConnectionID"]);
            Assert.IsFalse(ds.Tables["DBADash"]!.Columns.Contains(ConnectionIDPrefix.AppliedColumnName));
        }

        [TestMethod]
        public void ApplyIsOnlyDoneOnce()
        {
            // e.g. a file written to the failed message folder after it was prefixed, then imported again
            var ds = CollectedDataSet();
            ConnectionIDPrefix.Apply(ds, Prefix);
            ConnectionIDPrefix.Apply(ds, Prefix);
            Assert.AreEqual("CustomerA:SRV-SQL01", ds.Tables["DBADash"]!.Rows[0]["ConnectionID"]);
        }

        [TestMethod]
        public void ApplySurvivesSerialization()
        {
            var ds = CollectedDataSet();
            ConnectionIDPrefix.Apply(ds, Prefix);
            using var stream = new System.IO.MemoryStream();
            ds.WriteXml(stream, XmlWriteMode.WriteSchema);
            stream.Position = 0;
            var read = new DataSet();
            read.ReadXml(stream);

            ConnectionIDPrefix.Apply(read, Prefix);
            Assert.AreEqual("CustomerA:SRV-SQL01", read.Tables["DBADash"]!.Rows[0]["ConnectionID"]);
        }

        [TestMethod]
        public void ApplyWithoutAPrefixChangesNothing()
        {
            var ds = CollectedDataSet();
            ConnectionIDPrefix.Apply(ds, null);
            ConnectionIDPrefix.Apply(ds, "");
            Assert.AreEqual("SRV-SQL01", ds.Tables["DBADash"]!.Rows[0]["ConnectionID"]);
            Assert.IsFalse(ds.Tables["DBADash"]!.Columns.Contains(ConnectionIDPrefix.AppliedColumnName));
        }

        [TestMethod]
        public void ApplyPrefixesEveryConnectionIDColumn()
        {
            var ds = CollectedDataSet();
            var offline = new DataTable("OfflineInstances");
            offline.Columns.Add("ConnectionID", typeof(string));
            offline.Rows.Add("SRV-SQL02");
            offline.Rows.Add(DBNull.Value);
            ds.Tables.Add(offline);

            ConnectionIDPrefix.Apply(ds, Prefix);

            Assert.AreEqual("CustomerA:SRV-SQL02", offline.Rows[0]["ConnectionID"]);
            Assert.AreEqual(DBNull.Value, offline.Rows[1]["ConnectionID"]);
        }

        [TestMethod]
        public void ApplyChangesNothingIfAConnectionIDWouldBeTooLong()
        {
            var ds = CollectedDataSet();
            var other = new DataTable("Other");
            other.Columns.Add("ConnectionID", typeof(string));
            other.Rows.Add(new string('x', 128 - Prefix.Length + 1));
            ds.Tables.Add(other);

            Assert.ThrowsExactly<InvalidOperationException>(() => ConnectionIDPrefix.Apply(ds, Prefix));
            Assert.AreEqual("SRV-SQL01", ds.Tables["DBADash"]!.Rows[0]["ConnectionID"]);
            Assert.IsFalse(ds.Tables["DBADash"]!.Columns.Contains(ConnectionIDPrefix.AppliedColumnName));
        }

        [TestMethod]
        public void ApplyPrefixesConnectionChangeReplyPayload()
        {
            var results = new List<ConnectionChangeResult>
            {
                new() { ConnectionID = "SRV-SQL01", Outcome = ConnectionChangeResult.Added },
                new() { ConnectionID = null, Outcome = ConnectionChangeResult.Failed }
            };
            var ds = ConnectionChanges.ToDataSet(results);

            ConnectionIDPrefix.Apply(ds, Prefix);

            var read = ConnectionChanges.FromDataSet<List<ConnectionChangeResult>>(ds);
            Assert.AreEqual("CustomerA:SRV-SQL01", read[0].ConnectionID);
            Assert.IsNull(read[1].ConnectionID);
        }

        /// <summary>
        /// A connection test reply about a connection that's already configured: the GUI looks the instance up in the
        /// repository by <see cref="SourceConnectionCheck.RepositoryConnectionID"/>, which prefers ExistingConnectionID.
        /// </summary>
        [TestMethod]
        public void ApplyPrefixesConnectionTestReplyForAConfiguredConnection()
        {
            var checks = new List<SourceConnectionCheck>
            {
                new() { ConnectionID = "SRV-SQL01", Existing = ExistingConnectionStates.Configured, ExistingConnectionID = "SRV-SQL01-Custom" },
                new() { ConnectionID = "SRV-SQL02", Existing = ExistingConnectionStates.None }
            };
            var ds = ConnectionChanges.ToDataSet(checks);

            ConnectionIDPrefix.Apply(ds, Prefix);

            var read = ConnectionChanges.FromDataSet<List<SourceConnectionCheck>>(ds);
            Assert.AreEqual("CustomerA:SRV-SQL01-Custom", read[0].RepositoryConnectionID);
            Assert.AreEqual("CustomerA:SRV-SQL01", read[0].ConnectionID);
            Assert.AreEqual("CustomerA:SRV-SQL02", read[1].RepositoryConnectionID);
            Assert.IsNull(read[1].ExistingConnectionID);
        }

        [TestMethod]
        public void RelayRemovesThePrefix()
        {
            var msg = new CollectionMessage { ConnectionID = "CustomerA:SRV-SQL01", CollectAgent = RemoteAgent() };

            var relayed = (CollectionMessage)ConnectionIDPrefix.PrepareForRelay(msg, [PrefixedS3Source()]);

            Assert.AreEqual("SRV-SQL01", relayed.ConnectionID);
            Assert.AreEqual(AgentS3Path, relayed.CollectAgent.S3Path);
            Assert.AreEqual(msg.CollectAgent.ServiceSQSQueueUrl, relayed.CollectAgent.ServiceSQSQueueUrl, "The queue the message is relayed to");
            Assert.AreEqual(msg.Id, relayed.Id);
        }

        /// <summary>
        /// The GUI addresses the message to the registration, which has the prefix on the service name.  The remote
        /// service only accepts a message for its own identity, so the relay removes it.
        /// </summary>
        [TestMethod]
        public void RelayAddressesTheRemoteServiceByItsOwnIdentity()
        {
            var registered = RemoteAgent();
            registered.AgentServiceName = "CustomerA:DBADashService";
            registered.AgentPath = @"C:\DBADash";
            var own = new DBADashAgent { AgentHostName = registered.AgentHostName, AgentServiceName = "DBADashService", AgentPath = registered.AgentPath };
            var msg = new CollectionMessage { ConnectionID = "CustomerA:SRV-SQL01", CollectAgent = registered };

            var relayed = ConnectionIDPrefix.PrepareForRelay(msg, [PrefixedS3Source()]);

            Assert.AreEqual("DBADashService", relayed.CollectAgent.AgentServiceName);
            Assert.AreEqual(own.AgentIdentifier, relayed.CollectAgent.AgentIdentifier);
            Assert.AreNotEqual(registered.AgentIdentifier, relayed.CollectAgent.AgentIdentifier);
            Assert.AreEqual(registered.ServiceSQSQueueUrl, relayed.CollectAgent.ServiceSQSQueueUrl);
        }

        [TestMethod]
        public void RelayLeavesARegistrationWithoutThePrefixOnTheServiceNameAlone()
        {
            // Registered before the prefix was added to the service name
            var msg = new CollectionMessage { ConnectionID = "CustomerA:SRV-SQL01", CollectAgent = RemoteAgent() };
            msg.CollectAgent.AgentServiceName = "DBADashService";

            var relayed = ConnectionIDPrefix.PrepareForRelay(msg, [PrefixedS3Source()]);

            Assert.AreEqual("DBADashService", relayed.CollectAgent.AgentServiceName);
        }

        [TestMethod]
        public void ReplyDataPathMatchesTheSourceItWasRelayedFor()
        {
            var sources = new[] { PrefixedS3Source(), new DBADashSource("s3://dbadash-bucket/customer-b") };
            var handle = Guid.NewGuid();

            Assert.IsTrue(ConnectionIDPrefix.TryMatchMessageDataPath(sources, AgentS3Path.AppendToUrl($"{handle}.message"), out var prefix));
            Assert.AreEqual(Prefix, prefix);

            Assert.IsTrue(ConnectionIDPrefix.TryMatchMessageDataPath(sources, $"s3://dbadash-bucket/customer-b/{handle}.message", out prefix));
            Assert.IsNull(prefix, "A source without a prefix");

            Assert.IsFalse(ConnectionIDPrefix.TryMatchMessageDataPath(sources, $"s3://other-bucket/{handle}.message", out _));
            Assert.IsFalse(ConnectionIDPrefix.TryMatchMessageDataPath(sources, null, out _));
        }

        [TestMethod]
        public void RelayRemovesThePrefixFromListsAndNestedInstances()
        {
            var pause = new CollectionPauseMessage { ConnectionIDs = ["CustomerA:SRV-SQL01", "customera:SRV-SQL02"], CollectAgent = RemoteAgent() };
            var relayedPause = (CollectionPauseMessage)ConnectionIDPrefix.PrepareForRelay(pause, [PrefixedS3Source()]);
            CollectionAssert.AreEqual(new[] { "SRV-SQL01", "SRV-SQL02" }, relayedPause.ConnectionIDs);

            var multi = new MultiCollectionMessage
            {
                Instances = [new CollectionInstance { InstanceID = 1, ConnectionID = "CustomerA:SRV-SQL01" }],
                CollectAgent = RemoteAgent()
            };
            var relayedMulti = (MultiCollectionMessage)ConnectionIDPrefix.PrepareForRelay(multi, [PrefixedS3Source()]);
            Assert.AreEqual("SRV-SQL01", relayedMulti.Instances[0].ConnectionID);
            Assert.AreEqual(1, relayedMulti.Instances[0].InstanceID);
        }

        [TestMethod]
        public void RelayRefusesAnInstanceWithoutThePrefix()
        {
            // An instance from another source - the remote service would be asked about an instance that isn't its own
            var msg = new CollectionMessage { ConnectionID = "CustomerB:SRV-SQL01", CollectAgent = RemoteAgent() };
            Assert.ThrowsExactly<InvalidOperationException>(() => ConnectionIDPrefix.PrepareForRelay(msg, [PrefixedS3Source()]));
        }

        [TestMethod]
        public void RelayRefusesAPrefixedInstanceIfTheRemoteServiceIsNotMatchedToASource()
        {
            var msg = new CollectionMessage { ConnectionID = "CustomerA:SRV-SQL01", CollectAgent = RemoteAgent("s3://other-bucket/path") };
            Assert.ThrowsExactly<InvalidOperationException>(() => ConnectionIDPrefix.PrepareForRelay(msg, [PrefixedS3Source()]));
        }

        [TestMethod]
        public void RelayLeavesAnUnprefixedRemoteServiceAlone()
        {
            var msg = new CollectionMessage { ConnectionID = "SRV-SQL01", CollectAgent = RemoteAgent("s3://other-bucket/path") };
            Assert.AreSame(msg, ConnectionIDPrefix.PrepareForRelay(msg, [PrefixedS3Source(), new DBADashSource("s3://other-bucket/path")]));
        }

        /// <summary>
        /// The remote service is imported through a source without a prefix.  Its ConnectionIDs are its own even when one
        /// starts with another source's prefix, so the message is relayed as it is.
        /// </summary>
        [TestMethod]
        public void RelayLeavesAMatchedUnprefixedSourceAloneWhenAnIDLooksPrefixed()
        {
            var sources = new[]
            {
                new DBADashSource("s3://other-bucket/customer-x") { ConnectionIDPrefix = "SRV-" },
                new DBADashSource(AgentS3Path)
            };
            var msg = new CollectionMessage { ConnectionID = "SRV-SQL01", CollectAgent = RemoteAgent() };
            Assert.AreSame(msg, ConnectionIDPrefix.PrepareForRelay(msg, sources));
        }

        [TestMethod]
        public void RelayIsUnchangedWithoutPrefixes()
        {
            var msg = new CollectionMessage { ConnectionID = "SRV-SQL01", CollectAgent = RemoteAgent() };
            Assert.AreSame(msg, ConnectionIDPrefix.PrepareForRelay(msg, [new DBADashSource(AgentS3Path)]));
        }

        [TestMethod]
        public void S3PathsWrittenDifferentlyMatch()
        {
            var sources = new[] { PrefixedS3Source() };
            Assert.AreEqual(Prefix, ConnectionIDPrefix.ForS3Path(sources, "s3://dbadash-bucket/customer-a"));
            Assert.AreEqual(Prefix, ConnectionIDPrefix.ForS3Path(sources, "s3://dbadash-bucket/customer-a/"));
            Assert.IsNull(ConnectionIDPrefix.ForS3Path(sources, "s3://dbadash-bucket/customer-b"));
            Assert.IsNull(ConnectionIDPrefix.ForS3Path(sources, "s3://dbadash-bucket"));
            Assert.IsNull(ConnectionIDPrefix.ForS3Path(sources, null));

            var minio = new[] { PrefixedS3Source("https://minio.local:9000/dbadash/customer-a") };
            Assert.AreEqual(Prefix, ConnectionIDPrefix.ForS3Path(minio, "https://MINIO.local:9000/dbadash/customer-a/"));
            Assert.IsNull(ConnectionIDPrefix.ForS3Path(minio, "https://minio.local:9001/dbadash/customer-a"));
        }

        [TestMethod]
        public void TwoSourcesForOnePathWithDifferentPrefixesIsAmbiguous()
        {
            var sources = new[] { PrefixedS3Source(), new DBADashSource("s3://dbadash-bucket/customer-a") { ConnectionIDPrefix = "Other:" } };
            Assert.ThrowsExactly<InvalidOperationException>(() => ConnectionIDPrefix.ForS3Path(sources, AgentS3Path));
        }

        /// <summary>
        /// The relay finds the ConnectionIDs in a message or reply payload by property name (ending in ConnectionID or
        /// ConnectionIDs).  An instance named by a property called anything else would cross the relay with the prefix
        /// on it, or without it.  Checks the messaging types and the types they serialize, plus the reply payloads built
        /// outside the messaging namespace (<see cref="ConnectionChanges.ToDataSet"/> takes an object).
        /// </summary>
        [TestMethod]
        public void MessagesOnlyNameInstancesWithPropertiesTheRelayRewrites()
        {
            var assembly = typeof(MessageBase).Assembly;
            var replyPayloads = new[] { typeof(SourceConnectionCheck) };
            var pending = new Queue<Type>(assembly.GetExportedTypes().Where(t => t.Namespace == typeof(MessageBase).Namespace).Concat(replyPayloads));
            var seen = new HashSet<Type>();
            var unexpected = new List<string>();
            while (pending.Count > 0)
            {
                var type = pending.Dequeue();
                if (!seen.Add(type)) continue;
                foreach (var property in type.GetProperties())
                {
                    if (Attribute.IsDefined(property, typeof(JsonIgnoreAttribute))) continue;
                    if (property.Name.Contains("ConnectionID", StringComparison.OrdinalIgnoreCase)
                        && !ConnectionIDPrefix.IsConnectionIDName(property.Name)
                        && !ConnectionIDPrefix.IsConnectionIDsName(property.Name))
                    {
                        unexpected.Add($"{type.Name}.{property.Name}");
                    }
                    // Follow what's serialized with the type - nested classes and the items of lists and dictionaries
                    var types = property.PropertyType.IsGenericType ? property.PropertyType.GetGenericArguments() : [property.PropertyType];
                    foreach (var t in types.Select(t => t.IsArray ? t.GetElementType()! : t).Where(t => t.Assembly == assembly && t.IsClass))
                    {
                        pending.Enqueue(t);
                    }
                }
            }
            Assert.AreEqual(0, unexpected.Count, string.Join(", ", unexpected));
        }
    }
}
