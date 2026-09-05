using System.Collections.Generic;
using System.Linq;
using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// The findings: what the viewer says about a deadlock beyond drawing it.
    ///
    /// Tests assert on the finding that fires and on the facts woven into its text - the processes
    /// and objects it names - rather than on whole sentences, so the wording can be improved without
    /// rewriting the suite.
    /// </summary>
    [TestClass]
    public class DeadlockAnalyserTests
    {
        private static IReadOnlyList<DeadlockFinding> AnalyseSample(string sampleName) =>
            DeadlockAnalyser.Analyse(DeadlockParser.Parse(TestGraphs.Load(sampleName)).First());

        private static IReadOnlyList<DeadlockFinding> AnalyseXml(string xml) =>
            DeadlockAnalyser.Analyse(DeadlockParser.Parse(xml).First());

        private static DeadlockFinding Single(IReadOnlyList<DeadlockFinding> findings, string title) =>
            findings.SingleOrDefault(f => f.Title.Contains(title, System.StringComparison.OrdinalIgnoreCase))
            ?? throw new AssertFailedException(
                $"No finding titled '{title}'.  Found: {string.Join(", ", findings.Select(f => f.Title))}");

        // ---------------------------------------------------------------- patterns

        [TestMethod]
        public void Analyse_ConversionDeadlock_IsRecognised()
        {
            var finding = Single(AnalyseSample(TestGraphs.Conversion), "conversion");

            Assert.AreEqual(DeadlockFindingSeverity.Advice, finding.Severity);
            CollectionAssert.AreEquivalent(
                new[] { 58, 64 },
                finding.Processes.Select(p => p.Spid!.Value).ToArray());
            StringAssert.Contains(finding.Detail, "Ops.dbo.Ledger");
            StringAssert.Contains(finding.Detail, "UPDLOCK");
        }

        [TestMethod]
        public void Analyse_TwoProcessDeadlock_NamesBothObjectsInOrder()
        {
            var finding = Single(AnalyseSample(TestGraphs.KeyLock), "opposite order");

            Assert.AreEqual(DeadlockFindingSeverity.Advice, finding.Severity);
            StringAssert.Contains(finding.Detail, "Sales.dbo.Orders");
            StringAssert.Contains(finding.Detail, "Sales.dbo.OrderLines");
            Assert.AreEqual(2, finding.Resources.Count);
        }

        [TestMethod]
        public void Analyse_KeyLookupDeadlock_SuggestsCoveringIndex()
        {
            // Two indexes of one table, locked in opposite order - the classic key-lookup deadlock.
            // Settling on a lock order is not an option (the engine chooses the order it walks a
            // table's indexes in), so this must not be reported as objects locked in opposite order.
            var findings = AnalyseSample(TestGraphs.KeyLookup);

            var finding = Single(findings, "key lookup");

            Assert.AreEqual(DeadlockFindingSeverity.Advice, finding.Severity);
            StringAssert.Contains(finding.Detail, "Sales.dbo.Orders");
            StringAssert.Contains(finding.Detail, "PK_Orders");
            StringAssert.Contains(finding.Detail, "IX_Orders_Status");
            StringAssert.Contains(finding.Detail, "covering index");

            Assert.IsFalse(findings.Any(f => f.Title.Contains("opposite order")),
                "Two indexes of one table have no lock order to settle on; the covering-index finding replaces it.");
        }

        [TestMethod]
        public void Analyse_ThreeWayDeadlock_DescribesTheWholeChain()
        {
            // The pairwise rule cannot see a three way cycle - no two of them hold what the other
            // wants - so without this the most confusing graphs would get nothing at all.
            var findings = AnalyseSample(TestGraphs.ThreeWay);
            var finding = Single(findings, "cycle of 3");

            Assert.AreEqual(3, finding.Processes.Count);
            foreach (var spid in new[] { "SPID 10", "SPID 20", "SPID 30" })
            {
                StringAssert.Contains(finding.Detail, spid);
            }

            Assert.IsFalse(findings.Any(f => f.Title.Contains("opposite order")),
                "A three way cycle has no pair holding what the other wants.");
        }

        [TestMethod]
        public void Analyse_ParallelDeadlock_IsCalledOutAndNotBlamedOnLockOrder()
        {
            var findings = AnalyseSample(TestGraphs.Parallel);
            var finding = Single(findings, "parallel");

            StringAssert.Contains(finding.Detail, "MAXDOP");

            // The exchanges are not objects with an order to settle on: this fired before the rule
            // learned to leave parallel deadlocks alone, and read as nonsense.
            Assert.IsFalse(findings.Any(f => f.Title.Contains("opposite order")));
            Assert.IsFalse(findings.Any(f => f.Title.Contains("cycle of")));
        }

        [TestMethod]
        public void Analyse_ReaderBlockingWriter_SuggestsRowVersioning()
        {
            var finding = Single(AnalyseSample(TestGraphs.SharedLock), "reader");

            Assert.AreEqual(71, finding.Processes.First().Spid);
            StringAssert.Contains(finding.Detail, "snapshot");
        }

        [TestMethod]
        public void Analyse_SameModuleOnBothSides_IsPointedOut()
        {
            var finding = Single(AnalyseSample(TestGraphs.Conversion), "same module");

            StringAssert.Contains(finding.Detail, "Ops.dbo.usp_AdjustBalance");
            Assert.AreEqual(DeadlockFindingSeverity.Information, finding.Severity);
        }

        // ---------------------------------------------------------------- settings and shapes

        [TestMethod]
        public void Analyse_StricterIsolationLevel_IsReported()
        {
            var finding = Single(AnalyseSample(TestGraphs.Conversion), "isolation");

            StringAssert.Contains(finding.Detail, "repeatable read");
        }

        [TestMethod]
        public void Analyse_TableLevelLock_IsReported()
        {
            var finding = Single(AnalyseSample(TestGraphs.XeEventEnvelope), "table");

            Assert.AreEqual(DeadlockFindingSeverity.Advice, finding.Severity);
            StringAssert.Contains(finding.Detail, "escalation");
        }

        [TestMethod]
        public void Analyse_IntentLocksAreNotMistakenForTableLocks()
        {
            // An IX object lock says rows below are locked, not that the table is - reporting it as a
            // table lock would send people hunting for an escalation that never happened.
            var findings = AnalyseSample(TestGraphs.IntentObjectLock);

            Assert.IsFalse(findings.Any(f => f.Title.Contains("table")));
        }

        [TestMethod]
        public void Analyse_HeapLock_IsReported()
        {
            var finding = Single(AnalyseSample(TestGraphs.Truncated), "heap");

            StringAssert.Contains(finding.Detail, "Ops.dbo.Queue");
        }

        // ---------------------------------------------------------------- victim and capture

        [TestMethod]
        public void Analyse_DeadlockPriority_ExplainsTheVictimChoice()
        {
            var finding = Single(AnalyseSample(TestGraphs.Priority), "priority");

            StringAssert.Contains(finding.Detail, "SPID 5");
            StringAssert.Contains(finding.Detail, "-5");
        }

        [TestMethod]
        public void Analyse_ExpensiveVictim_IsReported()
        {
            var finding = Single(AnalyseSample(TestGraphs.ExpensiveVictim), "roll back");

            StringAssert.Contains(finding.Detail, "50 MB");
        }

        [TestMethod]
        public void Analyse_SmallVictim_IsNotRemarkedOn()
        {
            var findings = AnalyseSample(TestGraphs.SmallVictim);

            Assert.IsFalse(findings.Any(f => f.Title.Contains("roll back")));
        }

        [TestMethod]
        public void Analyse_TruncatedCapture_WarnsBeforeAdvising()
        {
            var findings = AnalyseSample(TestGraphs.Truncated);

            Assert.IsNotNull(Single(findings, "incomplete"));
            Assert.IsNotNull(Single(findings, "No victim"));

            // Warnings lead: how much the rest is worth depends on them.
            Assert.AreEqual(DeadlockFindingSeverity.Warning, findings[0].Severity);
            CollectionAssert.AreEqual(
                findings.Select(f => f.Severity).OrderByDescending(s => s).ToArray(),
                findings.Select(f => f.Severity).ToArray());
        }

        [TestMethod]
        public void Analyse_GraphWithNoCycle_SaysSo()
        {
            // One process waiting on another that waits for nothing: half a deadlock, which is what a
            // dropped process leaves behind.
            var findings = AnalyseSample(TestGraphs.NoCycle);

            Assert.AreEqual(DeadlockFindingSeverity.Warning, Single(findings, "No complete cycle").Severity);
        }

        [TestMethod]
        public void Analyse_EmptyGraph_ProducesNothingToPanicAbout()
        {
            var findings = AnalyseXml("<deadlock><process-list /><resource-list /></deadlock>");

            // No processes means nothing to say beyond the missing victim - and no exceptions.
            Assert.IsTrue(findings.All(f => f.Severity != DeadlockFindingSeverity.Advice));
        }

        [TestMethod]
        public void Analyse_FindingsCarryTheModelObjectsTheyDescribe()
        {
            // The viewer takes the reader from a finding to the node it is about, so the references
            // have to be the graph's own objects rather than copies.
            var graph = DeadlockParser.Parse(TestGraphs.Load(TestGraphs.KeyLock)).First();
            var finding = DeadlockAnalyser.Analyse(graph).First(f => f.Title.Contains("opposite order"));

            Assert.IsTrue(finding.Processes.All(p => graph.Processes.Contains(p)));
            Assert.IsTrue(finding.Resources.All(r => graph.Resources.Contains(r)));
        }
    }
}
