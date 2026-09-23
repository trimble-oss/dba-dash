using System;
using System.Collections.Generic;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Layout
{
    /// <summary>
    /// The markers drawn on a node, for the things worth spotting without reading anything.
    ///
    /// A flags enum because a node can easily earn several - a parallel hash join that spilled and
    /// under-estimated its rows is exactly the node the reader is looking for, and it would be
    /// perverse to show only one of the three reasons.
    /// </summary>
    [Flags]
    public enum PlanNodeBadges
    {
        None = 0,

        /// <summary>
        /// The operator carries at least one warning - or, on the root, the statement does, and on
        /// a collapsed node, something hidden under it does.  Drawn as a triangle on the glyph
        /// rather than in the badge strip: see <see cref="PlanNode.WarningMarkerBounds"/>.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// A warning severe enough to be the likely answer - a spill, a missing predicate.  Always
        /// set together with <see cref="Warning"/>, and turns the triangle red.
        /// </summary>
        CriticalWarning = 2,

        /// <summary>
        /// Actual rows came out far from the estimate.  The threshold is in
        /// <see cref="PlanLayoutEngine"/>; the point of the badge is that a bad estimate here is
        /// what caused the bad choice further up.
        /// </summary>
        EstimateMismatch = 4,

        /// <summary>The operator ran on more than one thread.</summary>
        Parallel = 8,

        /// <summary>Batch mode, which is worth noticing on a rowstore plan.</summary>
        BatchMode = 16,

        /// <summary>
        /// The optimiser asked for an index on the object this operator reads.  Put on the node
        /// rather than left in a list, because a recommendation that does not say where to look is
        /// much less useful than one that does.
        /// </summary>
        MissingIndex = 32,

        /// <summary>
        /// Rows were read and then thrown away by a residual predicate.  Invisible in the row count,
        /// which reports what came out, not what was touched.
        /// </summary>
        RowsDiscarded = 64
    }

    /// <summary>
    /// Which badges a node shows, and in what order.
    ///
    /// Here rather than in the renderer because the layout sizes the strip for exactly the badges
    /// that will be drawn - if the two disagreed by one badge, every node with that combination
    /// would have a badge squeezed in or a gap where one should be.
    /// </summary>
    public static class PlanBadges
    {
        /// <summary>
        /// The badges drawn in a node's strip, worst first, so the one that matters is the one
        /// closest to the corner rather than wherever the enum happened to put it.
        ///
        /// Warnings are not among them.  They get the triangle on the glyph instead, which is where
        /// SSMS puts it, and it stays visible at zooms where the strip is not drawn at all.
        /// </summary>
        public static IEnumerable<PlanNodeBadges> Ordered(PlanNodeBadges badges)
        {
            if (badges.HasFlag(PlanNodeBadges.MissingIndex)) yield return PlanNodeBadges.MissingIndex;
            if (badges.HasFlag(PlanNodeBadges.EstimateMismatch)) yield return PlanNodeBadges.EstimateMismatch;
            if (badges.HasFlag(PlanNodeBadges.RowsDiscarded)) yield return PlanNodeBadges.RowsDiscarded;
            if (badges.HasFlag(PlanNodeBadges.Parallel)) yield return PlanNodeBadges.Parallel;
            if (badges.HasFlag(PlanNodeBadges.BatchMode)) yield return PlanNodeBadges.BatchMode;
        }

        /// <summary>How many badges will be drawn, for sizing the strip they sit in.</summary>
        public static int Count(PlanNodeBadges badges)
        {
            var count = 0;
            foreach (var _ in Ordered(badges)) count++;
            return count;
        }
    }

    /// <summary>
    /// A positioned node in a laid out plan.  The renderer draws it and the interaction layer hit
    /// tests against <see cref="Bounds"/>.
    /// </summary>
    public sealed class PlanNode
    {
        /// <summary>
        /// Stable identifier within the statement: the operator's node id, or "root" for the
        /// synthetic head.  Used to carry a selection across a re-layout.
        /// </summary>
        public string Id { get; internal set; } = string.Empty;

        /// <summary>
        /// The operator this node draws, or null for the synthetic statement root - SQL Server does
        /// not emit one, but a plan reads as a pipeline that ends somewhere, and without it the
        /// outermost operator has an arrow pointing into nothing.
        /// </summary>
        public PlanOperator? Operator { get; internal set; }

        /// <summary>The statement the node belongs to, which the root node describes.</summary>
        public PlanStatement Statement { get; internal set; } = null!;

        public PlanOperatorKind Kind { get; internal set; }

        public PlanOperatorCategory Category { get; internal set; }

        /// <summary>The heading line - the operator name.</summary>
        public string Title { get; internal set; } = string.Empty;

        /// <summary>
        /// The object the operator reads or writes, or null when it touches none.  Sized into the
        /// node, so the renderer draws it where the node was measured for it.
        /// </summary>
        public string? Subtitle { get; internal set; }

        /// <summary>
        /// The figures at the foot of the node - the share of cost, and the rows.  One string rather
        /// than several so the node is measured for exactly what is drawn.
        /// </summary>
        public string? MetricLine { get; internal set; }

        /// <summary>
        /// Elapsed and CPU time, on an actual plan that measured them - null otherwise.  A line of
        /// its own rather than more figures on <see cref="MetricLine"/>: on an actual plan the time
        /// is often the first thing looked for, and squeezed onto the cost line it would be the
        /// first thing elided.
        /// </summary>
        public string? TimingLine { get; internal set; }

        /// <summary>
        /// <see cref="Title"/> as the lines it is drawn on, one after another down <see cref="TitleBounds"/>.
        /// One line, except with the icon above the text - see <see cref="PlanLayoutOptions.IconAboveText"/> -
        /// where a long operator name wraps rather than widening the node.
        /// </summary>
        public IReadOnlyList<string> TitleLines { get; internal set; } = [];

        /// <summary>
        /// <see cref="Subtitle"/> as the lines it is drawn on, like <see cref="TitleLines"/>.  One
        /// line, cut short when drawn if it does not fit, unless object names wrap - see
        /// <see cref="PlanLayoutOptions.WrapObjectNames"/>.
        /// </summary>
        public IReadOnlyList<string> SubtitleLines { get; internal set; } = [];

        /// <summary><see cref="MetricLine"/> as the lines it is drawn on, like <see cref="TitleLines"/>.</summary>
        public IReadOnlyList<string> MetricLines { get; internal set; } = [];

        /// <summary><see cref="TimingLine"/> as the lines it is drawn on, like <see cref="TitleLines"/>.</summary>
        public IReadOnlyList<string> TimingLines { get; internal set; } = [];

        /// <summary>
        /// Centre each line of text across its bounds rather than starting it at the left - true with
        /// the icon above the text, where the text sits under the icon's middle.
        /// </summary>
        public bool CentreText { get; internal set; }

        public LayoutRect Bounds { get; internal set; }

        public PlanNodeBadges Badges { get; internal set; }

        /// <summary>
        /// The operator's share of the statement cost, 0 to 1.  Kept on the node because it sets the
        /// length of the bar the node is sized for.
        /// </summary>
        public double CostFraction { get; internal set; }

        /// <summary>The inputs, left to right on screen meaning right to left in the data flow.</summary>
        public IReadOnlyList<PlanNode> Children { get; internal set; } = [];

        /// <summary>The node this one feeds, or null at the root.</summary>
        public PlanNode? Parent { get; internal set; }

        /// <summary>Distance from the root, which sets the column the node sits in.</summary>
        public int Depth { get; internal set; }

        /// <summary>
        /// True when this node's inputs are hidden.  A collapsed node keeps its place in the picture
        /// and its subtree's cost, so a plan can be read a stage at a time without the shape of it
        /// changing underneath.
        /// </summary>
        public bool IsCollapsed { get; internal set; }

        /// <summary>
        /// How many operators are hidden under a collapsed node, so the node can say so rather than
        /// silently dropping part of the plan.
        /// </summary>
        public int HiddenDescendantCount { get; internal set; }

        /// <summary>
        /// How many of the operators hidden under a collapsed node carry warnings.  Their warnings
        /// are raised on the collapsed node, so collapsing part of a plan never hides a problem in
        /// it.
        /// </summary>
        public int HiddenWarningCount { get; internal set; }

        /// <summary>
        /// True when any of the hidden operators' warnings is critical.  Separate from the node's
        /// badges, which also carry the node's own warnings and so cannot say which side a critical
        /// one came from.
        /// </summary>
        public bool HiddenWarningsAreCritical { get; internal set; }

        /// <summary>True when the node, or anything collapsed under it, carries a warning.</summary>
        public bool HasWarnings => Badges.HasFlag(PlanNodeBadges.Warning);

        /// <summary>
        /// Where the warning triangle goes - over the lower right corner of the glyph, as SSMS
        /// draws it - or null when there is nothing to warn about.
        /// </summary>
        public LayoutRect? WarningMarkerBounds { get; internal set; }

        /// <summary>
        /// Where the glyph goes, in layout space.  Computed during sizing so the renderer does not
        /// have to re-derive the node's internal arrangement and risk disagreeing with it.
        /// </summary>
        public LayoutRect IconBounds { get; internal set; }

        /// <summary>The line the title is drawn on.</summary>
        public LayoutRect TitleBounds { get; internal set; }

        /// <summary>The line the subtitle is drawn on, or null when there is none.</summary>
        public LayoutRect? SubtitleBounds { get; internal set; }

        /// <summary>The line the metrics are drawn on, or null when there are none.</summary>
        public LayoutRect? MetricBounds { get; internal set; }

        /// <summary>The line the timings are drawn on, or null when there are none.</summary>
        public LayoutRect? TimingBounds { get; internal set; }

        /// <summary>The full width bar at the foot of the node, or null when it is turned off.</summary>
        public LayoutRect? MetricBarBounds { get; internal set; }

        /// <summary>
        /// The strip the badges are drawn into, right to left, or null when the node has none.  It
        /// straddles the node's top border towards the right, so it is partly outside
        /// <see cref="Bounds"/>.
        /// </summary>
        public LayoutRect? BadgeStripBounds { get; internal set; }

        /// <summary>
        /// The corner the expand or collapse control occupies, or null when the node has no inputs
        /// to hide.
        /// </summary>
        public LayoutRect? CollapseToggleBounds { get; internal set; }

        /// <summary>True for the synthetic node at the head of the plan.</summary>
        public bool IsRoot => Operator is null;

        /// <summary>
        /// The rows this node hands to its parent: actual where measured, estimated otherwise.  The
        /// root reports what the statement returned.
        /// </summary>
        public double Rows => Operator?.RowsForDisplay ?? 0;

        public override string ToString() => Subtitle is null ? Title : Title + " " + Subtitle;
    }
}
