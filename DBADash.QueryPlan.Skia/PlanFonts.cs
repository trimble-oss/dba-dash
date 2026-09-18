using System;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// The fonts used to measure and draw a plan, built once and shared by
    /// <see cref="SkiaPlanTextMeasurer"/> and <see cref="PlanRenderer"/>.
    ///
    /// Sharing matters: the layout was sized with these metrics, so drawing with anything else would
    /// put text outside the boxes that were measured for it.
    /// </summary>
    public sealed class PlanFonts : IDisposable
    {
        private readonly SKTypeface _typeface;
        private readonly SKTypeface _boldTypeface;

        public PlanFonts(PlanRenderStyle? style = null)
        {
            var s = style ?? new PlanRenderStyle();

            // FromFamilyName falls back to a default face when the family is missing, so this works
            // on a machine without the requested font rather than failing.
            _typeface = SKTypeface.FromFamilyName(s.FontFamily) ?? SKTypeface.Default;
            _boldTypeface = SKTypeface.FromFamilyName(
                                s.FontFamily,
                                SKFontStyleWeight.SemiBold,
                                SKFontStyleWidth.Normal,
                                SKFontStyleSlant.Upright)
                            ?? _typeface;

            Title = new SKFont(_boldTypeface, s.TitleFontSize, 1f, 0f);
            Detail = new SKFont(_typeface, s.DetailFontSize, 1f, 0f);
            Metric = new SKFont(_typeface, s.MetricFontSize, 1f, 0f);
            EdgeLabel = new SKFont(_typeface, s.EdgeLabelFontSize, 1f, 0f);
            TooltipTitle = new SKFont(_boldTypeface, s.TooltipTitleFontSize, 1f, 0f);
            TooltipText = new SKFont(_typeface, s.TooltipFontSize, 1f, 0f);
            TooltipLabel = new SKFont(_typeface, s.TooltipFontSize, 1f, 0f);
        }

        public SKFont Title { get; }

        public SKFont Detail { get; }

        public SKFont Metric { get; }

        public SKFont EdgeLabel { get; }

        public SKFont TooltipTitle { get; }

        public SKFont TooltipText { get; }

        public SKFont TooltipLabel { get; }

        /// <summary>Distance from one baseline to the next for <paramref name="font"/>.</summary>
        public static float LineHeight(SKFont font)
        {
            var metrics = font.Metrics;
            return metrics.Descent - metrics.Ascent;
        }

        /// <summary>
        /// Baseline offset from the top of a line box.  Ascent is negative in Skia, so negating it
        /// gives the distance down to the baseline.
        /// </summary>
        public static float Baseline(SKFont font) => -font.Metrics.Ascent;

        public void Dispose()
        {
            Title.Dispose();
            Detail.Dispose();
            Metric.Dispose();
            EdgeLabel.Dispose();
            TooltipTitle.Dispose();
            TooltipText.Dispose();
            TooltipLabel.Dispose();

            // SKTypeface.Default is a shared singleton and must not be disposed, and the bold lookup
            // can hand back the same instance as the regular one - so dispose each face at most
            // once, and only when we own it.
            DisposeTypeface(_typeface);
            if (!ReferenceEquals(_boldTypeface, _typeface)) DisposeTypeface(_boldTypeface);
        }

        private static void DisposeTypeface(SKTypeface typeface)
        {
            if (!ReferenceEquals(typeface, SKTypeface.Default)) typeface.Dispose();
        }
    }
}
