using DBADash;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The connection level settings that decide whether deadlocks are collected and which session is read.
    ///
    /// <para>The session name is the only mode switch there is - see
    /// <see cref="DBADashSource.DeadlockXESessionName"/> - so what it holds decides whether the collection
    /// runs at all, whether DBA Dash may create the session, and whether it may stop one.  Everything here
    /// is pure, so none of it needs an instance to run against.</para>
    /// </summary>
    [TestClass]
    public class DeadlockConfigurationTests
    {
        private const string SqlConnection = "Data Source=SQL1;Integrated Security=SSPI";

        private static DBADashSource Source(string? sessionName, string connectionString = SqlConnection) =>
            new()
            {
                SourceConnection = new DBADashConnection(connectionString),
                DeadlockXESessionName = sessionName
            };

        [TestMethod]
        public void BlankSessionName_SwitchesTheCollectionOff()
        {
            // Null, empty and whitespace are the same thing.  A config written before the collection existed
            // has no value here, and gets the collection switched off rather than an accidental default.
            foreach (var blank in new[] { null, "", "   " })
            {
                var source = Source(blank);
                Assert.AreEqual(string.Empty, source.DeadlockXESessionName, "Blank normalizes to empty");
                Assert.IsFalse(source.IsDeadlockCollectionEnabled);
                Assert.IsFalse(source.IsDeadlockXESessionManaged);
                Assert.IsTrue(source.IsCollectionDisabledByConfiguration(CollectionType.Deadlocks));
            }
        }

        [TestMethod]
        public void SessionName_IsTrimmed()
        {
            // A name pasted with a trailing space would otherwise be read as a session of its own and never
            // be recognised as the managed one.
            var source = Source("  DBADash_Deadlocks  ");

            Assert.AreEqual(DBADashSource.ManagedDeadlockXESessionName, source.DeadlockXESessionName);
            Assert.IsTrue(source.IsDeadlockXESessionManaged);
        }

        [TestMethod]
        [DataRow("DBADash_Deadlocks")]
        [DataRow("dbadash_deadlocks")]
        [DataRow("DBADASH_DEADLOCKS")]
        public void ReservedName_IsRecognisedWhateverItsCase(string sessionName)
        {
            // Session names are not case sensitive on the instance, so a name that differs only by case is
            // the same session - and DBA Dash would otherwise refuse to create or start it.
            var source = Source(sessionName);

            Assert.IsTrue(source.IsDeadlockCollectionEnabled);
            Assert.IsTrue(source.IsDeadlockXESessionManaged, "The reserved name is the one DBA Dash owns");
            Assert.IsFalse(source.IsCollectionDisabledByConfiguration(CollectionType.Deadlocks));
        }

        [TestMethod]
        [DataRow("system_health")]
        [DataRow("MyOwnDeadlockSession")]
        public void AnyOtherName_IsReadButNeverAltered(string sessionName)
        {
            // The distinction that stops DBA Dash stopping a session someone else relies on.
            var source = Source(sessionName);

            Assert.AreEqual(sessionName, source.DeadlockXESessionName);
            Assert.IsTrue(source.IsDeadlockCollectionEnabled);
            Assert.IsFalse(source.IsDeadlockXESessionManaged);
        }

        [TestMethod]
        public void NonSqlConnection_HasNoSessionToRead()
        {
            // A folder or bucket source has no instance and no extended events session, so the collection is
            // off however the value was set.
            var source = Source(DBADashSource.ManagedDeadlockXESessionName, @"C:\Dump");

            Assert.AreEqual(string.Empty, source.DeadlockXESessionName);
            Assert.IsFalse(source.IsDeadlockCollectionEnabled);
            Assert.IsFalse(source.FlushDeadlockXERingBuffer);
        }

        [TestMethod]
        public void FlushRingBuffer_IsOffByDefault_AndOnlyForSqlConnections()
        {
            // Off by default because emptying the buffer can lose a deadlock that has fired but not yet
            // reached the target.
            Assert.IsFalse(Source(DBADashSource.ManagedDeadlockXESessionName).FlushDeadlockXERingBuffer);

            var sql = Source(DBADashSource.ManagedDeadlockXESessionName);
            sql.FlushDeadlockXERingBuffer = true;
            Assert.IsTrue(sql.FlushDeadlockXERingBuffer);

            var folder = Source(DBADashSource.ManagedDeadlockXESessionName, @"C:\Dump");
            folder.FlushDeadlockXERingBuffer = true;
            Assert.IsFalse(folder.FlushDeadlockXERingBuffer, "Nothing to flush without an instance");
        }

        [TestMethod]
        public void SlowQueryThreshold_IsTheOffSwitchForSlowQueries()
        {
            // The other collection configuration alone can switch off, and the reason
            // IsCollectionDisabledByConfiguration takes a type rather than answering only for deadlocks.
            var source = Source(string.Empty);

            source.SlowQueryThresholdMs = -1;
            Assert.IsFalse(source.IsSlowQueryCollectionEnabled);
            Assert.IsTrue(source.IsCollectionDisabledByConfiguration(CollectionType.SlowQueries));

            source.SlowQueryThresholdMs = 0;
            Assert.IsTrue(source.IsSlowQueryCollectionEnabled, "Zero captures everything - it is not off");
            Assert.IsFalse(source.IsCollectionDisabledByConfiguration(CollectionType.SlowQueries));

            source.SlowQueryThresholdMs = 1000;
            Assert.IsTrue(source.IsSlowQueryCollectionEnabled);
        }

        [TestMethod]
        public void OnlyTheTwoConfigurableCollections_CanBeDisabledByConfiguration()
        {
            // Every other collection is decided by its schedule alone, and reporting one as disabled here
            // would hide it from the repository's overdue reporting for no reason.
            var source = Source(string.Empty);
            source.SlowQueryThresholdMs = -1;

            Assert.IsFalse(source.IsCollectionDisabledByConfiguration(CollectionType.CPU));
            Assert.IsFalse(source.IsCollectionDisabledByConfiguration(CollectionType.Databases));
            Assert.IsTrue(source.IsCollectionDisabledByConfiguration(CollectionType.Deadlocks));
            Assert.IsTrue(source.IsCollectionDisabledByConfiguration(CollectionType.SlowQueries));
        }
    }
}
