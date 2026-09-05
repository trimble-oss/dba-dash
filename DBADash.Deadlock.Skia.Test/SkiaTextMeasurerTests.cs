using System;
using DBADash.Deadlock.Layout;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace DBADash.Deadlock.Skia.Test
{
    /// <summary>
    /// The measurer is what lets the layout engine size nodes without knowing about fonts, and the
    /// elision helper is what copes with the widths the engine clamps to.
    /// </summary>
    [TestClass]
    public class SkiaTextMeasurerTests
    {
        [TestMethod]
        public void Measure_ReturnsAPositiveSize()
        {
            using var fonts = new DeadlockFonts();
            var measurer = new SkiaTextMeasurer(fonts);

            var size = measurer.Measure("SPID 61", DeadlockTextRole.Title);

            Assert.IsTrue(size.Width > 0);
            Assert.IsTrue(size.Height > 0);
        }

        [TestMethod]
        public void Measure_LongerTextIsWider()
        {
            using var fonts = new DeadlockFonts();
            var measurer = new SkiaTextMeasurer(fonts);

            var shortText = measurer.Measure("db.dbo.T", DeadlockTextRole.Detail);
            var longText = measurer.Measure("db.dbo.AVeryMuchLongerTableName", DeadlockTextRole.Detail);

            Assert.IsTrue(longText.Width > shortText.Width);
        }

        [TestMethod]
        public void Measure_LineHeightIsConstantForARole()
        {
            // Advance width, not the glyph bounding box: a string with no ascenders or descenders
            // must not come back shorter, or lines would sit unevenly.
            using var fonts = new DeadlockFonts();
            var measurer = new SkiaTextMeasurer(fonts);

            var withDescenders = measurer.Measure("gjpqy", DeadlockTextRole.Detail);
            var without = measurer.Measure("xxxxx", DeadlockTextRole.Detail);

            Assert.AreEqual(withDescenders.Height, without.Height, 1e-6);
        }

        [TestMethod]
        public void Measure_TitleIsLargerThanDetail()
        {
            using var fonts = new DeadlockFonts();
            var measurer = new SkiaTextMeasurer(fonts);

            var title = measurer.Measure("Sales.dbo.Orders", DeadlockTextRole.Title);
            var detail = measurer.Measure("Sales.dbo.Orders", DeadlockTextRole.Detail);

            Assert.IsTrue(title.Height > detail.Height);
        }

        [TestMethod]
        public void Measure_EmptyText_HasNoWidthButKeepsLineHeight()
        {
            using var fonts = new DeadlockFonts();
            var measurer = new SkiaTextMeasurer(fonts);

            var size = measurer.Measure(string.Empty, DeadlockTextRole.Detail);

            Assert.AreEqual(0, size.Width, 1e-6);
            Assert.IsTrue(size.Height > 0);
        }

        [TestMethod]
        public void Measure_HonoursTheConfiguredFontSize()
        {
            using var small = new DeadlockFonts(new DeadlockRenderStyle { TitleFontSize = 8 });
            using var large = new DeadlockFonts(new DeadlockRenderStyle { TitleFontSize = 24 });

            var smallSize = new SkiaTextMeasurer(small).Measure("SPID 61", DeadlockTextRole.Title);
            var largeSize = new SkiaTextMeasurer(large).Measure("SPID 61", DeadlockTextRole.Title);

            Assert.IsTrue(largeSize.Width > smallSize.Width);
            Assert.IsTrue(largeSize.Height > smallSize.Height);
        }

        [TestMethod]
        public void Constructor_NullFonts_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new SkiaTextMeasurer(null!));
        }

        [TestMethod]
        public void Fonts_CanBeDisposedWithoutError()
        {
            // The regular and bold lookups can hand back the same instance, and a missing family
            // falls back to the shared default face - neither must be disposed twice or at all.
            var fonts = new DeadlockFonts(
                new DeadlockRenderStyle { FontFamily = "A Font That Does Not Exist Anywhere" });

            try
            {
                fonts.Dispose();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Disposing fallback/shared faces must not throw, but got: {ex}");
            }
        }

        // ---------------------------------------------------------------- elision

        [TestMethod]
        public void Elide_LeavesTextThatFits()
        {
            using var fonts = new DeadlockFonts();

            Assert.AreEqual("SPID 61", DeadlockRenderer.Elide("SPID 61", fonts.Detail, 500f));
        }

        [TestMethod]
        public void Elide_ShortensTextThatDoesNotFitAndMarksIt()
        {
            using var fonts = new DeadlockFonts();
            const string original = "Warehouse.dbo.AnExtremelyLongTableNameThatWillNotFit";

            var result = DeadlockRenderer.Elide(original, fonts.Detail, 80f);

            Assert.AreNotEqual(original, result);
            Assert.IsTrue(result.EndsWith("…", StringComparison.Ordinal));
            Assert.IsTrue(
                fonts.Detail.MeasureText(result, (SKPaint?)null) <= 80f,
                "The elided text must actually fit the width it was given.");
        }

        [TestMethod]
        public void Elide_ZeroWidth_ReturnsNothing()
        {
            using var fonts = new DeadlockFonts();

            Assert.AreEqual(string.Empty, DeadlockRenderer.Elide("anything", fonts.Detail, 0f));
        }

        [TestMethod]
        public void Elide_WidthTooNarrowForEvenTheEllipsis_ReturnsNothing()
        {
            using var fonts = new DeadlockFonts();

            Assert.AreEqual(string.Empty, DeadlockRenderer.Elide("anything", fonts.Detail, 0.5f));
        }

        [TestMethod]
        public void Elide_EmptyInput_ReturnsEmpty()
        {
            using var fonts = new DeadlockFonts();

            Assert.AreEqual(string.Empty, DeadlockRenderer.Elide(string.Empty, fonts.Detail, 100f));
        }
    }
}
