using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Own operator times: the reported figures less what the operator's inputs took.  The small plans
    /// here are built inline so each test states the exact counters its answer comes from.
    /// </summary>
    [TestClass]
    public class PlanOperatorTimingTests
    {
        /// <summary>
        /// A serial row mode plan: a nested loop over a Compute Scalar with no timings of its own, a
        /// scan under that, and a seek.
        /// </summary>
        private static string SerialRowMode() =>
            Op(0, "Nested Loops", "Inner Join", T(0, 100, 80),
                Op(1, "Compute Scalar", "Compute Scalar", "",
                    Op(2, "Index Scan", "Index Scan", T(0, 30, 25))),
                Op(3, "Index Seek", "Index Seek", T(0, 50, 40)));

        // ---------------------------------------------------------------- row mode

        [TestMethod]
        public void OwnTime_TakesTheInputsOffARowModeOperator()
        {
            var statement = Parse(SerialRowMode());
            var join = TestPlans.Operator(statement, 0);

            // 100 ms reported, of which the scan took 30 and the seek 50.
            Assert.AreEqual(100, join.Runtime!.ActualElapsedMs);
            Assert.AreEqual(20, join.OwnElapsedMs);
            Assert.AreEqual(15, join.OwnCpuMs);
        }

        [TestMethod]
        public void OwnTime_LooksThroughAnOperatorWithNoTimings()
        {
            var statement = Parse(SerialRowMode());

            // The Compute Scalar measured nothing, so it has no time of its own to show - and the scan
            // under it still comes off the join, which it would not if the Compute Scalar counted as 0.
            Assert.IsNull(TestPlans.Operator(statement, 1).OwnElapsedMs);
            Assert.AreEqual(20, TestPlans.Operator(statement, 0).OwnElapsedMs);
        }

        [TestMethod]
        public void OwnTime_OfALeafIsWhatItReported()
        {
            var scan = TestPlans.Operator(Parse(SerialRowMode()), 2);

            Assert.AreEqual(30, scan.OwnElapsedMs);
            Assert.AreEqual(25, scan.OwnCpuMs);
        }

        [TestMethod]
        public void OwnTime_IsNeverNegative()
        {
            // Clock resolution lets an input report a little more than the operator it runs inside.
            var statement = Parse(
                Op(0, "Top", "Top", T(0, 10, 10),
                    Op(1, "Index Scan", "Index Scan", T(0, 12, 11))));

            Assert.AreEqual(0, TestPlans.Operator(statement, 0).OwnElapsedMs);
            Assert.AreEqual(0, TestPlans.Operator(statement, 0).OwnCpuMs);
        }

        [TestMethod]
        public void OwnTime_IsNullOnAnEstimatedPlan()
        {
            var statement = TestPlans.Statement(TestPlans.KeyLookupSeek);

            Assert.IsTrue(statement.Operators.All(o => o.OwnElapsedMs is null && o.OwnCpuMs is null));
        }

        // ---------------------------------------------------------------- parallel and batch mode

        [TestMethod]
        public void OwnTime_SubtractsThreadByThreadAndTakesTheWholeBatchRegionOff()
        {
            var statement = TestPlans.Statement(TestPlans.ParallelSpill);

            // The batch hash join and the scans under it took 180 + 90 + 60 = 330 ms on every thread.
            // The sort's threads reported 200, 250, 300 and 900, so only the busiest has time left
            // over: 570 ms.  Taking the slowest input off the slowest thread would give the same
            // answer here only because the inputs are even.
            var sort = TestPlans.Operator(statement, 1);
            Assert.AreEqual(570, sort.OwnElapsedMs);

            // CPU is added up over the threads: 800 - 270 on the busiest, and nothing on the others.
            Assert.AreEqual(530, sort.OwnCpuMs);

            // A batch mode operator reports its own time already.
            var hash = TestPlans.Operator(statement, 2);
            Assert.AreEqual(180, hash.OwnElapsedMs);
            Assert.AreEqual(560, hash.OwnCpuMs);
        }

        [TestMethod]
        public void OwnTime_ComparesTheBranchAsAWholeUnderAnExchange()
        {
            // The producer side of a repartition runs on another set of threads that is numbered from
            // one again, so thread 1 above the exchange and thread 1 below it are unrelated.
            var statement = Parse(
                Op(0, "Parallelism", "Gather Streams", T(0, 1000, 5),
                    Op(1, "Stream Aggregate", "Aggregate", T(1, 800, 300) + T(2, 700, 250),
                        Op(2, "Parallelism", "Repartition Streams", T(1, 600, 50) + T(2, 500, 40),
                            Op(3, "Index Scan", "Index Scan", T(1, 100, 90) + T(2, 400, 380))))));

            // The aggregate's inputs are the exchange's consumer side, on its own threads:
            // 800 - 600 and 700 - 500.
            var aggregate = TestPlans.Operator(statement, 1);
            Assert.AreEqual(200, aggregate.OwnElapsedMs);
            Assert.AreEqual(250 + 210, aggregate.OwnCpuMs);

            // The exchange's slowest consumer less its branch's slowest producer: 600 - 400.  Pairing
            // thread 1 with thread 1 would have said 500.
            var repartition = TestPlans.Operator(statement, 2);
            Assert.AreEqual(200, repartition.OwnElapsedMs);

            // An exchange's CPU is only ever its consumer side's, so nothing comes off it.
            Assert.AreEqual(90, repartition.OwnCpuMs);

            // The gather waited for the whole parallel branch: 1000 - 800.
            Assert.AreEqual(200, TestPlans.Operator(statement, 0).OwnElapsedMs);
        }

        [TestMethod]
        public void OwnTime_LeavesOutTheCoordinatorsWallClockInAParallelBranch()
        {
            // Thread 0 ran no executions, but carries the branch's 1,000 ms of wall clock.  The two
            // workers each spent about 950 ms under the batch hash join and the seek.
            var statement = Parse(
                Op(0, "Nested Loops", "Inner Join", T(0, 1000, 0, executions: 0) + T(1, 1000, 30) + T(2, 990, 28),
                    Op(1, "Hash Match", "Aggregate", T(0, 0, 0, "Batch", executions: 0) + T(1, 900, 12, "Batch") + T(2, 890, 11, "Batch")),
                    Op(2, "Index Seek", "Index Seek", T(0, 0, 0, executions: 0) + T(1, 50, 6) + T(2, 48, 5))));

            // 1000 - 950 and 990 - 938.  Counting thread 0 would have given the join all 1,000 ms.
            Assert.AreEqual(52, TestPlans.Operator(statement, 0).OwnElapsedMs);
        }

        [TestMethod]
        public void OwnTime_IsWhatWasReportedWhenThePlanSaysTimesAreExclusive()
        {
            // SQL Server 2022 can report every operator's own time itself.  Subtracting again would
            // leave the join with nothing.
            var statement = Parse(SerialRowMode(), "ExclusiveProfileTimeActive=\"true\"");

            Assert.IsTrue(statement.ExclusiveProfileTimeActive);
            Assert.AreEqual(100, TestPlans.Operator(statement, 0).OwnElapsedMs);
            Assert.AreEqual(80, TestPlans.Operator(statement, 0).OwnCpuMs);
        }

        // ---------------------------------------------------------------- where it is shown

        [TestMethod]
        public void Properties_ShowOwnTimeBesideTheReportedTimeWhereTheyDiffer()
        {
            var statement = Parse(SerialRowMode());

            var join = TestPlans.Operator(statement, 0).Properties.Single(p => p.Name == "Actual Execution");
            Assert.AreEqual("100 ms", join.Children.Single(p => p.Name == "Actual Elapsed Time").Value);
            Assert.AreEqual("20 ms", join.Children.Single(p => p.Name == "Own Elapsed Time").Value);
            Assert.AreEqual("15 ms", join.Children.Single(p => p.Name == "Own CPU Time").Value);

            // A leaf's own time is its reported time, and saying it twice is noise.
            var scan = TestPlans.Operator(statement, 2).Properties.Single(p => p.Name == "Actual Execution");
            Assert.IsFalse(scan.Children.Any(p => p.Name.StartsWith("Own", System.StringComparison.Ordinal)));
        }

        [TestMethod]
        public void Tooltip_ShowsTheChosenTimeWithTheOtherBesideIt()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(Parse(SerialRowMode()));
            var join = layout.Nodes.Single(n => n.Operator?.NodeId == 0);

            var own = PlanTooltipBuilder.Build(join, timeMode: OperatorTimeMode.Own);
            Assert.AreEqual("20 ms  (100 ms cumulative)", own.Rows.Single(r => r.Label == "Elapsed time").Value);

            var reported = PlanTooltipBuilder.Build(join, timeMode: OperatorTimeMode.AsReported);
            Assert.AreEqual("100 ms  (20 ms this node)", reported.Rows.Single(r => r.Label == "Elapsed time").Value);

            // No second figure where the two are the same.
            var scan = layout.Nodes.Single(n => n.Operator?.NodeId == 2);
            Assert.AreEqual("30 ms", PlanTooltipBuilder.Build(scan).Rows.Single(r => r.Label == "Elapsed time").Value);
        }

        [TestMethod]
        public void Controller_SwitchingTimeModeKeepsTheSelection()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout);
            controller.SetViewport(new LayoutSize(800, 600));

            controller.Select(layout.Nodes.Single(n => n.Operator?.NodeId == 1));
            PlanNode? raised = null;
            controller.SelectionChanged += (_, node) => raised = node;

            controller.OperatorTimeMode = OperatorTimeMode.AsReported;

            // The nodes were rebuilt, so the selection is found again by operator and the host told.
            Assert.AreEqual(OperatorTimeMode.AsReported, layout.OperatorTimeMode);
            Assert.AreEqual(1, controller.SelectedNode!.Operator!.NodeId);
            Assert.AreSame(controller.SelectedNode, raised);
            Assert.AreEqual("Elapsed 900 ms · CPU 1.28 s", controller.SelectedNode.TimingLine);
        }

        [TestMethod]
        public void Controller_SwitchingTimeModeWithNothingSelectedSelectsNothing()
        {
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(TestPlans.Statement(TestPlans.ParallelSpill));
            var controller = new PlanViewController(layout);

            controller.OperatorTimeMode = OperatorTimeMode.AsReported;

            // Selecting something here would open the properties panel on a click that had nothing
            // to do with it.
            Assert.IsNull(controller.SelectedNode);
        }
    }
}
