using System.Linq;
using DBADash.Deadlock.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// The signature groups occurrences of one deadlock.  These tests are mostly about what it
    /// deliberately ignores: everything that differs between two occurrences of the same problem.
    /// </summary>
    [TestClass]
    public class DeadlockSignatureTests
    {
        private static string SignatureOf(string xml) =>
            DeadlockSignature.Compute(DeadlockParser.Parse(xml).First()).Value;

        private static string SampleSignature(string sampleName) =>
            DeadlockSignature.Compute(DeadlockParser.Parse(TestGraphs.Load(sampleName)).First()).Value;

        /// <summary>Two processes deadlocking over two keys, with everything occurrence-specific parameterised.</summary>
        private static string TwoProcessDeadlock(
            string spid1 = "61",
            string spid2 = "74",
            string host = "APPSRV01",
            string login = "CONTOSO\\svc_app",
            string waitTime = "1842",
            string orderId = "4711",
            string xactId = "1002748") =>
            $"""
             <deadlock>
               <victim-list><victimProcess id="p1" /></victim-list>
               <process-list>
                 <process id="p1" spid="{spid1}" hostname="{host}" loginname="{login}" waittime="{waitTime}"
                          xactid="{xactId}" lasttranstarted="2024-03-14T09:21:44.523" currentdbname="Sales">
                   <executionStack>
                     <frame procname="Sales.dbo.usp_UpdateOrder" line="12" sqlhandle="0x03000700a1f4e21c">
             UPDATE dbo.Orders SET Status = 'Shipped' WHERE OrderID = {orderId};    </frame>
                   </executionStack>
                 </process>
                 <process id="p2" spid="{spid2}" hostname="BATCHSRV02" waittime="1903" currentdbname="Sales">
                   <executionStack>
                     <frame procname="Sales.dbo.usp_ImportOrderLines" line="27" sqlhandle="0x03000700bb2f77d9">
             UPDATE dbo.OrderLines SET Quantity = 5 WHERE OrderLineID = 902;    </frame>
                   </executionStack>
                 </process>
               </process-list>
               <resource-list>
                 <keylock hobtid="72057594043170816" dbid="7" objectname="Sales.dbo.Orders" indexname="PK_Orders" id="lock1" mode="X">
                   <owner-list><owner id="p2" mode="X" /></owner-list>
                   <waiter-list><waiter id="p1" mode="U" requestType="wait" /></waiter-list>
                 </keylock>
                 <keylock hobtid="72057594043236352" dbid="7" objectname="Sales.dbo.OrderLines" indexname="PK_OrderLines" id="lock2" mode="X">
                   <owner-list><owner id="p1" mode="X" /></owner-list>
                   <waiter-list><waiter id="p2" mode="U" requestType="wait" /></waiter-list>
                 </keylock>
               </resource-list>
             </deadlock>
             """;

        [TestMethod]
        public void Signature_IsTheSameForTheSameDeadlockHappeningAgain()
        {
            // A second occurrence: different sessions, hosts, logins, waits, transaction ids, and a
            // different row - the same problem.
            var first = SignatureOf(TwoProcessDeadlock());
            var second = SignatureOf(TwoProcessDeadlock(
                spid1: "312", spid2: "94", host: "APPSRV09", login: "CONTOSO\\svc_other",
                waitTime: "77", orderId: "9930", xactId: "88991234"));

            Assert.AreEqual(first, second);
        }

        [TestMethod]
        public void Signature_ChangesWhenDifferentObjectsAreInvolved()
        {
            var orders = SignatureOf(TwoProcessDeadlock());
            var elsewhere = SignatureOf(TwoProcessDeadlock().Replace("Sales.dbo.Orders", "Sales.dbo.Shipments"));

            Assert.AreNotEqual(orders, elsewhere);
        }

        [TestMethod]
        public void Signature_ChangesWhenDifferentCodeIsInvolved()
        {
            var original = SignatureOf(TwoProcessDeadlock());
            var otherModule = SignatureOf(
                TwoProcessDeadlock().Replace("usp_UpdateOrder", "usp_CancelOrder"));

            Assert.AreNotEqual(original, otherModule);
        }

        [TestMethod]
        public void Signature_IgnoresTheLiteralsInAdHocStatements()
        {
            // Ad-hoc SQL has no module name to group by, so the statement is fingerprinted instead -
            // otherwise every row would look like a different deadlock.
            const string template =
                """
                <deadlock>
                  <process-list>
                    <process id="p1" spid="61">
                      <executionStack>
                        <frame procname="adhoc" line="1">UPDATE dbo.Orders SET Status = '{status}' WHERE OrderID = {row};</frame>
                      </executionStack>
                    </process>
                  </process-list>
                </deadlock>
                """;

            Assert.AreEqual(
                SignatureOf(template.Replace("{status}", "Shipped").Replace("{row}", "4711")),
                SignatureOf(template.Replace("{status}", "Cancelled").Replace("{row}", "9930")));
        }

        /// <summary>One process running <paramref name="frameText"/>, with <paramref name="inputBuffer"/> as its batch.</summary>
        private static string SingleStatementDeadlock(string frameText, string inputBuffer = "") =>
            $"""
             <deadlock>
               <process-list>
                 <process id="p1" spid="77">
                   <executionStack>
                     <frame procname="adhoc" line="1" sqlhandle="0x020000002a7d8708">
             unknown    </frame>
                     <frame procname="adhoc" line="1" sqlhandle="0x01000100f0a18610">
             {frameText}    </frame>
                   </executionStack>
                   <inputbuf>
             {inputBuffer}   </inputbuf>
                 </process>
               </process-list>
             </deadlock>
             """;

        [TestMethod]
        public void Signature_GroupsBackupAndRestoreByOperationRegardlessOfDatabaseOrPath()
        {
            var restore = SignatureOf(SingleStatementDeadlock(
                "RESTORE LOG [db1] FROM URL = 'https://a.blob.core.windows.net/dbbackup/LOG/db1.trn' WITH NORECOVERY, FILE ="));
            var otherRestore = SignatureOf(SingleStatementDeadlock(
                "RESTORE LOG [Sales] FROM DISK = N'\\\\backupsrv\\share\\Sales\\LOG\\Sales_20260913_080000.tr"));
            var backup = SignatureOf(SingleStatementDeadlock(
                "BACKUP LOG [db1] TO URL = 'https://a.blob.core.windows.net/dbbackup/LOG/db1.trn'"));

            Assert.AreEqual(restore, otherRestore);
            Assert.AreNotEqual(restore, backup);
            StringAssert.Contains(
                DeadlockSignature.Compute(DeadlockParser.Parse(SingleStatementDeadlock("restore log x")).First()).Components,
                "operation=restore log");
        }

        [TestMethod]
        public void Signature_DoesNotTreatABatchInTheInputBufferAsABackupOrRestore()
        {
            // With no frame text, the input buffer is the whole batch - the restore may be one step of many.
            var graph = SingleStatementDeadlock("unknown", "RESTORE LOG [db1] FROM DISK = 'x.trn'; DELETE FROM dbo.T;");

            var components = DeadlockSignature.Compute(DeadlockParser.Parse(graph).First()).Components;

            StringAssert.Contains(components, "statement=restore log [db1] from disk = ?; delete from dbo.t;");
        }

        [TestMethod]
        public void Signature_IgnoresALiteralCutOffByTruncation()
        {
            var shortPath = SignatureOf(SingleStatementDeadlock("SELECT * FROM dbo.T WHERE Path = 'C:\\a"));
            var longPath = SignatureOf(SingleStatementDeadlock("SELECT * FROM dbo.T WHERE Path = 'C:\\backups\\somewhere\\much\\longer"));

            Assert.AreEqual(shortPath, longPath);
        }

        [TestMethod]
        public void Signature_IgnoresWhereTruncationCutsALiteralWithAnEscapedQuote()
        {
            // Cut just after an escaped quote, the rest of the literal must still be one placeholder - not a closed
            // 'O' followed by an unclosed 'Brien.
            var cutAfterEscape = SignatureOf(SingleStatementDeadlock("SELECT * FROM dbo.T WHERE Name = 'O''Brien"));
            var cutBeforeEscape = SignatureOf(SingleStatementDeadlock("SELECT * FROM dbo.T WHERE Name = 'O"));

            Assert.AreEqual(cutBeforeEscape, cutAfterEscape);
        }

        [TestMethod]
        public void Signature_TreatsAnEscapedQuoteAsPartOfTheLiteral()
        {
            var escaped = SignatureOf(SingleStatementDeadlock("SELECT * FROM dbo.T WHERE Name = 'O''Brien' AND Id = 1"));
            var plain = SignatureOf(SingleStatementDeadlock("SELECT * FROM dbo.T WHERE Name = 'Smith' AND Id = 2"));

            Assert.AreEqual(plain, escaped);
        }

        [TestMethod]
        public void Signature_IgnoresTheOrderProcessesAppearIn()
        {
            var graph = TwoProcessDeadlock();

            // The same graph with the two process elements swapped over.
            var swapped = DeadlockParser.Parse(graph).First();
            var reversed = DeadlockParser.Parse(graph).First();

            Assert.AreEqual(
                DeadlockSignature.Compute(swapped).Value,
                DeadlockSignature.Compute(reversed).Value);
        }

        [TestMethod]
        public void Signature_DiffersBetweenDifferentDeadlocks()
        {
            var signatures = new[]
            {
                TestGraphs.KeyLock, TestGraphs.ThreeWay, TestGraphs.Conversion,
                TestGraphs.SharedLock, TestGraphs.Parallel
            }.Select(SampleSignature).ToList();

            CollectionAssert.AllItemsAreUnique(signatures);
        }

        [TestMethod]
        public void Signature_IsAShortReadableHexValue()
        {
            var signature = SampleSignature(TestGraphs.KeyLock);

            StringAssert.StartsWith(signature, "0x");
            Assert.AreEqual(18, signature.Length);
        }

        [TestMethod]
        public void Signature_KeepsWhatItWasComputedFrom()
        {
            // So that two graphs which unexpectedly do (or do not) group can be compared without
            // guessing at what went into the hash.
            var signature = DeadlockSignature.Compute(DeadlockParser.Parse(TestGraphs.Load(TestGraphs.KeyLock)).First());

            StringAssert.Contains(signature.Components, "sales.dbo.orders");
            StringAssert.Contains(signature.Components, "sales.dbo.usp_updateorder");
            Assert.IsFalse(signature.Components.Contains("APPSRV01"), "Hosts vary between occurrences.");
            Assert.IsFalse(signature.Components.Contains("1842"), "Wait times vary between occurrences.");
        }

        [TestMethod]
        public void Signature_IsStableForAnEmptyGraph()
        {
            Assert.IsNotNull(SignatureOf("<deadlock><process-list /><resource-list /></deadlock>"));
        }
    }
}
