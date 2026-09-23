using System;
using System.Linq;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia.Test
{
    [TestClass]
    public class PlanLegendRenderingTests
    {
        private static readonly PlanLegendEntry[] Entries = PlanLegend.Sections.SelectMany(s => s.Entries).ToArray();

        /// <summary>Draws one entry at the size it asks for onto the palette's background.</summary>
        private static SKBitmap Draw(PlanRenderer renderer, PlanLegendEntry entry, float scale = 1)
        {
            var size = PlanRenderer.LegendSampleSize(entry);
            var bitmap = new SKBitmap((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale));

            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(renderer.Palette.Background);
            canvas.Scale(scale);
            renderer.DrawLegendSample(canvas, entry, scale);

            return bitmap;
        }

        private static int DrawnPixels(SKBitmap bitmap, SKColor background)
        {
            var count = 0;

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y) != background) count++;
                }
            }

            return count;
        }

        [TestMethod]
        public void EveryEntryDrawsSomething()
        {
            using var fonts = new PlanFonts();

            foreach (var palette in new[] { PlanPalette.Light(), PlanPalette.Dark() })
            {
                using var renderer = new PlanRenderer(fonts, palette);

                foreach (var entry in Entries)
                {
                    using var bitmap = Draw(renderer, entry, 2);

                    Assert.IsTrue(DrawnPixels(bitmap, palette.Background) > 20, entry.Title + " (" + entry.Sample + ") drew nothing.");
                }
            }
        }

        [TestMethod]
        public void AnOperatorIconIsTheChipThePlanDraws()
        {
            // The legend's icon and the plan's are one drawing routine: a legend entry for a kind has
            // the colour family's chip in it.
            using var fonts = new PlanFonts();
            using var renderer = new PlanRenderer(fonts);

            var entry = Entries.First(e => e.Sample == PlanLegendSample.OperatorIcon && e.Operator == PlanOperatorKind.Sort);
            using var bitmap = Draw(renderer, entry, 2);

            var chip = renderer.Palette.IconFor(PlanOperatorCategory.Transform);
            var matching = 0;

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (RenderFixture.IsNear(bitmap.GetPixel(x, y), chip, 4)) matching++;
                }
            }

            Assert.IsTrue(matching > bitmap.Width * bitmap.Height / 3, "The chip's colour did not fill the icon.");
        }

        [TestMethod]
        public void TheWarningAndCriticalMarkersAreTheirOwnColours()
        {
            using var fonts = new PlanFonts();
            using var renderer = new PlanRenderer(fonts);

            int Count(PlanLegendSample sample, SKColor colour)
            {
                using var bitmap = Draw(renderer, Entries.First(e => e.Sample == sample), 2);
                var count = 0;

                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (RenderFixture.IsNear(bitmap.GetPixel(x, y), colour, 4)) count++;
                    }
                }

                return count;
            }

            var palette = renderer.Palette;

            Assert.IsTrue(Count(PlanLegendSample.WarningMarker, palette.Warning) > 50);
            Assert.AreEqual(0, Count(PlanLegendSample.WarningMarker, palette.Critical));
            Assert.IsTrue(Count(PlanLegendSample.CriticalWarningMarker, palette.Critical) > 50);
            Assert.AreEqual(0, Count(PlanLegendSample.CriticalWarningMarker, palette.Warning));
        }

        [TestMethod]
        public void TheEstimateOutlineAppearsOnlyOnTheSamplesThatHaveOne()
        {
            using var fonts = new PlanFonts();
            using var renderer = new PlanRenderer(fonts);

            int Critical(PlanLegendSample sample)
            {
                using var bitmap = Draw(renderer, Entries.First(e => e.Sample == sample), 2);
                var count = 0;

                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        // Loose: the outline is a line a pixel or two wide, so few pixels are its exact colour.
                        if (RenderFixture.IsNear(bitmap.GetPixel(x, y), renderer.Palette.Critical, 60)) count++;
                    }
                }

                return count;
            }

            Assert.IsTrue(Critical(PlanLegendSample.ArrowUnderestimated) > 20);
            Assert.IsTrue(Critical(PlanLegendSample.ArrowOverestimated) > 20);
            Assert.AreEqual(0, Critical(PlanLegendSample.ArrowActual));
        }

        [TestMethod]
        public void TheSamplesFollowThePalette()
        {
            using var fonts = new PlanFonts();
            using var light = new PlanRenderer(fonts, PlanPalette.Light());
            using var dark = new PlanRenderer(fonts, PlanPalette.Dark());

            var entry = Entries.First(e => e.Sample == PlanLegendSample.ArrowActual);

            using var inLight = Draw(light, entry);
            using var inDark = Draw(dark, entry);

            Assert.AreNotEqual(inLight.GetPixel(inLight.Width / 2, inLight.Height / 2), inDark.GetPixel(inDark.Width / 2, inDark.Height / 2));
        }
    }
}
