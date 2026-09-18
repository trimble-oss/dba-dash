using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.QueryPlan.Skia.Test
{
    [TestClass]
    public class PlanOperatorGlyphsTests
    {
        /// <summary>
        /// The most valuable test in this project.
        ///
        /// The glyphs are SVG path data in string literals, so a mistyped command or a missing
        /// number is not a compile error - it is a silently empty icon, or a parse that returns null
        /// and takes the whole plan down with it, and only for the one operator that happens to be
        /// in the plan somebody opened.  Parsing every one of them here is what makes that a build
        /// failure instead.
        /// </summary>
        [TestMethod]
        public void EveryKindHasGlyphPathsThatParse()
        {
            foreach (var kind in Enum.GetValues<PlanOperatorKind>())
            {
                var (stroke, fill, accent) = PlanOperatorGlyphs.PathsFor(kind);

                Assert.IsNotNull(stroke, $"{kind} has no stroke path.");
                Assert.IsFalse(stroke.IsEmpty, $"{kind} parsed to an empty path - check its path data.");

                if (PlanOperatorGlyphs.For(kind).Fill is not null)
                {
                    Assert.IsNotNull(fill, $"{kind} declares a fill path that did not parse.");
                    Assert.IsFalse(fill!.IsEmpty, $"{kind} has an empty fill path.");
                }

                if (PlanOperatorGlyphs.For(kind).Accent is not null)
                {
                    Assert.IsNotNull(accent, $"{kind} declares an accent path that did not parse.");
                    Assert.IsFalse(accent!.IsEmpty, $"{kind} has an empty accent path.");
                }
            }
        }

        /// <summary>
        /// The operator reference is only worth reading if it covers the whole set and says what
        /// each symbol means - a symbol added without a meaning would show up on the page with a
        /// blank where the explanation should be.
        /// </summary>
        [TestMethod]
        public void EverySymbolSaysWhatItStandsFor()
        {
            foreach (var kind in Enum.GetValues<PlanOperatorKind>())
            {
                var glyph = PlanOperatorGlyphs.For(kind);

                Assert.IsFalse(string.IsNullOrWhiteSpace(glyph.Name), $"{kind}'s symbol has no name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(glyph.Meaning), $"{kind}'s symbol does not say what it stands for.");
            }
        }

        [TestMethod]
        public void TheOperatorReferenceShowsEveryOperatorAndItsPathData()
        {
            var html = PlanOperatorReference.BuildHtml(new DateTime(2026, 1, 1));

            foreach (var kind in Enum.GetValues<PlanOperatorKind>())
            {
                var glyph = PlanOperatorGlyphs.For(kind);

                StringAssert.Contains(html, "PlanOperatorKind." + kind + "<", $"{kind} is missing from the reference.");
                StringAssert.Contains(html, System.Net.WebUtility.HtmlEncode(PlanOperatorDescriptions.For(kind)), $"{kind} does not say what it does.");

                foreach (var (physicalOp, description) in PlanOperatorDescriptions.VariantsOf(kind))
                {
                    StringAssert.Contains(html, System.Net.WebUtility.HtmlEncode(description), $"{physicalOp} does not say what it does.");
                }

                // What it is drawn from: its SVG file where it has one, otherwise its glyph's path data.
                if (PlanOperatorIcons.SvgFor(kind) is { } svg)
                {
                    StringAssert.Contains(html, "Icons/" + svg.FileName, $"{kind}'s SVG file is not named.");
                }
                else
                {
                    StringAssert.Contains(html, System.Net.WebUtility.HtmlEncode(glyph.Stroke), $"{kind}'s path data is missing.");
                }
            }

            // Every category chip colour is defined for both themes, or its chips draw black.
            foreach (var category in Enum.GetValues<PlanOperatorCategory>())
            {
                Assert.AreEqual(2, CountOf(html, "--cat-" + category + ":"), $"{category} needs a light and a dark colour.");
            }
        }

        private static int CountOf(string text, string value)
        {
            var count = 0;
            for (var at = text.IndexOf(value, StringComparison.Ordinal); at >= 0; at = text.IndexOf(value, at + 1, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// The symbols are drawn on a 24 unit grid and scaled onto the icon chip, so anything
        /// outside that grid is drawn outside the chip - over the node's title, or off the node
        /// altogether.  A stray digit in a path is exactly how that happens.
        /// </summary>
        [TestMethod]
        public void EveryGlyphStaysInsideItsGrid()
        {
            // A little slack for the stroke, which is centred on the path and so hangs half its
            // width outside it.
            const float tolerance = 1.5f;

            foreach (var kind in Enum.GetValues<PlanOperatorKind>())
            {
                var (stroke, fill, accent) = PlanOperatorGlyphs.PathsFor(kind);

                foreach (var path in new[] { stroke, fill, accent })
                {
                    if (path is null) continue;

                    var bounds = path.Bounds;
                    Assert.IsTrue(bounds.Left >= -tolerance, $"{kind} extends left of the grid: {bounds}");
                    Assert.IsTrue(bounds.Top >= -tolerance, $"{kind} extends above the grid: {bounds}");
                    Assert.IsTrue(
                        bounds.Right <= PlanOperatorGlyphs.GlyphExtent + tolerance,
                        $"{kind} extends right of the grid: {bounds}");
                    Assert.IsTrue(
                        bounds.Bottom <= PlanOperatorGlyphs.GlyphExtent + tolerance,
                        $"{kind} extends below the grid: {bounds}");
                }
            }
        }

        /// <summary>
        /// A glyph that fills only a corner of its chip reads as a mistake beside the others, and a
        /// truncated path is exactly what that looks like.
        ///
        /// Measured on the longer side rather than both, because some symbols are legitimately flat
        /// or narrow - an equals sign for Compute Scalar is six units tall and right to be - so a
        /// square-ish minimum would reject good artwork.  What is being caught is a symbol that is
        /// small in every direction.
        /// </summary>
        [TestMethod]
        public void EveryGlyphFillsAReasonableShareOfItsGrid()
        {
            foreach (var kind in Enum.GetValues<PlanOperatorKind>())
            {
                var (stroke, _, _) = PlanOperatorGlyphs.PathsFor(kind);
                var bounds = stroke.Bounds;

                var longer = Math.Max(bounds.Width, bounds.Height);
                var shorter = Math.Min(bounds.Width, bounds.Height);

                Assert.IsTrue(
                    longer >= 12,
                    $"{kind} spans only {longer:0.#} of a 24 unit grid at its longest.");

                Assert.IsTrue(
                    shorter >= 5,
                    $"{kind} is only {shorter:0.#} units across at its narrowest.");
            }
        }

        [TestMethod]
        public void PathsAreCachedRatherThanReparsedPerFrame()
        {
            // A plan redraws on every mouse move, so parsing path data per frame would be a real
            // cost for a result that never changes.
            var (first, _, _) = PlanOperatorGlyphs.PathsFor(PlanOperatorKind.HashMatchJoin);
            var (second, _, _) = PlanOperatorGlyphs.PathsFor(PlanOperatorKind.HashMatchJoin);

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void UnmappedKindsFallBackToANeutralGlyph()
        {
            // Every kind is mapped today, but a kind added without a glyph must draw something
            // rather than crash on a missing dictionary entry.
            Assert.IsNotNull(PlanOperatorGlyphs.For(PlanOperatorKind.Unknown));
            Assert.IsNotNull(PlanOperatorGlyphs.PathsFor(PlanOperatorKind.Unknown).Stroke);
        }

        /// <summary>
        /// The operators a reader most needs to tell apart must not share a picture.  Sharing is
        /// fine in general - a hash join and a hash aggregate both say "hash" - but these pairs are
        /// the ones where the difference is the whole point of looking at the plan.
        /// </summary>
        [TestMethod]
        public void TheOperatorsWorthDistinguishingHaveDistinctGlyphs()
        {
            var mustDiffer = new (PlanOperatorKind A, PlanOperatorKind B)[]
            {
                // A scan and a seek on the same index are the difference between a working index and
                // one being read end to end.
                (PlanOperatorKind.NonClusteredIndexScan, PlanOperatorKind.NonClusteredIndexSeek),

                // Clustered means the table itself, which changes what you would do about a scan.
                (PlanOperatorKind.ClusteredIndexScan, PlanOperatorKind.NonClusteredIndexScan),
                (PlanOperatorKind.ClusteredIndexSeek, PlanOperatorKind.NonClusteredIndexSeek),

                // A key lookup is the thing people go hunting for.
                (PlanOperatorKind.KeyLookup, PlanOperatorKind.ClusteredIndexSeek),

                // A sort feeding a Top usually wants an index; a plain sort is often just the query.
                (PlanOperatorKind.Sort, PlanOperatorKind.TopSort),

                // The three exchanges call for three different responses.
                (PlanOperatorKind.GatherStreams, PlanOperatorKind.RepartitionStreams),
                (PlanOperatorKind.DistributeStreams, PlanOperatorKind.RepartitionStreams),

                // The join algorithms.
                (PlanOperatorKind.HashMatchJoin, PlanOperatorKind.MergeJoin),
                (PlanOperatorKind.HashMatchJoin, PlanOperatorKind.NestedLoops),
                (PlanOperatorKind.MergeJoin, PlanOperatorKind.NestedLoops)
            };

            foreach (var (a, b) in mustDiffer)
            {
                Assert.AreNotEqual(
                    PlanOperatorGlyphs.For(a).Stroke,
                    PlanOperatorGlyphs.For(b).Stroke,
                    $"{a} and {b} are drawn with the same symbol, and a reader has to tell them apart.");
            }
        }

        /// <summary>
        /// Every category has to be recognisable from the others by hue, because the chip colour is
        /// the coarse sort a reader does before reading any labels.
        /// </summary>
        [TestMethod]
        public void EveryCategoryHasItsOwnChipColour()
        {
            foreach (var palette in new[] { PlanPalette.Light(), PlanPalette.Dark() })
            {
                var colours = Enum.GetValues<PlanOperatorCategory>()
                    .Select(palette.IconFor)
                    .ToList();

                Assert.AreEqual(
                    colours.Count,
                    colours.Distinct().Count(),
                    "Two operator categories share a chip colour.");
            }
        }
    }
}
