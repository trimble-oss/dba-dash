using System;
using System.Linq;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Test
{
    [TestClass]
    public class PlanLegendTests
    {
        private static PlanLegendSection Section(string title) =>
            PlanLegend.Sections.Single(s => s.Title == title);

        [TestMethod]
        public void EveryEntryHasATitleAndADescription()
        {
            foreach (var entry in PlanLegend.Sections.SelectMany(s => s.Entries))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Title), "An entry has no title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Description), entry.Title + " has no description.");
            }
        }

        [TestMethod]
        public void EveryOperatorIsListedOnce()
        {
            // A new operator kind shows up in the legend without anyone remembering to add it, and a
            // kind is never listed twice.
            var listed = Section("Operators").Entries.Select(e => e.Operator).ToList();

            CollectionAssert.AreEquivalent(Enum.GetValues<PlanOperatorKind>(), listed);
        }

        [TestMethod]
        public void TheOperatorsAreGroupedByColourFamily()
        {
            var families = Section("Operators").Entries.Select(PlanLegend.CategoryOf).ToList();

            // Each family in one run, so a heading can stand over it.
            Assert.AreEqual(families.Distinct().Count(), families.Where((f, i) => i == 0 || f != families[i - 1]).Count());
        }

        [TestMethod]
        public void EveryBadgeThePlanDrawsIsExplained()
        {
            var explained = PlanLegend.Sections
                .SelectMany(s => s.Entries)
                .Where(e => e.Sample == PlanLegendSample.Badge)
                .Select(e => e.Badge)
                .ToList();

            // Whichever badges the layout can put in a strip are the ones the legend has to show.
            var drawn = Enum.GetValues<PlanNodeBadges>()
                .Where(b => b != PlanNodeBadges.None && PlanBadges.Count(b) == 1)
                .ToList();

            CollectionAssert.AreEquivalent(drawn, explained);
        }

        [TestMethod]
        public void BothWarningColoursAreExplained()
        {
            var samples = PlanLegend.Sections.SelectMany(s => s.Entries).Select(e => e.Sample).ToList();

            CollectionAssert.Contains(samples, PlanLegendSample.WarningMarker);
            CollectionAssert.Contains(samples, PlanLegendSample.CriticalWarningMarker);
        }

        [TestMethod]
        public void TheEstimateThresholdsQuotedAreTheOnesTheLayoutUses()
        {
            var text = string.Join(" ", Section("Arrows").Entries.Select(e => e.Title + " " + e.Description));

            StringAssert.Contains(text, PlanLayoutEngine.EstimateMismatchThreshold.ToString("0"));
            StringAssert.Contains(text, PlanLayoutEngine.EstimateCriticalThreshold.ToString("0"));
        }

        [TestMethod]
        public void TheKeysAndMouseActionsAreListed()
        {
            Assert.IsTrue(PlanLegend.Controls.Count > 0);
            Assert.IsTrue(PlanLegend.Controls.All(c => c.Input.Length > 0 && c.Action.Length > 0));
        }
    }
}
