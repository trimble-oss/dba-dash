using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DBADash.AvailabilityGroups;
using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class AGHealthEventCollectorTests
    {
        private static readonly Guid GroupId = Guid.Parse("4a0b6f7e-3c2d-4f5e-9a8b-1c2d3e4f5a6b");
        private static readonly Guid ReplicaId = Guid.Parse("9f8e7d6c-5b4a-4c3d-8e2f-1a0b9c8d7e6f");

        private static string StateChangeXml(string timestamp = "2026-10-07T13:00:03.1234567Z") =>
            $@"<event name=""availability_replica_state_change"" package=""sqlserver"" timestamp=""{timestamp}"">
  <data name=""current_state""><type name=""hadr_ag_replica_state"" package=""sqlserver"" /><value>1</value><text><![CDATA[PRIMARY_NORMAL]]></text></data>
  <data name=""previous_state""><type name=""hadr_ag_replica_state"" package=""sqlserver"" /><value>3</value><text><![CDATA[PRIMARY_PENDING]]></text></data>
  <data name=""availability_group_id""><type name=""guid_ptr"" package=""package0"" /><value>{GroupId}</value></data>
  <data name=""availability_group_name""><type name=""unicode_string"" package=""package0"" /><value><![CDATA[AG1]]></value></data>
  <data name=""availability_replica_id""><type name=""guid_ptr"" package=""package0"" /><value>{ReplicaId}</value></data>
  <data name=""availability_replica_name""><type name=""unicode_string"" package=""package0"" /><value><![CDATA[SQL02]]></value></data>
</event>";

        private const string DDLXml =
            @"<event name=""alwayson_ddl_executed"" package=""sqlserver"" timestamp=""2026-10-07T14:00:00.000Z"">
  <data name=""ddl_action""><type name=""alwayson_ddl_action"" package=""sqlserver"" /><value>2</value><text><![CDATA[alter]]></text></data>
  <data name=""ddl_phase""><type name=""ddl_opcode"" package=""sqlserver"" /><value>1</value><text><![CDATA[commit]]></text></data>
  <data name=""availability_group_name""><type name=""unicode_string"" package=""package0"" /><value><![CDATA[AG1]]></value></data>
  <data name=""statement""><type name=""unicode_string"" package=""package0"" /><value><![CDATA[ALTER AVAILABILITY GROUP [AG1] FORCE_FAILOVER_ALLOW_DATA_LOSS]]></value></data>
</event>";

        private const string ErrorXml =
            @"<event name=""error_reported"" package=""sqlserver"" timestamp=""2026-10-07T13:00:00.300Z"">
  <data name=""error_number""><type name=""int32"" package=""package0"" /><value>19407</value></data>
  <data name=""severity""><type name=""int32"" package=""package0"" /><value>16</value></data>
  <data name=""message""><type name=""unicode_string"" package=""package0"" /><value><![CDATA[The lease between availability group 'AG1' and the Windows Server Failover Cluster has expired.]]></value></data>
</event>";

        private static DataRow AddSingle(string xml)
        {
            var dt = AGHealthEventCollector.CreateTable();
            AGHealthEventCollector.AddEvent(dt, xml, new HashSet<string>());
            Assert.AreEqual(1, dt.Rows.Count);
            return dt.Rows[0];
        }

        [TestMethod]
        public void StateChange_UsesMappedTextAndIds()
        {
            var row = AddSingle(StateChangeXml());

            Assert.AreEqual(AGHealthEventCollector.ReplicaStateChangeEvent, row["EventName"]);
            Assert.AreEqual(new DateTime(2026, 10, 7, 13, 0, 3, 123), row["EventTime"]);
            Assert.AreEqual("PRIMARY_PENDING", row["PreviousState"]);
            Assert.AreEqual("PRIMARY_NORMAL", row["CurrentState"]);
            Assert.AreEqual(GroupId, row["group_id"]);
            Assert.AreEqual(ReplicaId, row["replica_id"]);
            Assert.AreEqual("AG1", row["AvailabilityGroupName"]);
            Assert.AreEqual("SQL02", row["ReplicaName"]);
            Assert.AreEqual(32, ((byte[])row["EventHash"]).Length);
        }

        [TestMethod]
        public void DDL_CapturesStatementAndPhase()
        {
            var row = AddSingle(DDLXml);

            Assert.AreEqual("ALTER", row["DDLAction"]);
            Assert.AreEqual("COMMIT", row["DDLPhase"]);
            StringAssert.Contains((string)row["Details"], "FORCE_FAILOVER_ALLOW_DATA_LOSS");
            Assert.AreEqual(DBNull.Value, row["group_id"]);
            Assert.AreEqual(DBNull.Value, row["CurrentState"]);
        }

        [TestMethod]
        public void ErrorReported_CapturesNumberAndMessage()
        {
            var row = AddSingle(ErrorXml);

            Assert.AreEqual(19407, row["ErrorNumber"]);
            StringAssert.StartsWith((string)row["Details"], "The lease between");
            Assert.AreEqual(DBNull.Value, row["AvailabilityGroupName"]);
        }

        [TestMethod]
        public void ServerDiagnostics_SummarizesComponentAttributes()
        {
            const string xml =
                @"<event name=""sp_server_diagnostics_component_result"" package=""sqlserver"" timestamp=""2026-10-07T13:00:00.050Z"">
  <data name=""component""><type name=""sp_server_diagnostics_component"" package=""sqlserver"" /><value>2</value><text><![CDATA[QUERY_PROCESSING]]></text></data>
  <data name=""state""><type name=""sp_server_diagnostics_state"" package=""sqlserver"" /><value>3</value><text><![CDATA[ERROR]]></text></data>
  <data name=""data""><type name=""xml"" package=""package0"" /><value><queryProcessing maxWorkers=""512"" workersCreated=""512"" hasUnresolvableDeadlockOccurred=""0"" hasDeadlockedSchedulersOccurred=""1""><blockingTasks><blocked-process-report /></blockingTasks></queryProcessing></value></data>
</event>";
            var row = AddSingle(xml);

            Assert.AreEqual("QUERY_PROCESSING", row["Component"]);
            Assert.AreEqual("ERROR", row["CurrentState"]);
            Assert.AreEqual("queryProcessing: maxWorkers=512, workersCreated=512, hasUnresolvableDeadlockOccurred=0, hasDeadlockedSchedulersOccurred=1", row["Details"]);
        }

        [TestMethod]
        public void ReplicaStartStopAndManagerState_UpperCaseState()
        {
            var stop = AddSingle(
                $@"<event name=""availability_replica_state"" package=""sqlserver"" timestamp=""2026-10-07T13:00:00.000Z"">
  <data name=""current_state""><value>1</value><text><![CDATA[Stopping]]></text></data>
  <data name=""availability_group_id""><value>{GroupId}</value></data>
  <data name=""availability_group_name""><value><![CDATA[AG1]]></value></data>
</event>");
            Assert.AreEqual("STOPPING", stop["CurrentState"]);
            Assert.AreEqual(GroupId, stop["group_id"]);

            var manager = AddSingle(
                @"<event name=""availability_replica_manager_state_change"" package=""sqlserver"" timestamp=""2026-10-07T13:00:00.000Z"">
  <data name=""current_state""><value>2</value><text><![CDATA[OFFLINE]]></text></data>
</event>");
            Assert.AreEqual("OFFLINE", manager["CurrentState"]);
        }

        [TestMethod]
        public void DatabaseSyncState_UsesAlternateIdNames()
        {
            var agDatabaseId = Guid.NewGuid();
            var row = AddSingle(
                $@"<event name=""hadr_db_partner_set_sync_state"" package=""sqlserver"" timestamp=""2026-10-07T13:00:00.000Z"">
  <data name=""database_id""><value>7</value></data>
  <data name=""commit_policy""><value>0</value><text><![CDATA[DoNothing]]></text></data>
  <data name=""commit_policy_target""><value>2</value><text><![CDATA[WaitForHarden]]></text></data>
  <data name=""sync_state""><value>1</value><text><![CDATA[NOT]]></text></data>
  <data name=""group_id""><value>{GroupId}</value></data>
  <data name=""replica_id""><value>{ReplicaId}</value></data>
  <data name=""ag_database_id""><value>{agDatabaseId}</value></data>
</event>");

            Assert.AreEqual(7, row["database_id"]);
            Assert.AreEqual("NOT", row["CurrentState"]);
            Assert.AreEqual(GroupId, row["group_id"]);
            Assert.AreEqual(ReplicaId, row["replica_id"]);
            Assert.AreEqual(agDatabaseId, row["group_database_id"]);
            Assert.AreEqual("commit_policy=DoNothing, commit_policy_target=WaitForHarden", row["Details"]);
        }

        [TestMethod]
        public void SameEventTwice_IsAddedOnce()
        {
            var dt = AGHealthEventCollector.CreateTable();
            var seen = new HashSet<string>();
            AGHealthEventCollector.AddEvent(dt, StateChangeXml(), seen);
            AGHealthEventCollector.AddEvent(dt, StateChangeXml(), seen);
            AGHealthEventCollector.AddEvent(dt, StateChangeXml("2026-10-07T13:00:04.000Z"), seen);

            Assert.AreEqual(2, dt.Rows.Count);
        }

        [TestMethod]
        public void MalformedOrUntimedEvents_AreSkipped()
        {
            var dt = AGHealthEventCollector.CreateTable();
            var seen = new HashSet<string>();
            AGHealthEventCollector.AddEvent(dt, "<event name=\"error_reported\"", seen);
            AGHealthEventCollector.AddEvent(dt, "<event name=\"error_reported\"><data name=\"error_number\"><value>1</value></data></event>", seen);

            Assert.AreEqual(0, dt.Rows.Count);
        }

        [TestMethod]
        public void LongDetails_AreTruncatedToColumnWidth()
        {
            var xml = ErrorXml.Replace("The lease between", new string('x', 5000));
            var row = AddSingle(xml);

            Assert.AreEqual(4000, ((string)row["Details"]).Length);
        }

        /// <summary>Table-valued parameters bind by ordinal, so the DataTable must match dbo.AGHealthEvents column for column.</summary>
        [TestMethod]
        public void TableMatchesTableType()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DBADash.sln"))) dir = dir.Parent;
            if (dir == null) Assert.Inconclusive("Repository root (the directory containing DBADash.sln) was not found.");

            var path = Path.Combine(dir.FullName, "DBADashDB", "dbo", "User Defined Types", "AGHealthEvents.sql");
            var expected = DeadlockTableContractTests.ParseTableTypeColumns(File.ReadAllText(path));
            var actual = AGHealthEventCollector.CreateTable().Columns.Cast<DataColumn>().Select(c => c.ColumnName);

            Assert.AreEqual(string.Join(", ", expected), string.Join(", ", actual));
        }

        [TestMethod]
        public void NothingToCollect_IsSkippedUntilRecheck_AndLoggedOnChangeOnly()
        {
            var key = "skip-" + Guid.NewGuid();
            Assert.IsFalse(AGHealthEventCollector.IsSkipped(key));

            Assert.IsTrue(AGHealthEventCollector.RecordStatus(key, AGHealthEventCollector.CollectionStatus.NoAvailabilityGroups));
            Assert.IsTrue(AGHealthEventCollector.IsSkipped(key));

            // Same status on the next check - nothing new to report
            Assert.IsFalse(AGHealthEventCollector.RecordStatus(key, AGHealthEventCollector.CollectionStatus.NoAvailabilityGroups));

            // An AG was created but the session isn't running - a different reason, so reported
            Assert.IsTrue(AGHealthEventCollector.RecordStatus(key, AGHealthEventCollector.CollectionStatus.SessionUnavailable));
            Assert.IsTrue(AGHealthEventCollector.IsSkipped(key));

            // Collected - no longer skipped
            Assert.IsTrue(AGHealthEventCollector.RecordStatus(key, AGHealthEventCollector.CollectionStatus.Collected));
            Assert.IsFalse(AGHealthEventCollector.IsSkipped(key));
            Assert.IsFalse(AGHealthEventCollector.RecordStatus(key, AGHealthEventCollector.CollectionStatus.Collected));
        }

        [TestMethod]
        public void CursorStore_SerializeRoundTrips()
        {
            var positions = new[]
            {
                new KeyValuePair<string, FileTargetCursor>("conn1", new FileTargetCursor(@"C:\Log\AlwaysOn_health_0_1.xel", 1024, 3)),
                new KeyValuePair<string, FileTargetCursor>("conn2", FileTargetCursor.None)
            };

            var restored = AGHealthEventCursorStore.Deserialize(AGHealthEventCursorStore.Serialize(positions));

            Assert.AreEqual(1, restored.Count);
            Assert.AreEqual(@"C:\Log\AlwaysOn_health_0_1.xel", restored["conn1"].FileName);
            Assert.AreEqual(1024, restored["conn1"].Offset);
            Assert.AreEqual(3, restored["conn1"].ConsumedAtOffset);
        }
    }
}
