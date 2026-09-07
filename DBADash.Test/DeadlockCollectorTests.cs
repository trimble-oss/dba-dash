using System;
using System.Data;
using System.Linq;
using DBADash;
using DBADash.Deadlocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The shred pipeline: XE event XML in, the three repository tables out.  Everything here is pure, so
    /// none of it needs an instance to run against.
    /// </summary>
    [TestClass]
    public class DeadlockCollectorTests
    {
        /// <summary>
        /// A realistic xml_deadlock_report: two sessions taking the same two objects in opposite orders, one
        /// chosen as the victim.  Written out in full rather than reduced to the attributes under test, so
        /// that the parser is exercised on the shape it will really see.
        /// </summary>
        private const string DeadlockEvent = """
<event name="xml_deadlock_report" package="sqlserver" timestamp="2025-06-19T11:04:58.412Z">
  <data name="xml_report">
    <type name="xml" package="package0" />
    <value>
      <deadlock>
        <victim-list>
          <victimProcess id="processb1d0c8" />
        </victim-list>
        <process-list>
          <process id="processb1d0c8" taskpriority="0" logused="248" waitresource="KEY: 11:72057594045595648 (61a06abd401c)" waittime="2011" transactionname="user_transaction" lasttranstarted="2025-06-19T11:04:56.390" XDES="0x1a2b3c" lockMode="U" schedulerid="3" kpid="8140" status="suspended" spid="102" sbid="0" ecid="0" priority="0" trancount="2" lastbatchstarted="2025-06-19T11:04:56.390" lastbatchcompleted="2025-06-19T11:04:56.387" clientapp="Billing" hostname="BILLSRV01" hostpid="4120" loginname="CONTOSO\svc_bill" isolationlevel="read committed (2)" xactid="1899234" currentdb="11" currentdbname="Billing" lockTimeout="4294967295" clientoption1="671088672" clientoption2="128056">
            <executionStack>
              <frame procname="Billing.dbo.usp_PostInvoice" line="12" stmtstart="278" stmtend="490" sqlhandle="0x03000b00a1b2c3d4">
UPDATE dbo.Invoice SET Status = 'Posted' WHERE InvoiceID = @InvoiceID;    </frame>
            </executionStack>
            <inputbuf>
EXEC dbo.usp_PostInvoice @InvoiceID = 88231;   </inputbuf>
          </process>
          <process id="processb1e440" taskpriority="0" logused="1104" waitresource="KEY: 11:72057594045661184 (7c9f12ab3e01)" waittime="2007" transactionname="user_transaction" lasttranstarted="2025-06-19T11:04:56.393" XDES="0x4d5e6f" lockMode="U" schedulerid="5" kpid="9004" status="suspended" spid="117" sbid="0" ecid="0" priority="0" trancount="2" lastbatchstarted="2025-06-19T11:04:56.393" lastbatchcompleted="2025-06-19T11:04:56.390" clientapp="Billing" hostname="BILLSRV02" hostpid="7788" loginname="CONTOSO\svc_bill" isolationlevel="read committed (2)" xactid="1899241" currentdb="11" currentdbname="Billing" lockTimeout="4294967295" clientoption1="671088672" clientoption2="128056">
            <executionStack>
              <frame procname="Billing.dbo.usp_ReverseInvoice" line="9" stmtstart="204" stmtend="418" sqlhandle="0x03000b00f1e2d3c4">
UPDATE dbo.InvoiceLine SET Amount = 0 WHERE InvoiceID = @InvoiceID;    </frame>
            </executionStack>
            <inputbuf>
EXEC dbo.usp_ReverseInvoice @InvoiceID = 88232;   </inputbuf>
          </process>
        </process-list>
        <resource-list>
          <keylock hobtid="72057594045595648" dbid="11" objectname="Billing.dbo.Invoice" indexname="PK_Invoice" id="lockb2c1a00" mode="X" associatedObjectId="72057594045595648">
            <owner-list>
              <owner id="processb1e440" mode="X" />
            </owner-list>
            <waiter-list>
              <waiter id="processb1d0c8" mode="U" requestType="wait" />
            </waiter-list>
          </keylock>
          <keylock hobtid="72057594045661184" dbid="11" objectname="Billing.dbo.InvoiceLine" indexname="PK_InvoiceLine" id="lockb2c1b80" mode="X" associatedObjectId="72057594045661184">
            <owner-list>
              <owner id="processb1d0c8" mode="X" />
            </owner-list>
            <waiter-list>
              <waiter id="processb1e440" mode="U" requestType="wait" />
            </waiter-list>
          </keylock>
        </resource-list>
      </deadlock>
    </value>
  </data>
</event>
""";

        private static readonly DateTime ExpectedEventTime = new(2025, 6, 19, 11, 4, 58, 412);

        [TestMethod]
        public void ShredsHeaderFromEvent()
        {
            var result = DeadlockCollector.ShredEvents(new[] { DeadlockEvent });

            Assert.AreEqual(1, result.GraphCount);
            var row = result.Deadlocks.Rows[0];

            Assert.AreEqual(ExpectedEventTime, (DateTime)row["EventTime"], "EventTime comes from the event envelope.");
            Assert.AreEqual((short)2, row["ProcessCount"]);
            Assert.AreEqual((short)1, row["VictimCount"]);
            Assert.AreEqual((short)2, row["ResourceCount"]);
            Assert.AreEqual(false, row["IsParallel"]);
            Assert.AreEqual(DeadlockTables.DeadlockHashBytes, ((byte[])row["DeadlockHash"]).Length);
            Assert.AreEqual((byte)1, row["SignatureVersion"]);
            StringAssert.StartsWith((string)row["Signature"], "0x", "Stored as the hex form the _Upd proc converts.");
        }

        [TestMethod]
        public void CompressedGraphRoundTrips()
        {
            var result = DeadlockCollector.ShredEvents(new[] { DeadlockEvent });
            var compressed = (byte[])result.Deadlocks.Rows[0]["DeadlockXmlCompressed"];

            var xml = SMOBaseClass.Unzip(compressed);
            StringAssert.Contains(xml, "<deadlock>");
            StringAssert.Contains(xml, "Billing.dbo.Invoice");
            Assert.IsTrue(compressed.Length < xml.Length,
                "Compression should be a saving even before the UTF-16 doubling is accounted for.");
        }

        [TestMethod]
        public void ShredsProcessesWithVictimAndModule()
        {
            var result = DeadlockCollector.ShredEvents(new[] { DeadlockEvent });
            var rows = result.Processes.Rows.Cast<DataRow>().ToList();

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(1, rows.Count(r => (bool)r["IsVictim"]), "The graph names exactly one victim.");

            var victim = rows.Single(r => (bool)r["IsVictim"]);
            Assert.AreEqual(102, victim["SPID"]);
            Assert.AreEqual("Billing.dbo.usp_PostInvoice", victim["ProcedureName"]);
            Assert.AreEqual("CONTOSO\\svc_bill", victim["LoginName"]);
            Assert.AreEqual("BILLSRV01", victim["HostName"]);
            Assert.AreEqual("Billing", victim["ClientApp"]);
            Assert.AreEqual(11, victim["database_id"]);
            Assert.AreEqual("U", victim["LockMode"]);
            Assert.AreEqual(248L, victim["LogUsed"]);
            Assert.AreEqual(2011L, victim["WaitTimeMs"]);
            StringAssert.Contains((string)victim["StatementText"], "UPDATE dbo.Invoice");
            StringAssert.StartsWith((string)victim["WaitResource"], "KEY: 11:");

            // Every process row must carry the header's key or it cannot be joined back to it.
            Assert.IsTrue(rows.All(r => (DateTime)r["EventTime"] == ExpectedEventTime));
            var headerHash = (byte[])result.Deadlocks.Rows[0]["DeadlockHash"];
            Assert.IsTrue(rows.All(r => ((byte[])r["DeadlockHash"]).SequenceEqual(headerHash)));
            CollectionAssert.AreEquivalent(new[] { (short)0, (short)1 },
                rows.Select(r => (short)r["ProcessIndex"]).ToArray());
        }

        [TestMethod]
        public void ShredsTheDiagnosticFieldsSpBlitzLockSurfaces()
        {
            var result = DeadlockCollector.ShredEvents(new[] { DeadlockEvent });
            var victim = result.Processes.Rows.Cast<DataRow>().Single(r => (bool)r["IsVictim"]);

            // Raw bitmasks - collected as-is, decoded into SET option names at report time.
            Assert.AreEqual(671088672, victim["ClientOption1"]);
            Assert.AreEqual(128056, victim["ClientOption2"]);

            Assert.AreEqual("suspended", victim["Status"]);
            Assert.AreEqual(2, victim["TransactionCount"]);
            Assert.AreEqual(4120, victim["HostPid"]);
            Assert.AreEqual(new DateTime(2025, 6, 19, 11, 4, 56, 390), victim["LastTransactionStarted"]);

            // The batch as submitted, kept alongside the statement that actually deadlocked.
            StringAssert.Contains((string)victim["InputBuffer"], "EXEC dbo.usp_PostInvoice");
            StringAssert.Contains((string)victim["StatementText"], "UPDATE dbo.Invoice");
        }

        [TestMethod]
        public void MissingClientOptionsAreNullRatherThanZero()
        {
            // Zero is a meaningful bitmask (every option off); absent must not be recorded as that.
            var noOptions = DeadlockEvent
                .Replace(" clientoption1=\"671088672\"", string.Empty)
                .Replace(" clientoption2=\"128056\"", string.Empty);

            var result = DeadlockCollector.ShredEvents(new[] { noOptions });
            var victim = result.Processes.Rows.Cast<DataRow>().Single(r => (bool)r["IsVictim"]);

            Assert.AreEqual(DBNull.Value, victim["ClientOption1"]);
            Assert.AreEqual(DBNull.Value, victim["ClientOption2"]);
        }

        [TestMethod]
        public void ShredsResourcesWithModes()
        {
            var result = DeadlockCollector.ShredEvents(new[] { DeadlockEvent });
            var rows = result.Resources.Rows.Cast<DataRow>().ToList();

            Assert.AreEqual(2, rows.Count);
            var invoice = rows.Single(r => (string)r["ObjectName"] == "Billing.dbo.Invoice");

            Assert.AreEqual("keylock", invoice["ResourceType"]);
            Assert.AreEqual("PK_Invoice", invoice["IndexName"]);
            Assert.AreEqual(11, invoice["database_id"]);
            Assert.AreEqual("X", invoice["LockMode"]);
            Assert.AreEqual("X", invoice["OwnerModes"]);
            Assert.AreEqual("U", invoice["WaiterModes"]);
            Assert.AreEqual((short)1, invoice["OwnerCount"]);
            Assert.AreEqual((short)1, invoice["WaiterCount"]);
            Assert.AreEqual(false, invoice["IsParallelismResource"]);
        }

        [TestMethod]
        public void HashIsStableAcrossIndentationDifferences()
        {
            // The same event reaching us with different indentation must hash the same, or the repository
            // would store one deadlock twice.  Only the whitespace between elements is changed here - the
            // text inside <inputbuf> and <frame> is the statement itself, so altering it would be a genuine
            // change of content rather than of formatting.
            var reformatted = string.Join("\n", DeadlockEvent.Split('\n')
                .Select(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*<[^>]*>\s*$")
                    ? "\t\t" + line
                    : line));

            Assert.AreNotEqual(DeadlockEvent, reformatted, "The transform should have changed something.");

            var a = (byte[])DeadlockCollector.ShredEvents(new[] { DeadlockEvent }).Deadlocks.Rows[0]["DeadlockHash"];
            var b = (byte[])DeadlockCollector.ShredEvents(new[] { reformatted }).Deadlocks.Rows[0]["DeadlockHash"];

            CollectionAssert.AreEqual(a, b);
        }

        [TestMethod]
        public void HashDiffersForADifferentOccurrence()
        {
            // Same pattern, different occurrence: the transaction ids and spids move.  These share a
            // signature by design, so only the hash can keep them apart.
            var second = DeadlockEvent.Replace("xactid=\"1899234\"", "xactid=\"1899999\"")
                .Replace("spid=\"102\"", "spid=\"143\"");

            var first = DeadlockCollector.ShredEvents(new[] { DeadlockEvent }).Deadlocks.Rows[0];
            var other = DeadlockCollector.ShredEvents(new[] { second }).Deadlocks.Rows[0];

            CollectionAssert.AreNotEqual((byte[])first["DeadlockHash"], (byte[])other["DeadlockHash"]);
            Assert.AreEqual(first["Signature"], other["Signature"],
                "The signature identifies the pattern, so it must not move with the occurrence.");
        }

        [TestMethod]
        public void DuplicateEventsInOneBatchAreCollapsed()
        {
            // The table types are keyed on (EventTime, DeadlockHash), so a duplicate reaching the server
            // would fail the whole import rather than be skipped.
            var result = DeadlockCollector.ShredEvents(new[] { DeadlockEvent, DeadlockEvent });

            Assert.AreEqual(1, result.GraphCount);
            Assert.AreEqual(2, result.Processes.Rows.Count);
            Assert.AreEqual(2, result.Resources.Rows.Count);
        }

        [TestMethod]
        public void RingBufferSuppressesWhatThePreviousReadReturned()
        {
            var targetData = $"<RingBufferTarget truncated=\"0\">{DeadlockEvent}</RingBufferTarget>";
            var state = new DeadlockCollectionState();

            Assert.AreEqual(1, DeadlockCollector.ShredRingBufferTargetData(targetData, state).GraphCount,
                "First read returns what the buffer holds.");
            Assert.AreEqual(0, DeadlockCollector.ShredRingBufferTargetData(targetData, state).GraphCount,
                "An unchanged buffer must not resend its contents every run.");
        }

        [TestMethod]
        public void RingBufferSeenSetIsBoundedByTheBuffer()
        {
            var state = new DeadlockCollectionState();
            DeadlockCollector.ShredRingBufferTargetData(
                $"<RingBufferTarget>{DeadlockEvent}</RingBufferTarget>", state);
            Assert.AreEqual(1, state.SeenHashes.Count);

            // Once the graph has aged out of the buffer it must leave the set too, or the set grows for the
            // life of the service.
            DeadlockCollector.ShredRingBufferTargetData("<RingBufferTarget />", state);
            Assert.AreEqual(0, state.SeenHashes.Count);
        }

        [TestMethod]
        public void RingBufferSizeDefaultsAndIsClampedToWhatTheTargetTakes()
        {
            // The value reaches the session's CREATE/ALTER as a literal, so an out-of-range one would either
            // fail the statement or build a buffer whose target_data comes back truncated - which loses the
            // whole read rather than part of it.
            Assert.AreEqual(CollectionConfig.DefaultDeadlockXERingBufferKB,
                new CollectionConfig().GetDeadlockXERingBufferKB(), "Unset uses the default.");

            Assert.AreEqual(CollectionConfig.MinDeadlockXERingBufferKB,
                new CollectionConfig { DeadlockXERingBufferKB = 1 }.GetDeadlockXERingBufferKB());

            Assert.AreEqual(CollectionConfig.MaxDeadlockXERingBufferKB,
                new CollectionConfig { DeadlockXERingBufferKB = 999999 }.GetDeadlockXERingBufferKB());

            Assert.AreEqual(2048, new CollectionConfig { DeadlockXERingBufferKB = 2048 }.GetDeadlockXERingBufferKB(),
                "A value in range is used as given.");
        }

        [TestMethod]
        public void RingBufferSeenSetCountsWhatTheBufferHoldsNotWhatWasReturned()
        {
            // The decision to empty the buffer after reading it is taken on this count, not on the number of
            // graphs the read returned - a buffer full of deadlocks already stored is exactly the case the
            // flush exists for, and it returns nothing new.
            var targetData = $"<RingBufferTarget truncated=\"0\">{DeadlockEvent}</RingBufferTarget>";
            var state = new DeadlockCollectionState();

            DeadlockCollector.ShredRingBufferTargetData(targetData, state);
            var second = DeadlockCollector.ShredRingBufferTargetData(targetData, state);

            Assert.AreEqual(0, second.GraphCount, "Nothing new to store.");
            Assert.AreEqual(1, state.SeenHashes.Count, "But the buffer still holds a graph, so it is worth emptying.");
        }

        [TestMethod]
        public void NonDeadlockRingBufferEventsAreIgnored()
        {
            // system_health carries far more than deadlocks; only the deadlock reports are ours.
            var targetData =
                "<RingBufferTarget>" +
                "<event name=\"sp_server_diagnostics_component_result\" timestamp=\"2025-06-19T11:00:00.000Z\">" +
                "<data name=\"data\"><value><system>lots of xml</system></value></data></event>" +
                DeadlockEvent +
                "</RingBufferTarget>";

            var result = DeadlockCollector.ShredRingBufferTargetData(targetData, new DeadlockCollectionState());

            Assert.AreEqual(1, result.GraphCount);
        }

        /// <summary>
        /// The Azure SQL Database shape: a database scoped session raises
        /// <c>database_xml_deadlock_report</c>, whose envelope carries the cycle id and database name
        /// alongside the same graph.  Built from the on-premises event so the two differ only in the
        /// envelope, which is the thing under test.
        /// </summary>
        private static readonly string AzureDeadlockEvent = DeadlockEvent
            .Replace("<event name=\"xml_deadlock_report\"",
                "<event name=\"database_xml_deadlock_report\"")
            .Replace("  <data name=\"xml_report\">",
                "  <data name=\"deadlock_cycle_id\"><type name=\"int32\" package=\"package0\" /><value>3</value></data>\r\n" +
                "  <data name=\"database_name\"><type name=\"unicode_string\" package=\"package0\" /><value>Billing</value></data>\r\n" +
                "  <data name=\"xml_report\">");

        [TestMethod]
        public void AzureDatabaseScopedDeadlockEventIsShredded()
        {
            // Azure SQL Database has no server scoped session, so this is the only event a deadlock arrives
            // under there.  Reading it must produce the same rows as the on-premises event.
            var result = DeadlockCollector.ShredRingBufferTargetData(
                $"<RingBufferTarget truncated=\"0\">{AzureDeadlockEvent}</RingBufferTarget>",
                new DeadlockCollectionState());

            Assert.AreEqual(1, result.GraphCount);
            Assert.AreEqual(ExpectedEventTime, result.Deadlocks.Rows[0]["EventTime"]);
            Assert.AreEqual(2, result.Processes.Rows.Count);
            Assert.AreEqual(2, result.Resources.Rows.Count);
        }

        [TestMethod]
        public void AzureAndOnPremisesEnvelopesOfTheSameGraphHashAlike()
        {
            // The hash is taken from the graph, not the envelope, so an instance read through either event
            // name identifies the same deadlock the same way.
            var onPrem = DeadlockCollector.ShredEvents(new[] { DeadlockEvent });
            var azure = DeadlockCollector.ShredEvents(new[] { AzureDeadlockEvent });

            CollectionAssert.AreEqual((byte[])onPrem.Deadlocks.Rows[0]["DeadlockHash"],
                (byte[])azure.Deadlocks.Rows[0]["DeadlockHash"]);
        }

        [TestMethod]
        public void UnparseableEventIsSkippedRatherThanFailingTheBatch()
        {
            // One bad graph must not cost us the good ones collected in the same run.
            var result = DeadlockCollector.ShredEvents(new[] { "not xml at all", DeadlockEvent });

            Assert.AreEqual(1, result.GraphCount);
        }

        [TestMethod]
        public void EventWithoutATimestampIsSkipped()
        {
            // EventTime is part of the key, so a graph without one cannot be stored.
            var noTimestamp = DeadlockEvent.Replace(" timestamp=\"2025-06-19T11:04:58.412Z\"", string.Empty);

            Assert.AreEqual(0, DeadlockCollector.ShredEvents(new[] { noTimestamp }).GraphCount);
        }

        [TestMethod]
        public void EmptyInputProducesEmptyTablesRatherThanNull()
        {
            // The collection sends its tables even when it found nothing, so the import can advance the
            // collection date and the instance isn't reported as overdue for not deadlocking.
            var result = DeadlockCollector.ShredEvents(Array.Empty<string>());

            Assert.AreEqual(0, result.GraphCount);
            Assert.IsNotNull(result.Processes);
            Assert.IsNotNull(result.Resources);
            Assert.AreEqual(DeadlockTables.DeadlocksTableName, result.Deadlocks.TableName);
        }
    }
}
