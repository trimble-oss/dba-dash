using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Test
{
    /// <summary>
    /// Adding and removing connections from the GUI: encrypting connection strings for the service, the gate on the
    /// service, and the checks made on a message before anything is changed.  No service, instance or config file needed.
    /// </summary>
    [TestClass]
    public class ConnectionMessagesTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext _) => MessageEncryption.Initialize();

        [TestMethod]
        public void Encryption_RoundTrips()
        {
            const string cs = "Data Source=SQL1;User ID=dba;Password=S3cret!;Encrypt=True";
            var encrypted = MessageEncryption.Encrypt(cs, MessageEncryption.PublicKey);

            Assert.IsTrue(MessageEncryption.IsEncrypted(encrypted));
            Assert.DoesNotContain("S3cret", encrypted);
            Assert.AreNotEqual(encrypted, MessageEncryption.Encrypt(cs, MessageEncryption.PublicKey)); // Random key and nonce
            Assert.AreEqual(cs, MessageEncryption.Decrypt(encrypted));
        }

        [TestMethod]
        public void Encryption_OtherKeyCantBeDecrypted()
        {
            using var other = RSA.Create(3072);
            var encrypted = MessageEncryption.Encrypt("Data Source=SQL1", Convert.ToBase64String(other.ExportSubjectPublicKeyInfo()));
            Assert.ThrowsExactly<CryptographicException>(() => MessageEncryption.Decrypt(encrypted));
        }

        [TestMethod]
        public void Encryption_TamperedValueRejected()
        {
            var encrypted = MessageEncryption.Encrypt("Data Source=SQL1", MessageEncryption.PublicKey);
            var tampered = encrypted[..^4] + (encrypted[^4] == 'A' ? "B" : "A") + encrypted[^3..];
            Assert.ThrowsExactly<CryptographicException>(() => MessageEncryption.Decrypt(tampered));
            Assert.ThrowsExactly<ArgumentException>(() => MessageEncryption.Decrypt("Data Source=SQL1"));
            Assert.ThrowsExactly<ArgumentException>(() => MessageEncryption.Encrypt("Data Source=SQL1", null));
        }

        [TestMethod]
        public void CreateSource_OnlySqlConnections()
        {
            var options = new SourceConnectionOptions();
            var src = ConnectionChanges.CreateSource(MessageEncryption.Encrypt("SQL1", MessageEncryption.PublicKey), options);
            StringAssert.Contains(src.SourceConnection.ConnectionString, "Data Source=SQL1");

            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.CreateSource(MessageEncryption.Encrypt("C:\\Temp\\Import", MessageEncryption.PublicKey), options));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.CreateSource(MessageEncryption.Encrypt("s3://bucket/path", MessageEncryption.PublicKey), options));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.CreateSource("SQL1", options)); // Must be encrypted
        }

        [TestMethod]
        public void ValidateOptions_NoCustomCollections()
        {
            ConnectionChanges.ValidateOptions(new SourceConnectionOptions());
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateOptions(new SourceConnectionOptions
            {
                CustomCollections = new() { { "Test", new CustomCollection { ProcedureName = "dbo.Test" } } }
            }));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateOptions(new SourceConnectionOptions
            {
                SlowQueryThresholdMs = 1000,
                SlowQueryCaptureMode = DBADashSource.SlowQueryCaptureModes.ExistingSession
            }));
        }

        [TestMethod]
        public void ValidateCount()
        {
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateCount(0));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateCount(ConnectionChanges.MaxConnectionsPerMessage + 1));
            ConnectionChanges.ValidateCount(1);
        }

        [TestMethod]
        public async Task Messages_RejectedUnlessEnabledAndStarted()
        {
            var messages = new MessageBase[]
            {
                new ConnectionTestMessage { ConnectionStrings = new List<string> { "x" } },
                new ConnectionAddMessage { Connections = new List<ConnectionAddMessage.ConnectionRequest> { new() } },
                new ConnectionRemoveMessage { ConnectionIDs = new List<string> { "SQL1" } },
                new ConnectionOptionsGetMessage { ConnectionID = "SQL1" },
                new ConnectionOptionsUpdateMessage { ConnectionIDs = new List<string> { "SQL1" } }
            };
            foreach (var message in messages)
            {
                var disabled = await Assert.ThrowsExactlyAsync<Exception>(() => message.Process(new CollectionConfig { AcceptsSourceChanges = true }, Guid.Empty, CancellationToken.None));
                StringAssert.Contains(disabled.Message, "not enabled", message.GetType().Name);

                var starting = await Assert.ThrowsExactlyAsync<Exception>(() => message.Process(new CollectionConfig { AllowConnectionChanges = true }, Guid.Empty, CancellationToken.None));
                StringAssert.Contains(starting.Message, "still starting", message.GetType().Name);
            }
        }

        [TestMethod]
        public void ConnectionChangesAllowScheduleChanges()
        {
            Assert.IsFalse(new CollectionConfig().IsScheduleChangesAllowed);
            Assert.IsTrue(new CollectionConfig { AllowScheduleChanges = true }.IsScheduleChangesAllowed);
            Assert.IsTrue(new CollectionConfig { AllowConnectionChanges = true }.IsScheduleChangesAllowed);
        }

        [TestMethod]
        public void FindInFileConfig_MatchesConnectionIDOrRunningConnectionString()
        {
            var withID = new DBADashSource("Data Source=SQL1;Integrated Security=True") { ConnectionID = "SQL1" };
            var withoutID = new DBADashSource("Data Source=SQL2;Integrated Security=True");
            var fileConfig = new CollectionConfig();
            fileConfig.SourceConnections.AddRange(new[] { withID, withoutID });

            // The running source has its ConnectionID generated at runtime
            var running = new CollectionConfig();
            running.SourceConnections.Add(new DBADashSource("Data Source=SQL2;Integrated Security=True") { ConnectionID = "SQL2\\GENERATED" });

            Assert.AreSame(withID, ConnectionRemoveMessage.FindInFileConfig(fileConfig, running, "sql1"));
            Assert.AreSame(withoutID, ConnectionRemoveMessage.FindInFileConfig(fileConfig, running, "SQL2\\GENERATED"));
            Assert.IsNull(ConnectionRemoveMessage.FindInFileConfig(fileConfig, running, "SQL3"));
            // Not the source saved without a ConnectionID
            Assert.IsNull(ConnectionRemoveMessage.FindInFileConfig(fileConfig, running, null!));
            Assert.IsNull(ConnectionRemoveMessage.FindInFileConfig(fileConfig, running, " "));
        }

        [TestMethod]
        public async Task CollectionActivity_WaitsForRunningCollections()
        {
            var src = new DBADashSource("Data Source=ActivityTest;Integrated Security=True");
            var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<DBADashSource>(Newtonsoft.Json.JsonConvert.SerializeObject(src))!; // As a collection job carries
            Assert.IsTrue(await CollectionActivity.WaitForIdleAsync(new[] { src }, TimeSpan.Zero, CancellationToken.None));

            var first = CollectionActivity.Begin(copy);
            var second = CollectionActivity.Begin(src);
            Assert.IsTrue(CollectionActivity.IsRunning(new[] { src }));
            Assert.IsFalse(await CollectionActivity.WaitForIdleAsync(new[] { src }, TimeSpan.FromMilliseconds(300), CancellationToken.None));

            first.Dispose();
            first.Dispose(); // Only counted once
            Assert.IsTrue(CollectionActivity.IsRunning(new[] { src }));
            var wait = CollectionActivity.WaitForIdleAsync(new[] { src }, TimeSpan.FromSeconds(10), CancellationToken.None);
            second.Dispose();
            Assert.IsTrue(await wait);
        }

        [TestMethod]
        public void IsCollected_MatchesCopiesWithTheSameConnectionString()
        {
            var cfg = new CollectionConfig();
            var src = new DBADashSource("Data Source=SQL1;Integrated Security=True");
            cfg.SourceConnections.Add(src);
            var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<DBADashSource>(Newtonsoft.Json.JsonConvert.SerializeObject(src))!;

            Assert.IsTrue(cfg.IsCollected(src));
            Assert.IsTrue(cfg.IsCollected(copy));
            Assert.IsFalse(cfg.IsCollected(new DBADashSource("Data Source=SQL1;User ID=dba;Password=new")));
            cfg.SourceConnections.Clear();
            Assert.IsFalse(cfg.IsCollected(copy));
        }

        [TestMethod]
        public void DerivedConnectionString_TakesTheParentsWithTheChildsDatabaseAndIntent()
        {
            var parent = new DBADashSource("Data Source=az.database.windows.net;Initial Catalog=master;User ID=dba;Password=NewPassword");
            var db = new DBADashSource("Data Source=az.database.windows.net;Initial Catalog=DB1;User ID=dba;Password=OldPassword");
            var replica = new DBADashSource("Data Source=az.database.windows.net;Initial Catalog=DB1;User ID=dba;Password=OldPassword;ApplicationIntent=ReadOnly");

            var dbCs = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionAddMessage.GetDerivedConnectionString(parent, db));
            Assert.AreEqual("DB1", dbCs.InitialCatalog);
            Assert.AreEqual("NewPassword", dbCs.Password);
            Assert.AreEqual(Microsoft.Data.SqlClient.ApplicationIntent.ReadWrite, dbCs.ApplicationIntent);

            var replicaCs = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionAddMessage.GetDerivedConnectionString(parent, replica));
            Assert.AreEqual(Microsoft.Data.SqlClient.ApplicationIntent.ReadOnly, replicaCs.ApplicationIntent);
            Assert.AreEqual("NewPassword", replicaCs.Password);
        }

        [TestMethod]
        public void RepositoryConnectionID_IsTheRetainedCustomID()
        {
            Assert.AreEqual("Generated", new SourceConnectionCheck { ConnectionID = "Generated" }.RepositoryConnectionID);
            Assert.AreEqual("Custom", new SourceConnectionCheck { ConnectionID = "Generated", Existing = ExistingConnectionStates.Configured, ExistingConnectionID = "Custom" }.RepositoryConnectionID);
            // A discovered database isn't kept - it's added with its own ID
            Assert.AreEqual("Generated", new SourceConnectionCheck { ConnectionID = "Generated", Existing = ExistingConnectionStates.Discovered, ExistingConnectionID = "Other" }.RepositoryConnectionID);
        }

        [TestMethod]
        public void ValidateConnectionIDs_RejectsBlank()
        {
            ConnectionChanges.ValidateConnectionIDs(new List<string> { "SQL1" });
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateConnectionIDs(new List<string> { "SQL1", null! }));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateConnectionIDs(new List<string> { "" }));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateConnectionIDs(new List<string>()));
            Assert.ThrowsExactly<ArgumentException>(() => ConnectionChanges.ValidateConnectionIDs(null!));
        }

        [TestMethod]
        public async Task RemoveAndUpdate_RejectBlankConnectionIDs()
        {
            var cfg = new CollectionConfig { AllowConnectionChanges = true, AcceptsSourceChanges = true };
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => new ConnectionRemoveMessage { ConnectionIDs = new List<string> { null! } }.Process(cfg, Guid.Empty, CancellationToken.None));
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => new ConnectionOptionsUpdateMessage { ConnectionIDs = new List<string> { "" } }.Process(cfg, Guid.Empty, CancellationToken.None));
        }

        [TestMethod]
        public async Task NewConnectionsGetAConnectionID()
        {
            // Saved with the ConnectionID even when the connection can't be validated - the data source
            var cfg = new CollectionConfig { Destination = "Data Source=Repo;Integrated Security=True;Initial Catalog=DBADashDB" };
            var src = new SourceConnectionOptions().CreateSource("Data Source=SQL9;Integrated Security=True");
            var check = await SourceConnectionAdder.CheckAsync(cfg, src, validate: false);
            SourceConnectionAdder.Apply(cfg, src, check, new SourceConnectionDecisions());
            Assert.AreEqual("SQL9", cfg.SourceConnections.Single().ConnectionID);
        }

        private static SourceConnectionOptions SlowQueries(DBADashSource.SlowQueryCaptureModes mode, int threshold = 1000, string deadlocks = "") =>
            new() { SlowQueryThresholdMs = threshold, SlowQueryCaptureMode = mode, SlowQueryXESessionName = mode == DBADashSource.SlowQueryCaptureModes.ExistingSession ? "Mine" : null, DeadlockXESessionName = deadlocks };

        [TestMethod]
        public void SessionsToDrop_RingBuffer()
        {
            var ring = SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer);
            CollectionAssert.AreEqual(Array.Empty<string>(), ConnectionOptionsUpdateMessage.GetSessionsToDrop(ring, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer)));
            CollectionAssert.AreEqual(new[] { "DBADash_1", "DBADash_2" }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(ring, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer, 2000)));
            CollectionAssert.AreEqual(new[] { "DBADash_1", "DBADash_2" }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(ring, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer, -1)));
            CollectionAssert.AreEqual(new[] { "DBADash_1", "DBADash_2" }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(ring, SlowQueries(DBADashSource.SlowQueryCaptureModes.EventFile)));
        }

        [TestMethod]
        public void SessionsToDrop_EventFileKeptUnlessNoLongerUsed()
        {
            var file = SlowQueries(DBADashSource.SlowQueryCaptureModes.EventFile);
            // A changed definition is rebuilt by the collection, keeping its unread events
            CollectionAssert.AreEqual(Array.Empty<string>(), ConnectionOptionsUpdateMessage.GetSessionsToDrop(file, SlowQueries(DBADashSource.SlowQueryCaptureModes.EventFile, 5000)));
            CollectionAssert.AreEqual(new[] { DBADashSource.ManagedSlowQueryXESessionName }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(file, SlowQueries(DBADashSource.SlowQueryCaptureModes.EventFile, -1)));
            CollectionAssert.AreEqual(new[] { DBADashSource.ManagedSlowQueryXESessionName }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(file, SlowQueries(DBADashSource.SlowQueryCaptureModes.ExistingSession)));
        }

        [TestMethod]
        public void SessionsToDrop_EventFileIsRingBufferOnAzure()
        {
            // Event file mode uses the ring buffer sessions on Azure SQL Database and Managed Instance
            var file = SlowQueries(DBADashSource.SlowQueryCaptureModes.EventFile);
            var off = SlowQueries(DBADashSource.SlowQueryCaptureModes.EventFile, -1);
            CollectionAssert.AreEqual(new[] { "DBADash_1", "DBADash_2" }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(file, off, isAzureDB: true));
            CollectionAssert.AreEqual(new[] { "DBADash_1", "DBADash_2" }, ConnectionOptionsUpdateMessage.GetSessionsToDrop(file, off, isManagedInstance: true));
            // Switching between the two is no change there
            CollectionAssert.AreEqual(Array.Empty<string>(), ConnectionOptionsUpdateMessage.GetSessionsToDrop(file, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer), isAzureDB: true));
        }

        [TestMethod]
        public void ConfiguredConnectionIDs_ResolvesIDsGeneratedAtRuntime()
        {
            var fileConfig = new CollectionConfig();
            fileConfig.SourceConnections.Add(new DBADashSource("Data Source=AZ1;Initial Catalog=master;Integrated Security=True") { ConnectionID = "AZ1|master" });
            // Saved without a ConnectionID - an Azure DB configured explicitly
            fileConfig.SourceConnections.Add(new DBADashSource("Data Source=AZ1;Initial Catalog=DB1;Integrated Security=True"));
            var running = new List<DBADashSource>
            {
                new("Data Source=AZ1;Initial Catalog=master;Integrated Security=True") { ConnectionID = "AZ1|master" },
                new("Data Source=AZ1;Initial Catalog=DB1;Integrated Security=True") { ConnectionID = "AZ1|DB1" },
                new("Data Source=AZ1;Initial Catalog=DB2;Integrated Security=True") { ConnectionID = "AZ1|DB2" } // Discovered
            };

            var configured = ConnectionChanges.GetConfiguredConnectionIDs(fileConfig, running);
            CollectionAssert.AreEquivalent(new[] { "AZ1|master", "AZ1|DB1" }, configured.ToList());
            // So only the discovered database is derived from master
            var derived = CollectionScheduleUpdateMessage.GetDerivedSources(running, "AZ1|master", configured);
            CollectionAssert.AreEqual(new[] { "AZ1|DB2" }, derived.Select(d => d.ConnectionID).ToList());
        }

        [TestMethod]
        public void SessionsToDrop_NeverTheUsersOwn()
        {
            var existing = SlowQueries(DBADashSource.SlowQueryCaptureModes.ExistingSession, deadlocks: "MyDeadlocks");
            CollectionAssert.AreEqual(Array.Empty<string>(), ConnectionOptionsUpdateMessage.GetSessionsToDrop(existing, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer, -1)));
        }

        [TestMethod]
        public void SessionsToDrop_ManagedDeadlockSession()
        {
            var managed = SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer, -1, DBADashSource.ManagedDeadlockXESessionName);
            CollectionAssert.AreEqual(Array.Empty<string>(), ConnectionOptionsUpdateMessage.GetSessionsToDrop(managed, managed));
            CollectionAssert.AreEqual(new[] { DBADashSource.ManagedDeadlockXESessionName },
                ConnectionOptionsUpdateMessage.GetSessionsToDrop(managed, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer, -1, DBADashSource.SystemHealthXESessionName)));
            CollectionAssert.AreEqual(new[] { DBADashSource.ManagedDeadlockXESessionName },
                ConnectionOptionsUpdateMessage.GetSessionsToDrop(managed, SlowQueries(DBADashSource.SlowQueryCaptureModes.RingBuffer, -1, string.Empty)));
        }

        [TestMethod]
        public void ApplyTo_KeepsWhatIsntAnOption()
        {
            var src = new DBADashSource("Data Source=SQL1;Integrated Security=True")
            {
                ConnectionID = "CustomID",
                SlowQueryEventFileMaxSizeMB = 77,
                FlushDeadlockXERingBuffer = true,
                CustomCollections = new() { { "Test", new CustomCollection { ProcedureName = "dbo.Test" } } },
                CollectionSchedules = new DBADashService.CollectionSchedules { { CollectionType.Jobs, new DBADashService.CollectionSchedule { Schedule = "600" } } }
            };
            new SourceConnectionOptions { SlowQueryThresholdMs = 500, RunningQueryLiveProfileThreshold = LiveProfileThreshold.DefaultThreshold, RunningQueryPlanThreshold = PlanCollectionThreshold.DefaultThreshold }
                .ApplyTo(src, includeCustomCollections: false);

            Assert.AreEqual(500, src.SlowQueryThresholdMs);
            Assert.IsNotNull(src.RunningQueryLiveProfileThreshold);
            Assert.AreEqual("CustomID", src.ConnectionID);
            Assert.AreEqual(77, src.SlowQueryEventFileMaxSizeMB);
            Assert.IsTrue(src.FlushDeadlockXERingBuffer);
            Assert.AreEqual(1, src.CustomCollections.Count);
            Assert.AreEqual("600", src.CollectionSchedules[CollectionType.Jobs].Schedule);
        }

        [TestMethod]
        public void ReplyPayload_RoundTrips()
        {
            var results = new List<ConnectionChangeResult> { new() { ConnectionID = "SQL1", Outcome = ConnectionChangeResult.Added } };
            var ds = ConnectionChanges.ToDataSet(results, new[] { "A warning" });

            var read = ConnectionChanges.FromDataSet<List<ConnectionChangeResult>>(ds);
            Assert.AreEqual("SQL1", read[0].ConnectionID);
            Assert.IsTrue(read[0].IsChange);
            CollectionAssert.AreEqual(new[] { "A warning" }, MessageWarnings.Read(ds));
            Assert.IsNull(ConnectionChanges.FromDataSet<List<ConnectionChangeResult>>(null));
        }
    }
}
