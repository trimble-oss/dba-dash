using System;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// The pictures for the legend - see <see cref="PlanLegend"/>.
    ///
    /// Every sample is drawn by the same private methods that draw the plan, with the plan's own
    /// palette and geometry, which is the whole point: a legend drawn separately would be right the day
    /// it was written and wrong the first time the renderer changed.
    /// </summary>
    public sealed partial class PlanRenderer
    {
        /// <summary>The width of a legend sample, in layout units, other than an operator icon's.</summary>
        public const float LegendSampleWidth = 64f;

        /// <summary>The height of a legend sample, in layout units, other than an operator icon's.</summary>
        public const float LegendSampleHeight = 36f;

        private static readonly PlanLayoutOptions LegendGeometry = new();

        /// <summary>How big <see cref="DrawLegendSample"/> draws <paramref name="entry"/>, in layout units.</summary>
        public static SKSize LegendSampleSize(PlanLegendEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return entry.Sample == PlanLegendSample.OperatorIcon
                ? new SKSize((float)LegendGeometry.IconSize, (float)LegendGeometry.IconSize)
                : new SKSize(LegendSampleWidth, LegendSampleHeight);
        }

        /// <summary>
        /// Draws the picture for a legend entry with its top left corner at the origin, in the
        /// <see cref="LegendSampleSize"/> it asks for.  The caller clears to the background first and
        /// translates to where it wants the sample.
        ///
        /// <paramref name="scale"/> is the scale the canvas is drawn at, which the dashed estimate
        /// outline needs: it is sized in screen pixels, as it is on a plan at that zoom.
        /// </summary>
        public void DrawLegendSample(SKCanvas canvas, PlanLegendEntry entry, double scale = 1)
        {
            ArgumentNullException.ThrowIfNull(canvas);
            ArgumentNullException.ThrowIfNull(entry);

            var size = LegendSampleSize(entry);
            var area = SKRect.Create(size.Width, size.Height);

            switch (entry.Sample)
            {
                case PlanLegendSample.OperatorIcon:
                    DrawChip(canvas, entry.Operator, PlanOperatorClassifier.CategoryOf(entry.Operator), area, 0);
                    break;

                case PlanLegendSample.Badge:
                    DrawBadge(canvas, entry.Badge, Centred(area, (float)LegendGeometry.BadgeSize), 0);
                    break;

                case PlanLegendSample.WarningMarker:
                    DrawWarningTriangle(canvas, Centred(area, (float)LegendGeometry.WarningMarkerSize), false, 0);
                    break;

                case PlanLegendSample.CriticalWarningMarker:
                    DrawWarningTriangle(canvas, Centred(area, (float)LegendGeometry.WarningMarkerSize), true, 0);
                    break;

                case PlanLegendSample.ArrowEstimated:
                    DrawSampleArrow(canvas, area, 6f, Palette.Edge, null, scale);
                    break;

                case PlanLegendSample.ArrowActual:
                    DrawSampleArrow(canvas, area, 6f, Palette.EdgeActual, null, scale);
                    break;

                case PlanLegendSample.ArrowEstimateOutBy10:
                    DrawSampleArrow(canvas, area, 6f, Palette.ColourFor(PlanEstimateAccuracy.Warning), null, scale);
                    break;

                case PlanLegendSample.ArrowEstimateOutBy100:
                    DrawSampleArrow(canvas, area, 6f, Palette.ColourFor(PlanEstimateAccuracy.Critical), null, scale);
                    break;

                // The body is the actual rows and the outline is the estimate, as on a plan drawn with
                // Line Width set to Actual vs Estimated.
                case PlanLegendSample.ArrowUnderestimated:
                    DrawSampleArrow(canvas, area, 14f, Palette.EdgeActual, (5f, Palette.ColourFor(PlanEstimateAccuracy.Critical)), scale);
                    break;

                case PlanLegendSample.ArrowOverestimated:
                    DrawSampleArrow(canvas, area, 4f, Palette.EdgeActual, (14f, Palette.ColourFor(PlanEstimateAccuracy.Critical)), scale);
                    break;

                case PlanLegendSample.ArrowOnPath:
                    DrawSampleArrow(canvas, area, 6f, Palette.EdgeHighlight, null, scale);
                    break;

                case PlanLegendSample.MetricBar:
                    DrawSampleBars(canvas, area);
                    break;

                case PlanLegendSample.CollapsedStack:
                    DrawSampleCollapsed(canvas, area);
                    break;

                case PlanLegendSample.CollapseToggle:
                    DrawToggleBox(canvas, Centred(area, 14f), collapsed: false, 0);
                    break;

                case PlanLegendSample.BorderSelected:
                    DrawSampleCard(canvas, area, Palette.Selection, _style.EmphasisBorderWidth);
                    break;

                case PlanLegendSample.BorderHovered:
                    DrawSampleCard(canvas, area, Palette.Hover, _style.EmphasisBorderWidth);
                    break;

                case PlanLegendSample.BorderSearchMatch:
                    DrawSampleCard(canvas, area, Palette.SearchMatch, _style.EmphasisBorderWidth);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(entry), entry.Sample, "Not a legend sample the renderer knows.");
            }
        }

        private static SKRect Centred(SKRect area, float size) =>
            SKRect.Create(area.MidX - (size / 2), area.MidY - (size / 2), size, size);

        /// <summary>A card as small as a legend sample can hold, in the border it is being shown with.</summary>
        private static SKRect SampleCard(SKRect area) => SKRect.Inflate(area, -6f, -6f);

        private void DrawSampleCard(SKCanvas canvas, SKRect area, SKColor border, float borderWidth)
        {
            var card = SampleCard(area);
            var radius = _style.NodeCornerRadius;

            _fill.Color = Palette.NodeFill;
            canvas.DrawRoundRect(card, radius, radius, _fill);

            _stroke.Color = border;
            _stroke.StrokeWidth = borderWidth;
            canvas.DrawRoundRect(card, radius, radius, _stroke);
        }

        /// <summary>
        /// An arrow running right to left across the sample, as they do on a plan.  Straight, since it
        /// is the colour and the width being shown and not the routing.
        /// </summary>
        private void DrawSampleArrow(
            SKCanvas canvas,
            SKRect area,
            float thickness,
            SKColor colour,
            (float Width, SKColor Colour)? estimate,
            double scale)
        {
            var y = area.MidY;

            // The route is in layout points, as the layout engine produces it: from the producer's edge
            // to the consumer's, so the head is at the left.
            LayoutPoint[] route = [new(area.Right - 2, y), new(area.Left + 2, y)];

            DrawArrow(canvas, route, thickness, colour, 0, estimate, scale);
        }

        /// <summary>
        /// The bar at the foot of a card at a low, a middling and a high share, which is the run of
        /// colours the reader has to learn.
        /// </summary>
        private void DrawSampleBars(SKCanvas canvas, SKRect area)
        {
            var height = (float)LegendGeometry.MetricBarHeight;
            var width = area.Width - 12f;
            var gap = 5f;
            var top = area.MidY - (((height * 3) + (gap * 2)) / 2);

            double[] shares = [0.15, 0.55, 0.95];

            for (var i = 0; i < shares.Length; i++)
            {
                var bar = SKRect.Create(area.Left + 6f, top + (i * (height + gap)), width, height);
                DrawBarFill(canvas, bar, shares[i], 0);
            }
        }

        /// <summary>A card with two more behind it and the count of what they stand for.</summary>
        private void DrawSampleCollapsed(SKCanvas canvas, SKRect area)
        {
            // Narrower than the other cards, to leave room on the right for the stack and the count.
            var card = SKRect.Create(area.Left + 4f, area.Top + 6f, 28f, area.Height - 12f);
            var radius = _style.NodeCornerRadius;

            DrawCollapsedStack(canvas, card, radius, 0);

            _fill.Color = Palette.NodeFill;
            canvas.DrawRoundRect(card, radius, radius, _fill);

            _stroke.Color = Palette.NodeBorder;
            _stroke.StrokeWidth = _style.NodeBorderWidth;
            canvas.DrawRoundRect(card, radius, radius, _stroke);

            DrawHiddenCount(canvas, card, 3, 0);
        }
    }
}
