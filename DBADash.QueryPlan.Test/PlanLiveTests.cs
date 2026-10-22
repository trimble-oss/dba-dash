using System;
using System.Linq;
using DBADash.QueryPlan.Live;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanLiveTests
    {
        // A live plan as sys.dm_exec_query_statistics_xml returns it under lightweight profiling: no timings, a Compute
        // Scalar with no counters, and an operator that hadn't started when it was captured.
        private const string LivePlan = """
            <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.564" Build="16.0.4222.2">
              <BatchSequence><Batch><Statements>
                <StmtSimple StatementId="1" StatementText="SELECT 1" StatementType="SELECT" StatementSubTreeCost="1">
                  <QueryPlan>
                    <QueryTimeStats ElapsedTime="100" CpuTime="90" />
                    <RelOp NodeId="0" PhysicalOp="Nested Loops" LogicalOp="Inner Join" EstimateRows="1000" AvgRowSize="100" EstimatedTotalSubtreeCost="1">
                      <OutputList />
                      <RunTimeInformation>
                        <RunTimeCountersPerThread Thread="0" ActualRows="10" ActualEndOfScans="0" ActualExecutions="1" ActualExecutionMode="Row" />
                      </RunTimeInformation>
                      <NestedLoops>
                        <RelOp NodeId="1" PhysicalOp="Compute Scalar" LogicalOp="Compute Scalar" EstimateRows="10" AvgRowSize="100" EstimatedTotalSubtreeCost="1">
                          <OutputList />
                          <ComputeScalar>
                            <RelOp NodeId="2" PhysicalOp="Table Spool" LogicalOp="Lazy Spool" EstimateRows="10" AvgRowSize="100" EstimatedTotalSubtreeCost="1">
                              <OutputList />
                              <RunTimeInformation>
                                <RunTimeCountersPerThread Thread="0" ActualRows="5" ActualRebinds="1" ActualRewinds="2" ActualEndOfScans="2" ActualExecutions="3" ActualExecutionMode="Row" />
                              </RunTimeInformation>
                              <Spool />
                            </RelOp>
                          </ComputeScalar>
                        </RelOp>
                        <RelOp NodeId="3" PhysicalOp="Clustered Index Seek" LogicalOp="Clustered Index Seek" EstimateRows="1" AvgRowSize="100" EstimatedTotalSubtreeCost="1">
                          <OutputList />
                          <IndexScan />
                        </RelOp>
                      </NestedLoops>
                    </RelOp>
                  </QueryPlan>
                </StmtSimple>
              </Statements></Batch></BatchSequence>
            </ShowPlanXML>
            """;

        private static PlanLiveCounters C(int node, long rows, long rebinds = 1, long rewinds = 0, long endOfScans = 0, int thread = 0) =>
            new() { NodeId = node, Thread = thread, Rows = rows, Rebinds = rebinds, Rewinds = rewinds, EndOfScans = endOfScans };

        [TestMethod]
        public void Apply_UpdatesTheCountersOfEachOperator()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(0, 500), C(2, 40, rebinds: 1, rewinds: 9, endOfScans: 9)]);

            var statement = PlanParser.Parse(xml).Statements[0];
            Assert.AreEqual(500, TestPlans.Operator(statement, 0).Runtime!.ActualRows);

            var spool = TestPlans.Operator(statement, 2).Runtime!;
            Assert.AreEqual(40, spool.ActualRows);
            Assert.AreEqual(10, spool.Threads[0].ActualExecutions, "Executions are rebinds plus rewinds.");
            Assert.AreEqual(9, spool.Threads[0].ActualEndOfScans);
            StringAssert.Contains(xml, "ActualRewinds=\"9\"");
        }

        [TestMethod]
        public void Apply_AddsCountersToAnOperatorThatHadNotStarted()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(3, 7)]);

            var seek = TestPlans.Operator(PlanParser.Parse(xml).Statements[0], 3);
            Assert.IsNotNull(seek.Runtime);
            Assert.AreEqual(7, seek.Runtime.ActualRows);
        }

        [TestMethod]
        public void Apply_AddsAThreadThatWasNotRunning()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(0, 3, thread: 0), C(0, 4, thread: 1)]);

            var runtime = TestPlans.Operator(PlanParser.Parse(xml).Statements[0], 0).Runtime!;
            Assert.AreEqual(2, runtime.Threads.Count);
            Assert.AreEqual(7, runtime.ActualRows);
            Assert.AreEqual("Row", runtime.Threads[1].ActualExecutionMode, "The new thread takes the operator's execution mode.");
        }

        [TestMethod]
        public void Apply_LeavesUnmeasuredOperatorsAlone()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(0, 500)]);

            Assert.IsNull(TestPlans.Operator(PlanParser.Parse(xml).Statements[0], 1).Runtime, "Compute Scalar has no counters to update.");
        }

        [TestMethod]
        public void Apply_DoesNotAddTimingsLightweightProfilingDidNotCollect()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(0, 500) with { ElapsedMs = 0, CpuMs = 0, LogicalReads = 0 }]);

            Assert.IsFalse(xml.Contains("ActualElapsedms"), "Zero times are lightweight profiling not measuring, not zero time.");
            Assert.IsFalse(xml.Contains("ActualLogicalReads"));
        }

        [TestMethod]
        public void Apply_SetsTimingsWhereTheyWereMeasured()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(0, 500) with { ElapsedMs = 1234, CpuMs = 1000, LogicalReads = 42 }]);

            var thread = TestPlans.Operator(PlanParser.Parse(xml).Statements[0], 0).Runtime!.Threads[0];
            Assert.AreEqual(1234, thread.ActualElapsedMs);
            Assert.AreEqual(1000, thread.ActualCpuMs);
            Assert.AreEqual(42, thread.ActualLogicalReads);
        }

        [TestMethod]
        public void Apply_LeavesTheStatementTimesFromThePlan()
        {
            var xml = PlanLivePatcher.Apply(LivePlan, [C(0, 500)]);

            var stats = PlanParser.Parse(xml).Statements[0].QueryTimeStats!;
            Assert.AreEqual(90, stats.CpuMs, "Request CPU is not this statement's time.");
            Assert.AreEqual(100, stats.ElapsedMs, "Request elapsed time is not this statement's time.");
        }

        [TestMethod]
        public void Snapshot_WithoutAnEarlierCaptureReportsNothingActive()
        {
            var snapshot = PlanLiveSnapshot.From([C(0, 10), C(2, 0, rebinds: 0), C(3, 5, endOfScans: 1)], null, DateTime.UtcNow);

            Assert.AreEqual(PlanLiveState.Waiting, snapshot.StateOf(0));
            Assert.AreEqual(PlanLiveState.NotStarted, snapshot.StateOf(2));
            Assert.AreEqual(PlanLiveState.Finished, snapshot.StateOf(3));
            Assert.IsNull(snapshot.StateOf(1), "Operators with no counters have no state.");
            Assert.IsFalse(snapshot.HasActive);
        }

        [TestMethod]
        public void Snapshot_OperatorsWhoseCountersMovedAreActive()
        {
            var before = new[] { C(0, 10), C(2, 4, rebinds: 1, rewinds: 1, endOfScans: 2), C(3, 5) };
            var now = new[] { C(0, 10), C(2, 6, rebinds: 1, rewinds: 2, endOfScans: 3), C(3, 5) };

            var snapshot = PlanLiveSnapshot.From(now, before, DateTime.UtcNow);

            Assert.AreEqual(PlanLiveState.Waiting, snapshot.StateOf(0));
            Assert.AreEqual(PlanLiveState.Active, snapshot.StateOf(2), "Moving rows wins over looking finished between executions.");
            Assert.AreEqual(PlanLiveState.Waiting, snapshot.StateOf(3));
            Assert.IsTrue(snapshot.HasActive);
        }

        [TestMethod]
        public void Snapshot_UsesOpenAndCloseTimesWhenTheyWereCollected()
        {
            var snapshot = PlanLiveSnapshot.From(
                [
                    C(0, 10) with { OpenTimeMs = 100 },
                    C(2, 0, rebinds: 0) with { OpenTimeMs = 100, CloseTimeMs = 200 },
                    C(3, 5, endOfScans: 1) with { OpenTimeMs = 0, CloseTimeMs = 0 }
                ],
                null, DateTime.UtcNow);

            Assert.AreEqual(PlanLiveState.Waiting, snapshot.StateOf(0));
            Assert.AreEqual(PlanLiveState.Finished, snapshot.StateOf(2), "Closed is finished, whatever the counters say.");
            Assert.AreEqual(PlanLiveState.Finished, snapshot.StateOf(3), "Without timestamps the counters decide.");
        }

        [TestMethod]
        public void Snapshot_UnusedThreadDoesNotKeepAClosedOperatorWaiting()
        {
            var snapshot = PlanLiveSnapshot.From(
                [
                    C(0, 10, thread: 0) with { OpenTimeMs = 0, CloseTimeMs = 0 },
                    C(0, 10, thread: 1) with { OpenTimeMs = 100, CloseTimeMs = 200 }
                ],
                null, DateTime.UtcNow);

            Assert.AreEqual(PlanLiveState.Finished, snapshot.StateOf(0), "A thread that never opened is not still running.");
        }

        [TestMethod]
        public void StatementText_FillsABlankLivePlan()
        {
            const string xml = """
                <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan">
                  <BatchSequence><Batch><Statements>
                    <StmtSimple StatementId="1" StatementText="" StatementType="SELECT"><QueryPlan /></StmtSimple>
                  </Statements></Batch></BatchSequence>
                </ShowPlanXML>
                """;

            var filled = PlanLiveStatementText.Apply(xml, "SELECT 1 FROM t");

            Assert.AreEqual("SELECT 1 FROM t", PlanParser.Parse(filled).Statements[0].StatementText);
        }

        [TestMethod]
        public void StatementText_LeavesAPlanThatAlreadyHasIt()
        {
            Assert.AreSame(LivePlan, PlanLiveStatementText.Apply(LivePlan, "SELECT 2"));
        }

        [TestMethod]
        public void Snapshot_SumsThreads()
        {
            var before = new[] { C(0, 10, thread: 1), C(0, 10, thread: 2) };
            var now = new[] { C(0, 10, thread: 1), C(0, 11, thread: 2) };

            Assert.AreEqual(PlanLiveState.Active, PlanLiveSnapshot.From(now, before, DateTime.UtcNow).StateOf(0));
            Assert.AreEqual(1, PlanLiveSnapshot.From(now, before, DateTime.UtcNow).States.Count);
            Assert.IsTrue(PlanLiveSnapshot.From(now, before, DateTime.UtcNow).States.Keys.SequenceEqual([0]));
        }
    }
}
