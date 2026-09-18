using System;
using DBADash.QueryPlan.Layout;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// The layout engine's text measurement, backed by real font metrics.
    ///
    /// This is the seam that lets the layout engine size nodes to their content without knowing
    /// anything about fonts or drawing.  It must be constructed from the same <see cref="PlanFonts"/>
    /// the renderer draws with, or text will not sit where the node was measured for it.
    /// </summary>
    public sealed class SkiaPlanTextMeasurer : IPlanTextMeasurer
    {
        private readonly PlanFonts _fonts;

        public SkiaPlanTextMeasurer(PlanFonts fonts)
        {
            _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        }

        public LayoutSize Measure(string text, PlanTextRole role)
        {
            var font = FontFor(role);

            // Advance width rather than the glyph bounding box: the bounding box of a string with no
            // ascenders or descenders is short, which would give inconsistent line heights.
            var width = font.MeasureText(text ?? string.Empty, (SKPaint?)null);
            return new LayoutSize(width, PlanFonts.LineHeight(font));
        }

        internal SKFont FontFor(PlanTextRole role) => role switch
        {
            PlanTextRole.Title => _fonts.Title,
            PlanTextRole.Detail => _fonts.Detail,
            PlanTextRole.Metric => _fonts.Metric,
            _ => _fonts.EdgeLabel
        };
    }
}
