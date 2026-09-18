using System;
using System.Collections.Generic;
using System.Linq;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia.Test
{
    /// <summary>
    /// SVG icons standing in for glyphs: how a file name is read, what happens to a bad file, and how
    /// each style is drawn.  The drawing tests swap in an SVG for the Key Lookup in the sample plan,
    /// rather than shipping a test icon in the product.
    /// </summary>
    [TestClass]
    public class PlanOperatorIconsTests
    {
        /// <summary>A plain red square, so what the renderer did to its colour is easy to count.</summary>
        private const string RedSquare =
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><rect width='24' height='24' fill='#FF0000'/></svg>";

        private const string BlackSquare =
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><rect width='24' height='24' fill='#000000'/></svg>";

        private static readonly SKColor Red = new(0xFF, 0x00, 0x00);

        private static PlanSvgIcon Icon(string fileName, string source = RedSquare)
        {
            var problems = new List<string>();
            return PlanOperatorIcons.Load(fileName, source, problems)
                   ?? throw new InvalidOperationException(string.Join("; ", problems));
        }

        // ---------------------------------------------------------------- file names

        [TestMethod]
        public void ParseFileName_ReadsTheKindAndTheStyle()
        {
            Assert.AreEqual((PlanOperatorKind.ComputeScalar, PlanIconStyle.TintedSvg), PlanOperatorIcons.ParseFileName("ComputeScalar.svg"));
            Assert.AreEqual((PlanOperatorKind.ComputeScalar, PlanIconStyle.FullColourSvg), PlanOperatorIcons.ParseFileName("ComputeScalar.color.svg"));
            Assert.AreEqual((PlanOperatorKind.ComputeScalar, PlanIconStyle.FullColourSvg), PlanOperatorIcons.ParseFileName("ComputeScalar.colour.svg"));

            // Nobody should have to get the capitals of an enum right to add an icon.
            Assert.AreEqual((PlanOperatorKind.KeyLookup, PlanIconStyle.FullColourSvg), PlanOperatorIcons.ParseFileName("keylookup.COLOR.svg"));
        }

        [TestMethod]
        public void ParseFileName_RejectsWhatNamesNoOperator()
        {
            Assert.IsNull(PlanOperatorIcons.ParseFileName("NotAnOperator.svg"));
            Assert.IsNull(PlanOperatorIcons.ParseFileName("ComputeScalar.png"));
            Assert.IsNull(PlanOperatorIcons.ParseFileName("7.svg"), "An enum's number is not its name.");
            Assert.IsNull(PlanOperatorIcons.ParseFileName(".color.svg"));
        }

        // ---------------------------------------------------------------- loading

        /// <summary>
        /// Every file in the Icons folder is in use.  A misspelt name or an SVG that does not load
        /// would otherwise go unnoticed, because the operator quietly keeps its glyph.
        /// </summary>
        [TestMethod]
        public void EveryIconFileIsInUse()
        {
            Assert.AreEqual(0, PlanOperatorIcons.Problems.Count, string.Join(Environment.NewLine, PlanOperatorIcons.Problems));
        }

        [TestMethod]
        public void Load_TurnsAwayAFileThatIsNotSvg()
        {
            var problems = new List<string>();

            Assert.IsNull(PlanOperatorIcons.Load("KeyLookup.svg", "<not svg", problems));
            StringAssert.Contains(problems.Single(), "did not load");
        }

        [TestMethod]
        public void Load_TurnsAwayAFileNamedAfterNoOperator()
        {
            var problems = new List<string>();

            Assert.IsNull(PlanOperatorIcons.Load("KeyLookups.svg", RedSquare, problems));
            StringAssert.Contains(problems.Single(), "not named after an operator kind");
        }

        [TestMethod]
        public void Overrides_ReplaceOnlyTheirOwnOperator()
        {
            var before = PlanOperatorIcons.SvgFor(PlanOperatorKind.KeyLookup);

            using (PlanOperatorIcons.OverrideForTesting(Icon("KeyLookup.svg")))
            {
                Assert.AreEqual(PlanIconStyle.TintedSvg, PlanOperatorIcons.StyleFor(PlanOperatorKind.KeyLookup));
                Assert.AreEqual(PlanIconStyle.Glyph, PlanOperatorIcons.StyleFor(PlanOperatorKind.RidLookup),
                    "An icon is per operator, even where the glyph is shared.");
            }

            // Whatever the project really ships comes back once the test is done.
            Assert.AreSame(before, PlanOperatorIcons.SvgFor(PlanOperatorKind.KeyLookup));
        }

        // ---------------------------------------------------------------- drawing

        [TestMethod]
        public void Render_RecoloursATintedSvgsBlackToTheSymbolColour()
        {
            using var icons = PlanOperatorIcons.OverrideForTesting(Icon("KeyLookup.svg", BlackSquare));
            using var fixture = LookupFixture(out var chip);

            Assert.IsTrue(Share(fixture, chip, fixture.Renderer.Palette.IconSymbol) > 0.4,
                "The square should fill the symbol area in the symbol colour.");
            Assert.IsTrue(Share(fixture, chip, fixture.Renderer.Palette.IconFor(PlanOperatorCategory.DataAccess)) > 0.1,
                "The chip is still drawn behind it, where the square leaves it showing.");
        }

        [TestMethod]
        public void Render_RecoloursATintedSvgsRedToTheAccentColour()
        {
            using var icons = PlanOperatorIcons.OverrideForTesting(Icon("KeyLookup.svg"));
            using var fixture = LookupFixture(out var chip);

            // The red itself is gone, and the square is in the theme's accent instead.
            Assert.AreEqual(0, fixture.PixelsNear(Red, 40));
            Assert.IsTrue(Share(fixture, chip, fixture.Renderer.Palette.IconAccent) > 0.4,
                "The square should fill the symbol area in the accent colour.");
            Assert.IsTrue(Share(fixture, chip, fixture.Renderer.Palette.IconFor(PlanOperatorCategory.DataAccess)) > 0.1,
                "The chip is still drawn behind it.");
        }

        [TestMethod]
        public void Render_DrawsAFullColourSvgInItsOwnColoursWithNoChip()
        {
            using var icons = PlanOperatorIcons.OverrideForTesting(Icon("KeyLookup.color.svg"));
            using var fixture = LookupFixture(out var chip);

            Assert.IsTrue(Share(fixture, chip, Red) > 0.8, "The square should fill the icon, in red.");
            Assert.AreEqual(0, Share(fixture, chip, fixture.Renderer.Palette.IconFor(PlanOperatorCategory.DataAccess)),
                "No chip behind a full colour icon.");
        }

        [TestMethod]
        public void Reference_SaysWhichOperatorsAreDrawnFromSvg()
        {
            using var icons = PlanOperatorIcons.OverrideForTesting(Icon("KeyLookup.color.svg"));

            var html = PlanOperatorReference.BuildHtml(new DateTime(2026, 1, 1));

            StringAssert.Contains(html, "Icons/KeyLookup.color.svg");
            StringAssert.Contains(html, "drawn in its own colours");
            StringAssert.Contains(html, "data:image/svg+xml;base64,");
        }

        /// <summary>The key lookup sample drawn at twice size, and where its icon chip landed.</summary>
        private static RenderFixture LookupFixture(out SKRectI chip)
        {
            var fixture = new RenderFixture(RenderFixture.KeyLookupSeek);
            var lookup = fixture.Layout.Nodes.Single(n => n.Operator?.Kind == PlanOperatorKind.KeyLookup);

            fixture.Controller.SetZoom(2, new Layout.LayoutPoint(0, 0));
            fixture.Controller.SelectOperator(lookup.Operator);
            fixture.Render();

            var bounds = fixture.Controller.ToScreen(lookup.IconBounds);
            chip = new SKRectI((int)Math.Ceiling(bounds.X), (int)Math.Ceiling(bounds.Y),
                (int)Math.Floor(bounds.Right), (int)Math.Floor(bounds.Bottom));
            return fixture;
        }

        /// <summary>The share of a rectangle's pixels close to a colour.</summary>
        private static double Share(RenderFixture fixture, SKRectI area, SKColor colour)
        {
            using var bitmap = fixture.Snapshot();
            var near = 0;
            var total = 0;

            for (var x = area.Left; x < area.Right; x++)
            {
                for (var y = area.Top; y < area.Bottom; y++)
                {
                    total++;
                    var pixel = bitmap.GetPixel(x, y);
                    if (Math.Abs(pixel.Red - colour.Red) <= 40 &&
                        Math.Abs(pixel.Green - colour.Green) <= 40 &&
                        Math.Abs(pixel.Blue - colour.Blue) <= 40)
                    {
                        near++;
                    }
                }
            }

            return total == 0 ? 0 : near / (double)total;
        }
    }
}
