using System;
using System.Linq;
using DBADash.Deadlock.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// Covers the deadlock graph shapes that reach DBA Dash: the extended events form, the
    /// deadlock-list form SSMS saves, an XE event envelope, parallel deadlocks and truncated graphs.
    /// </summary>
    [TestClass]
    public class DeadlockParserTests
    {
        private static DeadlockGraph ParseSingle(string sampleName)
        {
            var graphs = DeadlockParser.Parse(TestGraphs.Load(sampleName));
            Assert.AreEqual(1, graphs.Count, "Expected exactly one deadlock in the sample.");
            return graphs[0];
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_ReadsProcessAttributes()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);

            Assert.AreEqual(2, graph.Processes.Count);

            var victim = graph.Processes.Single(p => p.Spid == 61);
            Assert.AreEqual("process21ec1f0c8", victim.Id);
            Assert.AreEqual(0, victim.Ecid);
            Assert.AreEqual("suspended", victim.Status);
            Assert.AreEqual("CONTOSO\\svc_app", victim.LoginName);
            Assert.AreEqual("APPSRV01", victim.HostName);
            Assert.AreEqual(6120, victim.HostPid);
            Assert.AreEqual("DBA Dash", victim.ClientApp);
            Assert.AreEqual("read committed (2)", victim.IsolationLevel);
            Assert.AreEqual("U", victim.LockMode);
            Assert.AreEqual("KEY: 7:72057594043170816 (8194443284a0)", victim.WaitResource);
            Assert.AreEqual(2, victim.TransactionCount);
            Assert.AreEqual("user_transaction", victim.TransactionName);
            Assert.AreEqual(288L, victim.LogUsed);
            Assert.AreEqual(0, victim.Priority);
            Assert.AreEqual(7, victim.CurrentDatabaseId);
            Assert.AreEqual("Sales", victim.CurrentDatabaseName);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_ReadsWaitTimeAsMilliseconds()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);
            var victim = graph.Processes.Single(p => p.Spid == 61);

            Assert.AreEqual(TimeSpan.FromMilliseconds(1842), victim.WaitTime);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_ReadsTimestampsAsUnspecifiedKind()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);
            var victim = graph.Processes.Single(p => p.Spid == 61);

            Assert.AreEqual(new DateTime(2024, 3, 14, 9, 21, 44, 523), victim.LastTransactionStarted);
            Assert.AreEqual(new DateTime(2024, 3, 14, 9, 21, 44, 523), victim.LastBatchStarted);
            Assert.AreEqual(new DateTime(2024, 3, 14, 9, 21, 44, 520), victim.LastBatchCompleted);

            // Deadlock timestamps carry no time zone, so they must not be silently treated as UTC.
            Assert.AreEqual(DateTimeKind.Unspecified, victim.LastTransactionStarted!.Value.Kind);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_IdentifiesVictimFromVictimList()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);

            Assert.AreEqual(1, graph.Victims.Count);
            Assert.AreEqual(61, graph.Victims[0].Spid);
            Assert.IsTrue(graph.Victims[0].IsVictim);
            Assert.IsFalse(graph.Processes.Single(p => p.Spid == 74).IsVictim);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_ReadsExecutionStack()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);
            var victim = graph.Processes.Single(p => p.Spid == 61);

            Assert.AreEqual(2, victim.ExecutionStack.Count);

            var frame = victim.ExecutionStack[0];
            Assert.AreEqual("Sales.dbo.usp_UpdateOrder", frame.ProcedureName);
            Assert.AreEqual(12, frame.Line);
            Assert.AreEqual(418, frame.StatementStart);
            Assert.AreEqual(712, frame.StatementEnd);
            Assert.AreEqual("0x03000700a1f4e21c", frame.SqlHandle);
            Assert.AreEqual("UPDATE dbo.Orders SET Status = @Status WHERE OrderID = @OrderID;", frame.Sql);

            // stmtstart is absent on the second frame and must come back null rather than zero.
            Assert.IsNull(victim.ExecutionStack[1].StatementStart);
            Assert.AreEqual(88, victim.ExecutionStack[1].StatementEnd);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_TrimsInputBuffer()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);
            var victim = graph.Processes.Single(p => p.Spid == 61);

            Assert.AreEqual(
                "EXEC dbo.usp_UpdateOrder @OrderID = 4711, @Status = 'Shipped';",
                victim.InputBuffer);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_ReadsResources()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);

            Assert.AreEqual(2, graph.Resources.Count);

            var orders = graph.Resources.Single(r => r.ObjectName == "Sales.dbo.Orders");
            Assert.AreEqual(DeadlockResourceType.KeyLock, orders.Type);
            Assert.AreEqual("keylock", orders.TypeName);
            Assert.AreEqual("lock1f4a2b080", orders.Id);
            Assert.AreEqual("PK_Orders", orders.IndexName);
            Assert.AreEqual(7, orders.DatabaseId);
            Assert.AreEqual("X", orders.Mode);
            Assert.AreEqual(72057594043170816L, orders.HobtId);
            Assert.AreEqual(72057594043170816L, orders.AssociatedObjectId);
            Assert.IsFalse(orders.IsParallelismResource);
        }

        [TestMethod]
        public void Parse_ResourceWithSeveralOwners_ResolvesEveryOne()
        {
            var graph = ParseSingle(TestGraphs.SharedLock);
            var customer = graph.Resources.Single(r => r.ObjectName == "Ops.dbo.Customer");

            CollectionAssert.AreEqual(
                new[] { 71, 62 },
                customer.Owners.Select(o => o.Process!.Spid!.Value).ToArray());
            Assert.IsTrue(customer.Owners.All(o => o.Mode == "S"));
            Assert.AreEqual(55, customer.Waiters.Single().Process!.Spid);
        }

        [TestMethod]
        public void Parse_ConversionDeadlock_KeepsAProcessThatIsBothOwnerAndWaiter()
        {
            var graph = ParseSingle(TestGraphs.Conversion);
            var ledger = graph.Resources.Single();

            CollectionAssert.AreEqual(
                new[] { 58, 64 },
                ledger.Owners.Select(o => o.Process!.Spid!.Value).ToArray());
            CollectionAssert.AreEqual(
                new[] { 58, 64 },
                ledger.Waiters.Select(w => w.Process!.Spid!.Value).ToArray());
            Assert.IsTrue(ledger.Waiters.All(w => w.RequestType == "convert"));
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_ResolvesOwnersAndWaitersToProcesses()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);
            var orders = graph.Resources.Single(r => r.ObjectName == "Sales.dbo.Orders");

            var owner = orders.Owners.Single();
            Assert.AreEqual("process21ec24988", owner.ProcessId);
            Assert.IsNotNull(owner.Process);
            Assert.AreEqual(74, owner.Process!.Spid);
            Assert.AreEqual("X", owner.Mode);

            var waiter = orders.Waiters.Single();
            Assert.AreEqual("process21ec1f0c8", waiter.ProcessId);
            Assert.IsNotNull(waiter.Process);
            Assert.AreEqual(61, waiter.Process!.Spid);
            Assert.AreEqual("U", waiter.Mode);
            Assert.AreEqual("wait", waiter.RequestType);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_IsNotParallel()
        {
            Assert.IsFalse(ParseSingle(TestGraphs.KeyLock).IsParallel);
        }

        [TestMethod]
        public void Parse_KeyLockDeadlock_CapturesSourceXml()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);

            StringAssert.StartsWith(graph.Xml, "<deadlock>");
            StringAssert.Contains(graph.Xml, "Sales.dbo.Orders");
        }

        [TestMethod]
        public void Parse_DeadlockListWrapper_IsAccepted()
        {
            // The .xdl form SSMS saves wraps the graph in deadlock-list.  A root-element check for
            // "deadlock" rejects this, which is the gap this parser closes.
            var graph = ParseSingle(TestGraphs.DeadlockListWrapper);

            Assert.AreEqual(2, graph.Processes.Count);
            Assert.AreEqual(2, graph.Resources.Count);
        }

        [TestMethod]
        public void Parse_DeadlockListWrapper_ReadsVictimFromAttribute()
        {
            var graph = ParseSingle(TestGraphs.DeadlockListWrapper);

            Assert.AreEqual(1, graph.Victims.Count);
            Assert.AreEqual(55, graph.Victims[0].Spid);
            Assert.IsTrue(graph.Victims[0].IsVictim);
        }

        [TestMethod]
        public void Parse_DeadlockListWrapper_ReadsPageLocks()
        {
            var graph = ParseSingle(TestGraphs.DeadlockListWrapper);

            Assert.IsTrue(graph.Resources.All(r => r.Type == DeadlockResourceType.PageLock));

            var stock = graph.Resources.Single(r => r.ObjectName == "Warehouse.dbo.Stock");
            Assert.AreEqual(6, stock.DatabaseId);
            Assert.AreEqual("FULL", stock.Attributes["subresource"]);
            Assert.AreEqual("2145", stock.Attributes["pageid"]);
            Assert.AreEqual(1, stock.FileId);
            Assert.AreEqual(2145L, stock.PageId);

            // A page lock is named by its page - db:file:page, as the waitresource attribute writes it -
            // because every page lock of one table carries the same object name.  The object is still
            // available, just not as the name.
            Assert.AreEqual("6:1:2145", stock.PageKey);
            Assert.AreEqual("Page 6:1:2145", stock.DisplayName);
            Assert.AreEqual("Warehouse.dbo.Stock", stock.ObjectDisplayName);
        }

        [TestMethod]
        public void Parse_XeEventEnvelope_FindsNestedDeadlock()
        {
            var graph = ParseSingle(TestGraphs.XeEventEnvelope);

            Assert.AreEqual(2, graph.Processes.Count);
            Assert.AreEqual(1, graph.Victims.Count);
            Assert.AreEqual(102, graph.Victims[0].Spid);
            Assert.IsTrue(graph.Resources.All(r => r.Type == DeadlockResourceType.ObjectLock));

            // Only the deadlock element is captured, not the surrounding event envelope.
            StringAssert.StartsWith(graph.Xml, "<deadlock>");
        }

        [TestMethod]
        public void Parse_ParallelDeadlock_IsDetectedAsParallel()
        {
            var graph = ParseSingle(TestGraphs.Parallel);

            Assert.IsTrue(graph.IsParallel);
            Assert.IsTrue(graph.Resources.All(r => r.Type == DeadlockResourceType.ExchangeEvent));
            Assert.IsTrue(graph.Resources.All(r => r.IsParallelismResource));
        }

        [TestMethod]
        public void Parse_ParallelDeadlock_KeepsProcessesWithSharedSpidSeparate()
        {
            var graph = ParseSingle(TestGraphs.Parallel);

            Assert.AreEqual(2, graph.Processes.Count);
            Assert.IsTrue(graph.Processes.All(p => p.Spid == 88));
            CollectionAssert.AreEquivalent(
                new[] { 0, 3 },
                graph.Processes.Select(p => p.Ecid!.Value).ToArray());
        }

        [TestMethod]
        public void Parse_ParallelDeadlock_KeepsUnmodelledResourceAttributes()
        {
            var graph = ParseSingle(TestGraphs.Parallel);
            var pipe = graph.Resources.First();

            // exchangeEvent has no object name, so the raw element name is the display name and the
            // detail that matters lives in the attributes.
            Assert.AreEqual("exchangeEvent", pipe.DisplayName);
            Assert.AreEqual("e_waitPipeNewRow", pipe.Attributes["WaitType"]);
            Assert.AreEqual("7", pipe.Attributes["nodeId"]);
        }

        [TestMethod]
        public void Parse_MultipleDeadlocks_ReturnsAllInDocumentOrder()
        {
            var graphs = DeadlockParser.Parse(TestGraphs.Load(TestGraphs.Multiple));

            Assert.AreEqual(2, graphs.Count);
            CollectionAssert.AreEqual(
                new[] { 201, 202 },
                graphs[0].Processes.Select(p => p.Spid!.Value).ToArray());
            CollectionAssert.AreEqual(
                new[] { 301, 302 },
                graphs[1].Processes.Select(p => p.Spid!.Value).ToArray());

            Assert.AreEqual(201, graphs[0].Victims.Single().Spid);
            Assert.AreEqual(302, graphs[1].Victims.Single().Spid);
        }

        [TestMethod]
        public void Parse_MultipleDeadlocks_CapturesXmlPerDeadlock()
        {
            var graphs = DeadlockParser.Parse(TestGraphs.Load(TestGraphs.Multiple));

            StringAssert.Contains(graphs[0].Xml, "Alpha.dbo.T1");
            Assert.IsFalse(graphs[0].Xml.Contains("Bravo.dbo.T2", StringComparison.Ordinal));
            StringAssert.Contains(graphs[1].Xml, "Bravo.dbo.T2");
        }

        [TestMethod]
        public void Parse_TruncatedGraph_LeavesUnresolvableParticipantProcessNull()
        {
            // The system_health ring buffer can drop processes, leaving owner/waiter entries that
            // reference a process not in the process-list.  That must not throw or be dropped.
            var graph = ParseSingle(TestGraphs.Truncated);
            var rid = graph.Resources.Single(r => r.Type == DeadlockResourceType.RidLock);

            var owner = rid.Owners.Single();
            Assert.AreEqual("processMissing", owner.ProcessId);
            Assert.IsNull(owner.Process);

            var waiter = rid.Waiters.Single();
            Assert.AreEqual("processPresent", waiter.ProcessId);
            Assert.IsNotNull(waiter.Process);
        }

        [TestMethod]
        public void Parse_TruncatedGraph_IgnoresVictimIdNotInProcessList()
        {
            var graph = ParseSingle(TestGraphs.Truncated);

            Assert.AreEqual(0, graph.Victims.Count);
            Assert.IsFalse(graph.Processes.Any(p => p.IsVictim));
        }

        [TestMethod]
        public void Parse_TruncatedGraph_KeepsUnrecognisedResourceType()
        {
            var graph = ParseSingle(TestGraphs.Truncated);
            var unknown = graph.Resources.Single(r => r.Type == DeadlockResourceType.Unknown);

            Assert.AreEqual("somethingNewInSql2030", unknown.TypeName);
            Assert.AreEqual("somethingNewInSql2030", unknown.DisplayName);
            Assert.AreEqual("42", unknown.Attributes["customAttribute"]);
            Assert.AreEqual(1, unknown.Owners.Count);
            Assert.AreEqual(0, unknown.Waiters.Count, "A missing waiter-list should yield an empty list.");
        }

        [TestMethod]
        public void Parse_TruncatedGraph_YieldsNullForAbsentAttributes()
        {
            var graph = ParseSingle(TestGraphs.Truncated);
            var process = graph.Processes.Single();

            Assert.IsNull(process.LoginName);
            Assert.IsNull(process.HostName);
            Assert.IsNull(process.WaitTime);
            Assert.IsNull(process.LastBatchStarted);
            Assert.IsNull(process.CurrentDatabaseName);
            Assert.AreEqual(0, process.ExecutionStack.Count);
        }

        // ---------------------------------------------------------------- one lock, one resource

        /// <summary>
        /// SQL Server writes the resource-list as a list of edges: a page several sessions are queued
        /// on appears once per owner/waiter pairing, all sharing a lock id.  Left alone that is one box
        /// per pairing on the graph and one row per pairing in the grid, all identical.
        /// </summary>
        [TestMethod]
        public void Parse_RepeatedEntriesForOneLock_BecomeOneResource()
        {
            var graph = ParseSingle(TestGraphs.ParallelPageLocks);

            // Four entries in the XML; the two sharing lock2a71b400 are the same page.
            Assert.AreEqual(3, graph.Resources.Count);

            var page900 = graph.Resources.Single(r => r.PageId == 900);
            Assert.AreEqual("lock2a71b400", page900.Id);

            // The owners and waiters of every entry, gathered onto the one resource.
            CollectionAssert.AreEquivalent(
                new[] { "process6b2a7c4" },
                page900.Owners.Select(o => o.ProcessId).ToArray());
            CollectionAssert.AreEquivalent(
                new[] { "process6b2a1c0", "process6b2a4e8" },
                page900.Waiters.Select(w => w.ProcessId).ToArray());
        }

        /// <summary>
        /// A process listed twice as the owner of one lock, in the same mode, is one owner - and one
        /// arrow.  Drawing it twice says nothing the first arrow did not.
        /// </summary>
        [TestMethod]
        public void Parse_RepeatedOwnerEntries_AreCollapsed()
        {
            var graph = ParseSingle(TestGraphs.ParallelPageLocks);

            var page900 = graph.Resources.Single(r => r.PageId == 900);

            Assert.AreEqual(1, page900.Owners.Count);
            Assert.AreEqual("U", page900.Owners[0].Mode);
        }

        /// <summary>
        /// Owners in different modes are different owners, so they survive.  Merging on the process
        /// alone would lose the mode that says what kind of deadlock this is.
        /// </summary>
        [TestMethod]
        public void Parse_OwnerEntriesInDifferentModes_AreKept()
        {
            var graph = DeadlockParser.Parse("""
                <deadlock>
                  <process-list>
                    <process id="p1" spid="55" />
                  </process-list>
                  <resource-list>
                    <keylock objectname="Ops.dbo.T" id="lock1" mode="X">
                      <owner-list>
                        <owner id="p1" mode="S" />
                        <owner id="p1" mode="U" />
                      </owner-list>
                    </keylock>
                  </resource-list>
                </deadlock>
                """)[0];

            CollectionAssert.AreEquivalent(
                new[] { "S", "U" },
                graph.Resources[0].Owners.Select(o => o.Mode).ToArray());
        }

        /// <summary>
        /// Nothing says two entries without a lock id are the same lock, so they are left as written.
        /// </summary>
        [TestMethod]
        public void Parse_EntriesWithoutALockId_AreNotMerged()
        {
            var graph = DeadlockParser.Parse("""
                <deadlock>
                  <process-list>
                    <process id="p1" spid="55" />
                  </process-list>
                  <resource-list>
                    <waitfor WaitType="e_waitLockTimeout">
                      <owner-list><owner id="p1" /></owner-list>
                    </waitfor>
                    <waitfor WaitType="e_waitLockTimeout">
                      <owner-list><owner id="p1" /></owner-list>
                    </waitfor>
                  </resource-list>
                </deadlock>
                """)[0];

            Assert.AreEqual(2, graph.Resources.Count);
        }

        [TestMethod]
        public void FindProcess_ReturnsNullForUnknownId()
        {
            var graph = ParseSingle(TestGraphs.KeyLock);

            Assert.IsNotNull(graph.FindProcess("process21ec1f0c8"));
            Assert.IsNull(graph.FindProcess("nope"));
            Assert.IsNull(graph.FindProcess(null));
        }
    }
}
