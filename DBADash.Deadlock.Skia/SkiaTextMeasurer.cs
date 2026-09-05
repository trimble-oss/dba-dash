using System;
using DBADash.Deadlock.Layout;
using SkiaSharp;

namespace DBADash.Deadlock.Skia
{
    /// <summary>
    /// The layout engine's text measurement, backed by real font metrics.
    ///
    /// This is the seam that lets the layout engine size nodes to their content without knowing
    /// anything about fonts or drawing.  It must be constructed from the same
    /// <see cref="DeadlockFonts"/> the renderer draws with, or text will not sit where the node was
    /// measured for it.
    /// </summary>
    public sealed class SkiaTextMeasurer : IDeadlockTextMeasurer
    {
        private readonly DeadlockFonts _fonts;

        public SkiaTextMeasurer(DeadlockFonts fonts)
        {
            _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
        }

        public LayoutSize Measure(string text, DeadlockTextRole role)
        {
            var font = FontFor(role);

            // Advance width rather than the glyph bounding box: the bounding box of a string with no
            // ascenders or descenders is short, which would give inconsistent line heights.
            var width = font.MeasureText(text ?? string.Empty, (SKPaint?)null);
            return new LayoutSize(width, DeadlockFonts.LineHeight(font));
        }

        internal SKFont FontFor(DeadlockTextRole role) => role switch
        {
            DeadlockTextRole.Title => _fonts.Title,
            DeadlockTextRole.EdgeLabel => _fonts.EdgeLabel,
            _ => _fonts.Detail
        };
    }
}
