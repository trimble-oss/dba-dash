using System;
using System.Globalization;
using DBADash.QueryPlan.Interaction;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Live;
using SkiaSharp;

namespace DBADash.QueryPlan.Skia
{
    /// <summary>
    /// What a live plan adds to the picture: where each operator of the running query has got to.  An operator moving
    /// rows has a highlighted border and its rows marching along the arrow out of it, one that hasn't started is
    /// dimmed, and each one shows how far through its estimated rows it is.
    /// </summary>
    public sealed partial class PlanRenderer
    {
        /// <summary>
        /// Seconds on a running clock, which moves the dashes along the arrows out of active operators.  Set by the
        /// host on a timer while a live plan has anything active; it has no effect on any other plan.
        /// </summary>
        public double FlowPhase { get; set; }

        // How far a not started operator is faded, and how fast the dashes march, in screen pixels a second.
        private const float NotStartedFade = 0.55f;
        private const float FlowSpeed = 28f;
        private const float FlowDash = 7f;
        private const float FlowGap = 9f;
        private const float ProgressHeight = 3f;

        private static PlanLiveState? LiveState(PlanNode node, PlanViewController controller) =>
            controller.Live is { } live && node.Operator is { } op ? StateOf(op, live) : null;

        /// <summary>
        /// The operator's state, or for a Compute Scalar SQL Server doesn't measure, its input's - it passes on exactly
        /// the rows it reads, as <see cref="DBADash.QueryPlan.Model.PlanOperator.ActualRows"/> infers, so the flow through
        /// the plan doesn't stop at it.
        /// </summary>
        private static PlanLiveState? StateOf(DBADash.QueryPlan.Model.PlanOperator op, PlanLiveSnapshot live)
        {
            for (var current = op; current is not null; current = current.Children.Count == 1 ? current.Children[0] : null)
            {
                if (live.StateOf(current.NodeId) is { } state) return state;
                if (current.Kind != DBADash.QueryPlan.Model.PlanOperatorKind.ComputeScalar) return null;
            }

            return null;
        }

        private static float LiveFade(PlanNode node, PlanViewController controller) =>
            LiveState(node, controller) == PlanLiveState.NotStarted ? NotStartedFade : 0;

        /// <summary>
        /// A strip along the top of the card for how far through its estimated rows the operator is, and the figure
        /// above the card.  The strip stops at full - an operator past its estimate is full and the figure says by how
        /// much - and is coloured by state: active, finished, or neither.
        /// </summary>
        private void DrawLiveProgress(SKCanvas canvas, PlanNode node, PlanViewController controller, float fade, float radius, bool detail)
        {
            if (LiveState(node, controller) is not { } state || state == PlanLiveState.NotStarted) return;
            if (node.Operator is not { } op) return;

            var colour = state switch
            {
                PlanLiveState.Active => Palette.Info,
                PlanLiveState.Finished => Palette.Success,
                _ => Palette.DetailText
            };

            // Against the estimate for the executions so far, as the plan viewer compares actual rows everywhere else -
            // the inner side of a loop is measured against its estimate per execution times the times it has run.
            var ratio = op.RowEstimateRatio;
            var fraction = state == PlanLiveState.Finished ? 1 : Math.Clamp(ratio ?? 0, 0, 1);

            var bounds = ToSk(node.Bounds);
            var strip = new SKRect(bounds.Left + radius, bounds.Top + 1, bounds.Right - radius, bounds.Top + 1 + ProgressHeight);

            _fill.Color = Faded(Palette.MetricBarTrack, fade);
            canvas.DrawRect(strip, _fill);
            _fill.Color = Faded(colour, fade);
            canvas.DrawRect(new SKRect(strip.Left, strip.Top, strip.Left + (strip.Width * (float)fraction), strip.Bottom), _fill);

            if (!detail || ratio is not { } shown) return;

            var text = shown >= 100 ? "9999%+" : (shown * 100).ToString("N0", CultureInfo.CurrentCulture) + "%";
            _text.Color = Faded(colour, fade);
            canvas.DrawText(text, bounds.Left + 2, bounds.Top - 4, _fonts.Metric, _text);
        }

        /// <summary>
        /// Dashes marching along the arrow out of an operator that is moving rows, so the flow through the plan can be
        /// watched.  Gaps in the background colour over the arrow's body rather than a second colour, so the arrow keeps
        /// whatever colour it has.  Sized in screen pixels, like the estimate outline, to stay visible zoomed out.
        /// </summary>
        private void DrawFlow(SKCanvas canvas, PlanEdge edge, PlanViewController controller, float thickness)
        {
            if (LiveState(edge.From, controller) != PlanLiveState.Active) return;

            var perPixel = 1 / (float)Math.Max(controller.Zoom, 0.01);
            var period = (FlowDash + FlowGap) * perPixel;
            var phase = -(float)(FlowPhase * FlowSpeed * perPixel % period);

            using var body = BuildEdgePath(edge.Points, _style.ArrowLength);
            using var dash = SKPathEffect.CreateDash([FlowDash * perPixel, FlowGap * perPixel], phase);

            _marker.Color = Palette.Background.WithAlpha(170);
            _marker.StrokeWidth = Math.Max(thickness * 0.45f, 1.5f * perPixel);
            _marker.StrokeCap = SKStrokeCap.Butt;
            _marker.PathEffect = dash;
            canvas.DrawPath(body, _marker);
            _marker.PathEffect = null;
            _marker.StrokeCap = SKStrokeCap.Round;
        }
    }
}
