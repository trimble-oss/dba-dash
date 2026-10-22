using DBADash.QueryPlan;
using DBADash.QueryPlan.Live;
using DBADashGUI.QueryPlans;
using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    [TestClass]
    public class LivePlanRefreshTests
    {
        private const string LivePlan = """
            <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.564" Build="16.0.4222.2">
              <BatchSequence><Batch><Statements>
                <StmtSimple StatementId="1" StatementText="SELECT 1" StatementType="SELECT" StatementSubTreeCost="1">
                  <QueryPlan DegreeOfParallelism="1" MemoryGrant="1024" CachedPlanSize="16" CompileTime="1" CompileCPU="1" CompileMemory="100">
                    <QueryTimeStats ElapsedTime="100" CpuTime="90" />
                    <RelOp NodeId="0" PhysicalOp="Nested Loops" LogicalOp="Inner Join" EstimateRows="1000" EstimateIO="0" EstimateCPU="0.1" AvgRowSize="100" EstimatedTotalSubtreeCost="1" Parallel="0" EstimateRebinds="0" EstimateRewinds="0" EstimatedExecutionMode="Row">
                      <OutputList />
                      <Warnings NoJoinPredicate="1" />
                      <RunTimeInformation>
                        <RunTimeCountersPerThread Thread="0" ActualRows="10" ActualEndOfScans="0" ActualExecutions="1" ActualExecutionMode="Row" />
                      </RunTimeInformation>
                      <NestedLoops Optimized="0">
                        <RelOp NodeId="1" PhysicalOp="Clustered Index Scan" LogicalOp="Clustered Index Scan" EstimateRows="10" EstimateIO="0.1" EstimateCPU="0.1" AvgRowSize="100" EstimatedTotalSubtreeCost="0.5" TableCardinality="1000" Parallel="0" EstimateRebinds="0" EstimateRewinds="0" EstimatedExecutionMode="Row">
                          <OutputList />
                          <RunTimeInformation>
                            <RunTimeCountersPerThread Thread="0" ActualRows="5" ActualEndOfScans="0" ActualExecutions="1" ActualExecutionMode="Row" />
                          </RunTimeInformation>
                          <IndexScan Ordered="0" ForcedIndex="0" NoExpandHint="0">
                            <Object Database="[db]" Schema="[dbo]" Table="[t]" Index="[pk]" IndexKind="Clustered" Storage="RowStore" />
                          </IndexScan>
                        </RelOp>
                        <RelOp NodeId="2" PhysicalOp="Clustered Index Seek" LogicalOp="Clustered Index Seek" EstimateRows="1" EstimateIO="0.1" EstimateCPU="0.1" AvgRowSize="100" EstimatedTotalSubtreeCost="0.5" TableCardinality="1000" Parallel="0" EstimateRebinds="9" EstimateRewinds="0" EstimatedExecutionMode="Row">
                          <OutputList />
                          <RunTimeInformation>
                            <RunTimeCountersPerThread Thread="0" ActualRows="5" ActualEndOfScans="5" ActualExecutions="5" ActualExecutionMode="Row" />
                          </RunTimeInformation>
                          <IndexScan Ordered="1" ForcedIndex="0" NoExpandHint="0">
                            <Object Database="[db]" Schema="[dbo]" Table="[u]" Index="[pk]" IndexKind="Clustered" Storage="RowStore" />
                          </IndexScan>
                        </RelOp>
                      </NestedLoops>
                    </RelOp>
                  </QueryPlan>
                </StmtSimple>
              </Statements></Batch></BatchSequence>
            </ShowPlanXML>
            """;

        private sealed class FakeSource : ILivePlanSource
        {
            private long _rows = 10;

            public string Description => "Session 1 on test";

            public Task<LivePlanCapture> CaptureAsync(CancellationToken cancellationToken)
            {
                _rows += 10;
                var counters = new[]
                {
                    new PlanLiveCounters { NodeId = 0, Rows = _rows, Rebinds = 1 },
                    new PlanLiveCounters { NodeId = 1, Rows = _rows / 2, Rebinds = 1 },
                    new PlanLiveCounters { NodeId = 2, Rows = _rows / 2, Rebinds = _rows / 2, EndOfScans = _rows / 2 }
                };
                return Task.FromResult(new LivePlanCapture
                {
                    PlanXml = PlanLivePatcher.Apply(LivePlan, counters),
                    Live = PlanLiveSnapshot.From(counters, null, DateTime.UtcNow),
                    CapturedAt = DateTime.Now
                });
            }
        }

        private sealed class FailingSource(string message) : ILivePlanSource
        {
            public string Description => "Session 1 on test";

            public Task<LivePlanCapture> CaptureAsync(CancellationToken cancellationToken) =>
                Task.FromException<LivePlanCapture>(new InvalidOperationException(message));
        }

        /// <summary>
        /// A long message - a failure, usually - was pushed off the end of the Live bar into its overflow menu.  It is
        /// cut to the room left, with an ellipsis, and the whole of it is the tooltip.
        /// </summary>
        [STATestMethod]
        public void LongStatusIsTruncatedToTheLiveBar()
        {
            var message = "A live plan is not available for session 1.  " + string.Concat(Enumerable.Repeat("This explanation goes on for a long time.  ", 10));

            using var form = new Form { Width = 700, Height = 400, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            var viewer = new QueryPlanViewerControl(PlanParser.Parse(LivePlan), LivePlan, "live.sqlplan", null, new FailingSource(message)) { Dock = DockStyle.Fill };
            form.Controls.Add(viewer);
            form.Show();
            Pump();

            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var refresh = (Task)viewer.GetType().GetMethod("RefreshLiveAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(viewer, null)!;
            while (!refresh.IsCompleted) Pump();
            Pump();

            var status = Field<ToolStripLabel>(viewer, "_liveStatus");
            StringAssert.StartsWith(status.Text, "Refresh failed: A live plan");
            StringAssert.EndsWith(status.Text, "…", "Expected the message to be cut to fit.");
            Assert.IsFalse(status.IsOnOverflow, "The status shouldn't be pushed into the overflow menu.");
            StringAssert.Contains(status.ToolTipText, message, "The tooltip should have the whole message.");
            StringAssert.Contains(status.ToolTipText, nameof(InvalidOperationException), "The tooltip should have the detail.");
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

        private static void Pump()
        {
            for (var i = 0; i < 20; i++)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// The cards were disposed and recreated on every refresh, which flickered.  They are kept, and their links
        /// re-pointed at the newer capture's operators.
        /// </summary>
        [STATestMethod]
        public void Refresh_ReusesTheCardsAndRepointsTheirLinks()
        {
            using var form = new Form { Width = 1000, Height = 500, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            var viewer = new QueryPlanViewerControl(PlanParser.Parse(LivePlan), LivePlan, "live.sqlplan", null, new FakeSource()) { Dock = DockStyle.Fill };
            form.Controls.Add(viewer);
            form.Show();
            Pump();

            var properties = Field<QueryPlanPropertiesControl>(viewer, "_properties");
            var cards = Field<System.Collections.IList>(properties, "_cards");
            Assert.IsTrue(cards.Count > 0, "The overview of a plan with a warning should have cards.");
            var before = cards.Cast<object>().Select(c => c.GetType().GetField("Item1")!.GetValue(c)).ToList();

            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var refresh = (Task)viewer.GetType().GetMethod("RefreshLiveAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(viewer, null)!;
            while (!refresh.IsCompleted) Pump();
            Pump();

            var after = cards.Cast<object>().Select(c => c.GetType().GetField("Item1")!.GetValue(c)).ToList();
            CollectionAssert.AreEqual(before, after, "The cards should be the same controls after a refresh.");

            // Every link goes to an operator of the plan now showing.
            var graph = Field<QueryPlanGraphControl>(viewer, "_graphControl");
            var current = graph.Statement.RootOperator!.DescendantsAndSelf().ToList();
            var requested = new System.Collections.Generic.List<DBADash.QueryPlan.Model.PlanOperator>();
            properties.OperatorRequested += (_, op) => requested.Add(op);

            // Taken first: following a link selects its operator, which refills the cards.
            var content = Field<System.Collections.IList>(properties, "_cardContent");
            var links = content.Cast<object>()
                .SelectMany(entry => ((System.Collections.Generic.Dictionary<string, Action>)entry.GetType().GetField("Item2")!.GetValue(entry)!).Values)
                .ToList();
            foreach (var action in links) action();

            Assert.IsTrue(requested.Count > 0, "Expected the cards to have links to operators.");
            Assert.IsTrue(requested.All(op => current.Any(c => ReferenceEquals(c, op))), "A card link went to an operator of the old capture.");
        }

        /// <summary>
        /// A refresh re-selects the operator on the newer capture, which re-fills the properties panel - and used to
        /// scroll it back to the top every few seconds.  Checked for an operator and for the statement overview, with a
        /// row clicked, over several refreshes.
        /// </summary>
        [STATestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Refresh_KeepsThePropertiesScrollPosition(bool operatorSelected)
        {
            using var form = new Form { Width = 1000, Height = 300, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            var viewer = new QueryPlanViewerControl(PlanParser.Parse(LivePlan), LivePlan, "live.sqlplan", null, new FakeSource()) { Dock = DockStyle.Fill };
            form.Controls.Add(viewer);
            form.Show();
            Pump();

            var graph = Field<QueryPlanGraphControl>(viewer, "_graphControl");
            if (operatorSelected) graph.SelectOperator(graph.Statement.RootOperator!.Children[1]);
            Pump();

            var properties = Field<QueryPlanPropertiesControl>(viewer, "_properties");
            var grid = Field<DataGridView>(properties, "_grid");
            Assert.IsTrue(grid.Visible, "The properties panel should be open - an operator, or the overview of a plan with a warning.");
            Assert.IsTrue(grid.Rows.Count > 8, $"Need enough properties to scroll: {grid.Rows.Count}");

            grid.Focus();
            grid.CurrentCell = grid.Rows[5].Cells[1];
            grid.FirstDisplayedScrollingRowIndex = 3;
            Pump();
            var before = grid.FirstDisplayedScrollingRowIndex;
            Assert.AreEqual(3, before, "Couldn't scroll the grid to start with.");

            var refreshLive = viewer.GetType().GetMethod("RefreshLiveAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            for (var i = 0; i < 3; i++)
            {
                // The app refreshes from a timer on the UI thread; the test thread's context has to say so for the
                // refresh to come back to it.
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                var refresh = (Task)refreshLive.Invoke(viewer, null)!;
                while (!refresh.IsCompleted) Pump();
                Pump();

                Assert.AreEqual(before, grid.FirstDisplayedScrollingRowIndex, $"The properties scrolled on refresh {i}.");
                Assert.AreEqual(5, grid.CurrentCell?.RowIndex, $"The current row moved on refresh {i}.");
            }
        }
    }
}