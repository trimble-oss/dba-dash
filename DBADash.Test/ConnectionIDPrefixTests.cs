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
        /// The relay finds the ConnectionIDs in a message by property name (ConnectionID, ConnectionIDs).  An instance
        /// named by a property called anything else would be relayed with the prefix still on it.
        /// </summary>
        [TestMethod]
        public void MessagesOnlyNameInstancesWithPropertiesTheRelayRewrites()
        {
            var unexpected = typeof(MessageBase).Assembly.GetExportedTypes()
                .Where(t => t.Namespace == typeof(MessageBase).Namespace)
                .SelectMany(t => t.GetProperties().Select(p => (Type: t, Property: p)))
                .Where(x => x.Property.Name.Contains("ConnectionID", StringComparison.OrdinalIgnoreCase)
                            && x.Property.Name is not ("ConnectionID" or "ConnectionIDs"))
                .Select(x => $"{x.Type.Name}.{x.Property.Name}")
                .ToList();
            Assert.AreEqual(0, unexpected.Count, string.Join(", ", unexpected));
        }
    }
}
