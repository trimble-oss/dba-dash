using System.Linq;
using DBADash.QueryPlan.Compare;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanComparisonTests
    {
        /// <summary>
        /// A one statement actual plan: a scan or a seek of one index under a SELECT, with the
        /// statement figures the comparison reads.
        /// </summary>
        private static PlanStatement Plan(
            string accessOp = "Index Scan",
            long elapsedMs = 1000,
            long cpuMs = 800,
            long logicalReads = 5000,
            long grantedKb = 2048,
            long usedKb = 1024,
            string planHash = "0x01",
            string queryHash = "0xAA",
            double cost = 10,
            string compiledValue = "(1)",
            string waits = """<Wait WaitType="PAGEIOLATCH_SH" WaitTimeMs="300" WaitCount="10" />""",
            bool actual = true,
            bool timeStats = true,
            string warnings = "")
        {
            var runtime = actual
                ? $"""<RunTimeInformation><RunTimeCountersPerThread Thread="0" ActualRows="10" ActualExecutions="1" ActualElapsedms="{elapsedMs}" ActualCPUms="{cpuMs}" ActualLogicalReads="{logicalReads}" ActualPhysicalReads="0" /></RunTimeInformation>"""
                : "";
            // No time stats is a plan from before SQL Server 2016 SP1, which had no wait stats either.
            var queryTimeStats = timeStats ? $"""<QueryTimeStats CpuTime="{cpuMs}" ElapsedTime="{elapsedMs}" />""" : "";
            var warningsXml = warnings == "" ? "" : "<Warnings>" + warnings + "</Warnings>";
            var runtimeStats = actual
                ? $"""
                   <MemoryGrantInfo RequestedMemory="{grantedKb}" GrantedMemory="{grantedKb}" MaxUsedMemory="{usedKb}" GrantWaitTime="0" />
                   <WaitStats>{waits}</WaitStats>
                   {queryTimeStats}
                   """
                : "";
            var runtimeValue = actual ? """ ParameterRuntimeValue="(5)" """ : "";

            return PlanParser.Parse($"""
                <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.564" Build="16.0.4222.2">
                  <BatchSequence><Batch><Statements>
                    <StmtSimple StatementId="1" StatementText="SELECT * FROM T WHERE Id = @id" StatementType="SELECT"
                                StatementSubTreeCost="{cost}" QueryHash="{queryHash}" QueryPlanHash="{planHash}">
                      <QueryPlan DegreeOfParallelism="1" CachedPlanSize="16">
                        {runtimeStats}
                        <RelOp NodeId="0" PhysicalOp="{accessOp}" LogicalOp="{accessOp}" EstimateRows="10" AvgRowSize="100" EstimatedTotalSubtreeCost="{cost}">
                          {warningsXml}
                          {runtime}
                          <IndexScan><Object Database="[db]" Schema="[dbo]" Table="[T]" Index="[IX_T]" /></IndexScan>
                        </RelOp>
                        <ParameterList><ColumnReference Column="@id" ParameterCompiledValue="{compiledValue}"{runtimeValue}/></ParameterList>
                      </QueryPlan>
                    </StmtSimple>
                  </Statements></Batch></BatchSequence>
                </ShowPlanXML>
                """).Statements[0];
        }

        [TestMethod]
        public void LowerElapsed_IsBetter_WithTheChangeSaidAsAMultiple()
        {
            var comparison = PlanComparison.Compare(Plan(elapsedMs: 10000), Plan(elapsedMs: 1000));

            var elapsed = comparison.Metric(PlanComparison.ElapsedName)!;
            Assert.AreEqual(PlanComparisonChange.Better, elapsed.Change);
            Assert.AreEqual("-9,000 ms (10x lower)", elapsed.DifferenceText);
            Assert.AreEqual(0.1, elapsed.Ratio!.Value, 0.0001);
        }

        [TestMethod]
        public void SwappingTheSides_SwapsBetterAndWorse()
        {
            var comparison = PlanComparison.Compare(Plan(logicalReads: 100), Plan(logicalReads: 5000));

            var reads = comparison.Metric(PlanComparison.LogicalReadsName)!;
            Assert.AreEqual(PlanComparisonChange.Worse, reads.Change);
            Assert.AreEqual("+4,900 (50x higher)", reads.DifferenceText);
        }

        [TestMethod]
        public void SmallTimingDifferences_AreSimilar_NotBetterOrWorse()
        {
            // Two runs of one plan never take exactly the same time: 3% and 5 ms are noise.
            var comparison = PlanComparison.Compare(Plan(elapsedMs: 1000, cpuMs: 5), Plan(elapsedMs: 1030, cpuMs: 10));

            Assert.AreEqual(PlanComparisonChange.Similar, comparison.Metric(PlanComparison.ElapsedName)!.Change);
            Assert.AreEqual(PlanComparisonChange.Similar, comparison.Metric(PlanComparison.CpuName)!.Change,
                "Doubled, but by less than the 10 ms noise floor.");
        }

        [TestMethod]
        public void NeutralFigures_AreChanged_NeverBetterOrWorse()
        {
            var comparison = PlanComparison.Compare(Plan(elapsedMs: 1000, cpuMs: 500), Plan(elapsedMs: 1000, cpuMs: 4000));

            Assert.AreEqual(PlanComparisonChange.Changed, comparison.Metric("CPU per Elapsed")!.Change);
        }

        [TestMethod]
        public void Trend_IsTheWayTheFigureMoved_WhateverThatMeans()
        {
            var comparison = PlanComparison.Compare(Plan(grantedKb: 4096, usedKb: 1024), Plan(grantedKb: 2048, usedKb: 1536, elapsedMs: 5000));

            Assert.AreEqual(PlanComparisonTrend.Higher, comparison.Metric(PlanComparison.ElapsedName)!.Trend);
            Assert.AreEqual(PlanComparisonChange.Worse, comparison.Metric(PlanComparison.ElapsedName)!.Change);

            // Up, and neither good nor bad news.
            Assert.AreEqual(PlanComparisonTrend.Higher, comparison.Metric(PlanComparison.GrantUsedName)!.Trend);
            Assert.AreEqual(PlanComparisonChange.Changed, comparison.Metric(PlanComparison.GrantUsedName)!.Change);

            Assert.AreEqual(PlanComparisonTrend.None, comparison.Metric(PlanComparison.CpuName)!.Trend, "Unchanged figures have no trend.");
        }

        [TestMethod]
        public void OptimiserEstimates_AreFlagged_MeasuredFiguresAreNot()
        {
            var comparison = PlanComparison.Compare(Plan(cost: 10), Plan(cost: 500));

            // Still better or worse, but the viewer shows it as higher or lower: a plan built from
            // more accurate estimates can cost more and run faster.
            var cost = comparison.Metric(PlanComparison.CostName)!;
            Assert.IsTrue(cost.IsEstimate);
            Assert.AreEqual(PlanComparisonChange.Worse, cost.Change);
            Assert.AreEqual(PlanComparisonTrend.Higher, cost.Trend);

            Assert.IsTrue(comparison.Metric(PlanComparison.GrantedName)!.IsEstimate);
            Assert.IsTrue(comparison.Metric("Requested")!.IsEstimate);
            Assert.IsFalse(comparison.Metric(PlanComparison.ElapsedName)!.IsEstimate);
            Assert.IsFalse(comparison.Metric(PlanComparison.SpillsName)!.IsEstimate);
            Assert.IsFalse(comparison.Metric("Worst Row Estimate")!.IsEstimate, "How accurate the estimates were is measured, not estimated.");
        }

        [TestMethod]
        public void KeyDifferences_LeadWithWhatHappened_AndLeaveTheGrantToSpillsAndGrantWait()
        {
            var comparison = PlanComparison.Compare(Plan(elapsedMs: 10000, grantedKb: 2048, cost: 10), Plan(elapsedMs: 1000, grantedKb: 8192, cost: 500));

            var metrics = comparison.Highlights.Where(h => h.Metric is not null).Select(h => h.Metric!.Name).ToList();
            Assert.AreEqual(PlanComparison.ElapsedName, metrics.First());
            Assert.AreEqual(PlanComparison.CostName, metrics.Last(), "The estimate comes after every measured figure.");
            CollectionAssert.DoesNotContain(metrics, PlanComparison.GrantedName);
        }

        [TestMethod]
        public void GrantUsed_IsComparedInPoints()
        {
            var comparison = PlanComparison.Compare(Plan(grantedKb: 4096, usedKb: 1024), Plan(grantedKb: 2048, usedKb: 1536));

            var used = comparison.Metric(PlanComparison.GrantUsedName)!;
            Assert.AreEqual("25%", used.BeforeText);
            Assert.AreEqual("75%", used.AfterText);
            Assert.AreEqual("+50 pts", used.DifferenceText);

            // Better sized, but closer to a spill: neither better nor worse.  The two ends that are a
            // problem are judged by Excessive Grant and Spills.
            Assert.AreEqual(PlanComparisonChange.Changed, used.Change);
        }

        private const long Gb = 1024 * 1024;

        [TestMethod]
        public void ExcessiveGrant_GoingAway_IsBetter_WithTheFiguresInTheKeyDifferences()
        {
            var comparison = PlanComparison.Compare(Plan(grantedKb: 4 * Gb, usedKb: Gb / 10), Plan(grantedKb: Gb / 2, usedKb: Gb / 4));

            var excessive = comparison.Metric(PlanComparison.ExcessiveGrantName)!;
            Assert.AreEqual("Yes", excessive.BeforeText);
            Assert.AreEqual("No", excessive.AfterText);
            Assert.AreEqual(PlanComparisonChange.Better, excessive.Change);

            Assert.IsTrue(comparison.Highlights.Any(h =>
                    h.Change == PlanComparisonChange.Better &&
                    h.Text == "No longer an excessive memory grant: 512 MB granted, 50% used (was 4 GB granted, 2.5% used)."),
                string.Join("\n", comparison.Highlights));
        }

        [TestMethod]
        public void ExcessiveGrant_Arriving_IsWorse()
        {
            var comparison = PlanComparison.Compare(Plan(grantedKb: Gb / 2, usedKb: Gb / 4), Plan(grantedKb: 4 * Gb, usedKb: Gb / 10));

            Assert.AreEqual(PlanComparisonChange.Worse, comparison.Metric(PlanComparison.ExcessiveGrantName)!.Change);
            Assert.IsTrue(comparison.Highlights.Any(h => h.Change == PlanComparisonChange.Worse && h.Text.StartsWith("Excessive memory grant: 4 GB granted")));
        }

        [TestMethod]
        public void ExcessiveGrant_NeedsAUsedFigure_SoAnEstimatedPlanCannotBeJudged()
        {
            Assert.IsNull(PlanComparison.Compare(Plan(actual: false), Plan(actual: false)).Metric(PlanComparison.ExcessiveGrantName));

            var mixed = PlanComparison.Compare(Plan(actual: false), Plan(grantedKb: 4 * Gb, usedKb: Gb / 10));
            Assert.AreEqual(PlanComparisonChange.NotComparable, mixed.Metric(PlanComparison.ExcessiveGrantName)!.Change);
        }

        [TestMethod]
        public void RuntimeFigures_AgainstAnEstimatedPlan_AreNotComparable()
        {
            var comparison = PlanComparison.Compare(Plan(actual: false), Plan());

            Assert.IsFalse(comparison.BothActual);
            var elapsed = comparison.Metric(PlanComparison.ElapsedName)!;
            Assert.AreEqual(PlanComparisonChange.NotComparable, elapsed.Change);
            Assert.IsNull(elapsed.BeforeText);
            Assert.AreEqual(string.Empty, elapsed.DifferenceText);
            Assert.IsTrue(comparison.Highlights.Any(h => h.Text.StartsWith("The before plan is estimated")));
        }

        [TestMethod]
        public void FiguresNeitherSideHas_AreLeftOut()
        {
            var comparison = PlanComparison.Compare(Plan(actual: false), Plan(actual: false));

            Assert.IsNull(comparison.Metric(PlanComparison.ElapsedName));
            Assert.IsNull(comparison.Metric(PlanComparison.SpillsName), "Nothing ran, so nothing could spill - not zero spills.");
            Assert.IsNotNull(comparison.Metric(PlanComparison.CostName));
        }

        [TestMethod]
        public void PlanShape_IsJudgedByTheQueryPlanHash()
        {
            Assert.AreEqual(true, PlanComparison.Compare(Plan(), Plan()).SamePlanShape);
            Assert.AreEqual(false, PlanComparison.Compare(Plan(planHash: "0x01"), Plan(planHash: "0x02")).SamePlanShape);

            var differentQuery = PlanComparison.Compare(Plan(queryHash: "0xAA"), Plan(queryHash: "0xBB"));
            Assert.AreEqual(false, differentQuery.SameQuery);
            Assert.IsTrue(differentQuery.Highlights[0].Text.Contains("different queries"), "Said first: it changes how everything else reads.");
        }

        [TestMethod]
        public void Objects_ShowTheAccessMethodThatWentAndTheOneThatCame()
        {
            var comparison = PlanComparison.Compare(Plan(accessOp: "Index Scan"), Plan(accessOp: "Index Seek", planHash: "0x02"));

            var scan = comparison.Objects.Single(o => o.Operator == "Index Scan");
            Assert.AreEqual(PlanPresence.BeforeOnly, scan.Status);
            Assert.AreEqual("db.dbo.T", scan.Object);
            Assert.AreEqual(5000, scan.Before!.LogicalReads);

            var seek = comparison.Objects.Single(o => o.Operator == "Index Seek");
            Assert.AreEqual(PlanPresence.AfterOnly, seek.Status);

            Assert.IsTrue(comparison.Highlights.Any(h => h.Text == "Removed: Index Scan on db.dbo.T.IX_T"));
            Assert.IsTrue(comparison.Highlights.Any(h => h.Text == "Added: Index Seek on db.dbo.T.IX_T"));
        }

        [TestMethod]
        public void Operators_AreGroupedByKind_WithCountsEachSide()
        {
            var comparison = PlanComparison.Compare(Plan(accessOp: "Index Scan"), Plan(accessOp: "Index Scan"));

            var scans = comparison.Operators.Single(o => o.Operator == "Index Scan");
            Assert.AreEqual(PlanPresence.Both, scans.Status);
            Assert.AreEqual(1, scans.Before!.Count);
            Assert.AreEqual(1, scans.After!.Count);
        }

        [TestMethod]
        public void Waits_AreTheUnionOfBothSides()
        {
            var comparison = PlanComparison.Compare(
                Plan(waits: """<Wait WaitType="PAGEIOLATCH_SH" WaitTimeMs="300" WaitCount="10" />"""),
                Plan(waits: """<Wait WaitType="CXPACKET" WaitTimeMs="50" WaitCount="4" />"""));

            var io = comparison.Waits.Single(w => w.WaitType == "PAGEIOLATCH_SH");
            Assert.AreEqual(300, io.BeforeMs);
            Assert.IsNull(io.AfterMs);
            Assert.AreEqual(-300, io.DifferenceMs);

            Assert.AreEqual("PAGEIOLATCH_SH", comparison.Waits[0].WaitType, "The biggest wait on either side first.");
            Assert.AreEqual(PlanComparisonChange.Better, io.Change);
        }

        [TestMethod]
        public void Waits_AreNotComparable_WhenOneSideCouldNotRecordThem()
        {
            // An actual plan without time stats predates wait stats, so its missing waits aren't zero.
            var comparison = PlanComparison.Compare(
                Plan(waits: """<Wait WaitType="PAGEIOLATCH_SH" WaitTimeMs="300" WaitCount="10" />"""),
                Plan(waits: "", timeStats: false));

            var io = comparison.Waits.Single(w => w.WaitType == "PAGEIOLATCH_SH");
            Assert.IsNull(io.DifferenceMs);
            Assert.AreEqual(PlanComparisonChange.NotComparable, io.Change);
        }

        [TestMethod]
        public void WaitTime_FromAPlanTooOldToRecordWaits_IsNoFigure_NotZero()
        {
            var comparison = PlanComparison.Compare(Plan(timeStats: false, waits: ""), Plan());

            var wait = comparison.Metric(PlanComparison.WaitName)!;
            Assert.IsNull(wait.Before);
            Assert.AreEqual(PlanComparisonChange.NotComparable, wait.Change);

            // A plan new enough to record waits, that recorded none, did no waiting.
            Assert.AreEqual(0, PlanComparison.Compare(Plan(waits: ""), Plan()).Metric(PlanComparison.WaitName)!.Before);
        }

        [TestMethod]
        public void RuntimeWarnings_AgainstAnEstimatedPlan_AreNotComparable_OtherWarningsStillCount()
        {
            const string spill = """<SpillToTempDb SpillLevel="1" SpilledThreadCount="1" />""";
            const string noStats = """<ColumnsWithNoStatistics><ColumnReference Database="[db]" Schema="[dbo]" Table="[T]" Column="c" /></ColumnsWithNoStatistics>""";

            var mixed = PlanComparison.Compare(Plan(actual: false), Plan(warnings: spill + noStats));
            var mixedWarnings = mixed.Warnings.Where(w => !w.IsAnalysis).ToList();

            Assert.AreEqual(PlanComparisonChange.NotComparable, mixedWarnings.Single(w => w.DependsOnPlanKind).Change);
            Assert.AreEqual(PlanComparisonChange.Worse, mixedWarnings.Single(w => !w.DependsOnPlanKind).Change);
            Assert.AreEqual(1, mixed.Metric(PlanComparison.WarningsName)!.After, "The spill isn't counted: the estimated plan could never have had one.");

            var bothActual = PlanComparison.Compare(Plan(), Plan(warnings: spill + noStats));
            Assert.AreEqual(PlanComparisonChange.Worse, bothActual.Warnings.Single(w => !w.IsAnalysis && w.DependsOnPlanKind).Change);
            Assert.AreEqual(2, bothActual.Metric(PlanComparison.WarningsName)!.After);
        }

        [TestMethod]
        public void DBADashFindings_AreComparedBesideTheWarnings()
        {
            var timedOut = Plan();
            timedOut.OptimisationEarlyAbortReason = "TimeOut";

            // A finding worth fixing, gone from the new plan, is better - and marked as DBA Dash's own.
            var fixedIt = PlanComparison.Compare(timedOut, Plan());
            var timeout = fixedIt.Warnings.Single(w => w.Title == "Optimizer timed out");
            Assert.IsTrue(timeout.IsAnalysis);
            Assert.AreEqual(1, timeout.BeforeCount);
            Assert.AreEqual(0, timeout.AfterCount);
            Assert.AreEqual(PlanComparisonChange.Better, timeout.Change);

            // A note coming or going is a change, not better or worse.
            var noWaits = PlanComparison.Compare(Plan(), Plan(waits: ""));
            Assert.AreEqual(PlanComparisonChange.Changed, noWaits.Warnings.Single(w => w.Title == "Waits").Change);

            // One only an actual plan gives says nothing against an estimated plan, and the note that
            // a plan is estimated is left out altogether.
            var mixed = PlanComparison.Compare(Plan(actual: false), Plan());
            Assert.AreEqual(PlanComparisonChange.NotComparable, mixed.Warnings.Single(w => w.Title == "Waits").Change);
            Assert.IsFalse(mixed.Warnings.Any(w => w.Title == PlanInsights.EstimatedPlanTitle));
        }

        [TestMethod]
        public void ACatchAllPlan_AgainstAnOptimizedVariant_ShowsTheWarningGone()
        {
            var comparison = PlanComparison.Compare(
                TestPlans.Statement(TestPlans.OptionalParameters),
                TestPlans.Statement(TestPlans.OptionalParametersVariantReused));

            var catchAll = comparison.Warnings.Single(w => w.Title == "Optional parameters");
            Assert.AreEqual(1, catchAll.BeforeCount);
            Assert.AreEqual(0, catchAll.AfterCount);
            Assert.AreEqual(PlanComparisonChange.Better, catchAll.Change);
            Assert.AreEqual(1, comparison.Warnings.Single(w => w.Title == "Optional parameters (optimized)").AfterCount);
        }

        [TestMethod]
        public void DifferentCompiledValues_AreHighlighted()
        {
            var comparison = PlanComparison.Compare(Plan(compiledValue: "(1)"), Plan(compiledValue: "(42)"));

            var id = comparison.Parameters.Single();
            Assert.IsTrue(id.CompiledValueDiffers);
            Assert.IsFalse(id.RuntimeValueDiffers);
            Assert.IsTrue(comparison.Highlights.Any(h => h.Text == "Compiled for different parameter values: @id."));
        }

        [TestMethod]
        public void Build_IsComparedWhenGiven()
        {
            var comparison = PlanComparison.Compare(Plan(), Plan(), "15.0.4415.2", "16.0.4222.2");

            var build = comparison.Metric("SQL Server Build")!;
            Assert.AreEqual(PlanComparisonChange.Changed, build.Change);
        }

        [TestMethod]
        public void Relative_SaysPercentagesForModestChangesAndMultiplesForLargeOnes()
        {
            Assert.AreEqual("35% higher", PlanComparisonMetric.Relative(100, 135));
            Assert.AreEqual("20% lower", PlanComparisonMetric.Relative(100, 80));
            Assert.AreEqual("3.2x higher", PlanComparisonMetric.Relative(10, 32));
            Assert.AreEqual("4x lower", PlanComparisonMetric.Relative(100, 25));
            Assert.AreEqual("100% lower", PlanComparisonMetric.Relative(100, 0));
            Assert.IsNull(PlanComparisonMetric.Relative(0, 10), "Nothing is a multiple of zero.");
        }

        [TestMethod]
        public void ToText_ListsEveryFigureUnderItsGroup()
        {
            var text = PlanComparison.Compare(Plan(elapsedMs: 10000), Plan(elapsedMs: 1000)).ToText();

            StringAssert.Contains(text, "Run Time");
            StringAssert.Contains(text, "  Elapsed Time: 10.0 s -> 1,000 ms  (-9,000 ms (10x lower))");
        }

        [TestMethod]
        public void SamplePlans_CompareWithoutError()
        {
            // Every pairing of the sample plans' statements, including one with itself - the
            // comparison has to cope with whatever two plans the reader picks.
            var statements = new[] { TestPlans.Batch, TestPlans.ParallelSpill, TestPlans.KeyLookupSeek, TestPlans.Expressions, TestPlans.Concatenation }
                .SelectMany(name => TestPlans.Load(name).Statements)
                .ToList();

            foreach (var before in statements)
            {
                foreach (var after in statements)
                {
                    var comparison = PlanComparison.Compare(before, after);
                    Assert.IsNotNull(comparison.ToText());
                }

                var self = PlanComparison.Compare(before, before);
                Assert.IsFalse(self.Metrics.Any(m => m.Change is PlanComparisonChange.Better or PlanComparisonChange.Worse or PlanComparisonChange.Changed),
                    "A plan compared with itself has changed nothing.");
            }
        }
    }
}
