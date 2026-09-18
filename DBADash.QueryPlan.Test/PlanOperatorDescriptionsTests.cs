using System;
using System.Linq;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DBADash.QueryPlan.Test.InlinePlan;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// What each operator does, as the tooltip, the properties panel and the operator reference say
    /// it - and the classification those descriptions depend on being right.
    /// </summary>
    [TestClass]
    public class PlanOperatorDescriptionsTests
    {
        private static readonly string Unrecognised = PlanOperatorDescriptions.For(PlanOperatorKind.Unknown);

        [TestMethod]
        public void EveryKind_SaysWhatItDoes()
        {
            foreach (var kind in Enum.GetValues<PlanOperatorKind>().Where(k => k != PlanOperatorKind.Unknown))
            {
                var text = PlanOperatorDescriptions.For(kind);

                Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"{kind} has no description.");
                Assert.AreNotEqual(Unrecognised, text, $"{kind} is described as an operator the viewer does not recognise.");
            }
        }

        [TestMethod]
        public void EveryPhysicalOperatorDescribed_IsClassifiedAsTheKindItIsListedUnder()
        {
            // A physical operator's own description is only used when it is that kind, so one listed
            // under the wrong kind would silently never be shown.
            foreach (var kind in Enum.GetValues<PlanOperatorKind>())
            {
                foreach (var (physicalOp, description) in PlanOperatorDescriptions.VariantsOf(kind))
                {
                    Assert.AreEqual(kind, PlanOperatorClassifier.Classify(physicalOp, null), physicalOp);
                    Assert.AreEqual(description, PlanOperatorDescriptions.For(kind, physicalOp, null), physicalOp);
                }
            }
        }

        [TestMethod]
        public void PhysicalOperatorsSharingAKind_AreDescribedSeparately()
        {
            var inserted = PlanOperatorDescriptions.For(PlanOperatorKind.ConstantScan, "Inserted Scan", "Inserted Scan");
            var deleted = PlanOperatorDescriptions.For(PlanOperatorKind.ConstantScan, "Deleted Scan", "Deleted Scan");
            var constant = PlanOperatorDescriptions.For(PlanOperatorKind.ConstantScan, "Constant Scan", "Constant Scan");

            StringAssert.Contains(inserted, "inserted table");
            StringAssert.Contains(deleted, "deleted table");
            Assert.AreEqual(PlanOperatorDescriptions.For(PlanOperatorKind.ConstantScan), constant);
        }

        [TestMethod]
        public void Spools_SayWhetherTheyAreEagerOrLazy()
        {
            StringAssert.Contains(PlanOperatorDescriptions.For(PlanOperatorKind.TableSpool, "Table Spool", "Eager Spool"), "This one is eager");
            StringAssert.Contains(PlanOperatorDescriptions.For(PlanOperatorKind.IndexSpool, "Index Spool", "Lazy Spool"), "This one is lazy");
            StringAssert.Contains(PlanOperatorDescriptions.For(PlanOperatorKind.Sort, "Sort", "Distinct Sort"), "removes duplicate rows");
            Assert.AreEqual(PlanOperatorDescriptions.For(PlanOperatorKind.Sort), PlanOperatorDescriptions.For(PlanOperatorKind.Sort, "Sort", "Sort"));
        }

        [TestMethod]
        public void Tooltip_CarriesTheOperatorsDescription()
        {
            var statement = Parse(Op(1, "Table Spool", "Eager Spool", ""));
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);

            var tooltip = PlanTooltipBuilder.Build(layout.Nodes.Single(n => n.Operator?.NodeId == 1));

            Assert.AreEqual(PlanOperatorDescriptions.For(PlanOperatorKind.TableSpool, "Table Spool", "Eager Spool"), tooltip.Description);
            Assert.IsNull(PlanTooltipBuilder.Build(layout.Root).Description, "The statement has its own properties rather than a description.");
        }

        [TestMethod]
        public void Descriptions_CanBeTurnedOff_AndTheTooltipShowingFollows()
        {
            var statement = Parse(Op(1, "Sort", "Sort", ""));
            var layout = new PlanLayoutEngine(new FakeTextMeasurer()).Layout(statement);
            var node = layout.Nodes.Single(n => n.Operator?.NodeId == 1);

            Assert.IsNull(PlanTooltipBuilder.Build(node, includeDescription: false).Description);

            var controller = new PlanViewController(layout, new PlanViewOptions { ShowOperatorDescriptions = false });
            controller.SetViewport(new LayoutSize(2000, 2000));
            controller.SetHover(controller.ToScreen(new LayoutPoint(node.Bounds.X + 5, node.Bounds.Y + 5)));

            Assert.AreSame(node, controller.HoveredNode);
            Assert.IsNull(controller.HoveredTooltip!.Description);

            controller.ShowOperatorDescriptions = true;
            Assert.AreEqual(PlanOperatorDescriptions.For(PlanOperatorKind.Sort), controller.HoveredTooltip!.Description, "The tooltip on screen changes with the setting.");
        }

        [TestMethod]
        public void ColumnstoreScans_AreRecognisedByTheirStorage()
        {
            // Showplan calls a columnstore scan an Index Scan or Clustered Index Scan; only the
            // object's storage says what it is.
            var statement = Parse("""
                <RelOp NodeId="1" PhysicalOp="Clustered Index Scan" LogicalOp="Clustered Index Scan" EstimateRows="1" EstimatedTotalSubtreeCost="1">
                  <IndexScan Ordered="false">
                    <Object Database="[Sales]" Schema="[dbo]" Table="[Orders]" Index="[CCI_Orders]" Storage="ColumnStore" />
                  </IndexScan>
                </RelOp>
                """);

            Assert.AreEqual(PlanOperatorKind.ColumnstoreIndexScan, statement.RootOperator!.Kind);
            Assert.AreEqual("Columnstore Index Scan", statement.RootOperator.DisplayName);

            Assert.AreEqual(PlanOperatorKind.NonClusteredIndexScan, PlanOperatorClassifier.Classify("Index Scan", "Index Scan", storage: "RowStore"));
            Assert.AreEqual(PlanOperatorKind.ColumnstoreIndexScan, PlanOperatorClassifier.Classify("Index Scan", "Index Scan", storage: "ColumnStore"));
        }
    }
}
