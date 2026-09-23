using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DBADash.QueryPlan.Layout;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// What a legend entry shows beside its text.  The picture itself is drawn by the renderer, from
    /// the same code that draws it on a plan - see <c>PlanRenderer.DrawLegendSample</c> - so a legend
    /// entry cannot show something the plan does not.
    /// </summary>
    public enum PlanLegendSample
    {
        /// <summary>An operator's icon: <see cref="PlanLegendEntry.Operator"/> says which.</summary>
        OperatorIcon,

        /// <summary>A badge: <see cref="PlanLegendEntry.Badge"/> says which.</summary>
        Badge,

        WarningMarker,
        CriticalWarningMarker,

        /// <summary>An arrow of a measured or estimated row count, in the plain colour.</summary>
        ArrowEstimated,
        ArrowActual,

        /// <summary>An arrow whose estimate was ten times out or more.</summary>
        ArrowEstimateOutBy10,

        /// <summary>An arrow whose estimate was a hundred times out or more.</summary>
        ArrowEstimateOutBy100,

        /// <summary>A fat arrow with the estimate outlined inside it, dashed.</summary>
        ArrowUnderestimated,

        /// <summary>A thin arrow with the estimate outlined round it, dashed.</summary>
        ArrowOverestimated,

        /// <summary>An arrow on the path from the selected operator to the root.</summary>
        ArrowOnPath,

        /// <summary>The bar at the foot of an operator, at three shares of the worst.</summary>
        MetricBar,

        CollapsedStack,
        CollapseToggle,

        BorderSelected,
        BorderHovered,
        BorderSearchMatch
    }

    /// <summary>One thing the legend explains: what it looks like, what it is called, and what it means.</summary>
    public sealed class PlanLegendEntry
    {
        internal PlanLegendEntry(
            string title,
            string description,
            PlanLegendSample sample,
            PlanOperatorKind @operator = PlanOperatorKind.Unknown,
            PlanNodeBadges badge = PlanNodeBadges.None)
        {
            Title = title;
            Description = description;
            Sample = sample;
            Operator = @operator;
            Badge = badge;
        }

        public string Title { get; }

        public string Description { get; }

        public PlanLegendSample Sample { get; }

        /// <summary>The operator drawn, for <see cref="PlanLegendSample.OperatorIcon"/>.</summary>
        public PlanOperatorKind Operator { get; }

        /// <summary>The badge drawn, for <see cref="PlanLegendSample.Badge"/>.</summary>
        public PlanNodeBadges Badge { get; }
    }

    /// <summary>A group of entries under one heading, with an optional paragraph before them.</summary>
    public sealed class PlanLegendSection
    {
        internal PlanLegendSection(string title, string? introduction, IReadOnlyList<PlanLegendEntry> entries, bool compact = false)
        {
            Title = title;
            Introduction = introduction;
            Entries = entries;
            Compact = compact;
        }

        public string Title { get; }

        public string? Introduction { get; }

        public IReadOnlyList<PlanLegendEntry> Entries { get; }

        /// <summary>
        /// The entries are shown as a grid of samples with their names, the description held for a
        /// tooltip - for the operators, of which there are many and whose descriptions are long.
        /// </summary>
        public bool Compact { get; }
    }

    /// <summary>A key or mouse action, and what it does.</summary>
    public sealed record PlanLegendControl(string Input, string Action);

    /// <summary>
    /// The legend and help for the plan viewer: what the icons, markers, arrows and bars on a plan
    /// mean, and how to move around it.
    ///
    /// The words are here, in the model, so they can be tested and sit beside the code that decides
    /// what they describe.  The pictures are not: they are drawn by the renderer itself, so the legend
    /// cannot drift from the plan.  The operator list is taken from <see cref="PlanOperatorKind"/>
    /// and <see cref="PlanOperatorDescriptions"/> rather than written out again, so a new operator
    /// appears here as soon as it exists.
    /// </summary>
    public static class PlanLegend
    {
        // The figures the words quote are the ones the plan is drawn with, read from the layout engine
        // rather than repeated, so the legend follows if a threshold is ever changed.
        private static readonly string EstimateOutByAmber =
            PlanLayoutEngine.EstimateMismatchThreshold.ToString("0", CultureInfo.InvariantCulture);

        private static readonly string EstimateOutByRed =
            PlanLayoutEngine.EstimateCriticalThreshold.ToString("0", CultureInfo.InvariantCulture);

        private static readonly string DiscardedRows =
            PlanLayoutEngine.DiscardedRowsThreshold.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>Every section, in the order the legend shows them.</summary>
        public static IReadOnlyList<PlanLegendSection> Sections { get; } =
        [
            Reading(),
            Markers(),
            Arrows(),
            Cards(),
            Operators()
        ];

        /// <summary>The keys and mouse actions of the plan.</summary>
        public static IReadOnlyList<PlanLegendControl> Controls { get; } =
        [
            new("Mouse wheel", "Zoom in and out, about the pointer."),
            new("Drag, or middle button drag", "Pan the plan. Operators are not draggable."),
            new("Click an operator", "Select it, and show its properties beside the plan."),
            new("Double click an operator", "Show the query text."),
            new("Right click", "The menu for what is under the pointer: copy the operator's details, collapse its inputs, Follow Data Path, and Fit to Window."),
            new("Box on an operator's left edge", "Collapse the operators feeding it, or show them again."),
            new("Arrow keys", "Walk the plan: Left goes to the operator that consumes this one's rows, Right to its first input, Up and Down to the sibling inputs."),
            new("+ and -", "Zoom in and out."),
            new("0", "Fit the whole plan in the window."),
            new("Space", "Collapse or expand the selected operator."),
            new("Ctrl+F", "Go to the Find box, to search for an operator, table, index or predicate."),
            new("F3 and Shift+F3", "Go to the next or previous match for what is typed in Find."),
            new("Escape", "Clear the selection, and leave Follow Data Path."),
            new("F1", "Show this legend."),
            new("Ctrl+W", "Close the plan in front.")
        ];

        private static PlanLegendSection Reading() => new(
            "Reading a plan",
            "A plan is read from right to left. Rows flow from the operators on the right, which read the data, along the arrows to the operators on the left, and out of the statement at the far left. " +
            "Each card is an operator: its icon, its name, the object it reads or writes, its share of the cost and its row count. On an actual plan it also shows its time.",
            []);

        private static PlanLegendSection Markers() => new(
            "Markers on an operator",
            "Badges sit on the top edge of a card, the most important closest to the corner. Hover an operator for the detail behind any of them.",
            [
                new("Warning", "The operator has a warning: something the plan reports, worth a look. The Warnings tab lists them all. On a collapsed operator it means something hidden under it has one.", PlanLegendSample.WarningMarker),
                new("Critical warning", "A more serious warning, worth looking at first: a spill to tempdb, or a join with no join predicate.", PlanLegendSample.CriticalWarningMarker),
                new("Missing index", "The optimizer asked for an index on the table this operator reads. The Missing Indexes tab has the CREATE INDEX statement.", PlanLegendSample.Badge, badge: PlanNodeBadges.MissingIndex),
                new("Estimate out by " + EstimateOutByAmber + "x or more", "The operator returned at least " + EstimateOutByAmber + " times more or fewer rows than the optimizer expected. A wrong estimate here is often what led it to a poor choice further up. Actual plans only.", PlanLegendSample.Badge, badge: PlanNodeBadges.EstimateMismatch),
                new("Rows read and discarded", "The operator read more rows than it returned, at least " + DiscardedRows + " of them thrown away by a predicate applied after reading. The row count on its arrow shows only what came out. Actual plans only.", PlanLegendSample.Badge, badge: PlanNodeBadges.RowsDiscarded),
                new("Parallel", "The operator ran on more than one thread.", PlanLegendSample.Badge, badge: PlanNodeBadges.Parallel),
                new("Batch mode", "The operator processed rows in batches rather than one at a time. Batch mode is what columnstore indexes and many analytic queries use.", PlanLegendSample.Badge, badge: PlanNodeBadges.BatchMode)
            ]);

        private static PlanLegendSection Arrows() => new(
            "Arrows",
            "An arrow carries rows from an operator into the one that consumes them, and its thickness is the number of rows - or their size, if you choose Data Size under Line Width. The number on it is the same figure.",
            [
                new("Estimated rows", "The plan has no actual figures, so the arrows are drawn by the rows the optimizer expected.", PlanLegendSample.ArrowEstimated),
                new("Actual rows", "The rows the query actually returned along the arrow.", PlanLegendSample.ArrowActual),
                new("Estimate out by " + EstimateOutByAmber + "x or more", "Amber: the optimizer expected " + EstimateOutByAmber + " times more or fewer rows than there were.", PlanLegendSample.ArrowEstimateOutBy10),
                new("Estimate out by " + EstimateOutByRed + "x or more", "Red: " + EstimateOutByRed + " times more or fewer. Where an estimate was closer than " + EstimateOutByAmber + " times the arrow keeps its plain colour, so the ones that are wrong stand out.", PlanLegendSample.ArrowEstimateOutBy100),
                new("Underestimate", "With Line Width set to Actual vs Estimated, the estimate is outlined over the actual arrow, dashed and coloured by how far out it was. Inside a thick arrow, the optimizer expected far fewer rows than came.", PlanLegendSample.ArrowUnderestimated),
                new("Overestimate", "A hollow dashed sleeve around a thin arrow: the optimizer expected far more rows than came.", PlanLegendSample.ArrowOverestimated),
                new("Path to the root", "Follow Data Path highlights the arrows from the selected operator to the statement and fades the rest of the plan.", PlanLegendSample.ArrowOnPath)
            ]);

        private static PlanLegendSection Cards() => new(
            "Operator cards",
            null,
            [
                new("Bar at the foot", "The operator's share of the metric chosen under Show - cost, rows, CPU, time, reads or estimate error - measured against the worst operator in the statement. It runs from blue through amber to red, so the operator to look at is a different colour and not only a longer bar.", PlanLegendSample.MetricBar),
                new("Collapsed operator", "The cards stacked behind it, and the +N, say the operators feeding it are hidden, and how many there are. Expand All puts them all back.", PlanLegendSample.CollapsedStack),
                new("Collapse box", "Click the box on the left edge of an operator to hide the operators feeding it, or show them again. It appears when the pointer is over the operator or it is selected.", PlanLegendSample.CollapseToggle),
                new("Selected", "The operator whose properties are shown. Click one to select it.", PlanLegendSample.BorderSelected),
                new("Hovered", "The operator under the pointer, whose tooltip is showing.", PlanLegendSample.BorderHovered),
                new("Find match", "An operator matching what is typed in Find. F3 goes to the next.", PlanLegendSample.BorderSearchMatch)
            ]);

        private static PlanLegendSection Operators()
        {
            // Every kind, in colour family order so the chip colours come in blocks - the same order as
            // the operator reference the build writes.
            var kinds = Enum.GetValues<PlanOperatorKind>()
                .OrderBy(PlanOperatorClassifier.CategoryOf)
                .ThenBy(kind => kind);

            return new PlanLegendSection(
                "Operators",
                "The icon's colour says what kind of work the operator does; its shape says which. Hover an operator for what it does, or one of these.",
                kinds.Select(kind => new PlanLegendEntry(
                        PlanOperatorNames.For(kind, null),
                        PlanOperatorDescriptions.For(kind),
                        PlanLegendSample.OperatorIcon,
                        kind))
                    .ToList(),
                compact: true);
        }

        /// <summary>The colour family an operator entry belongs to, for the heading it sits under.</summary>
        public static PlanOperatorCategory CategoryOf(PlanLegendEntry entry) =>
            PlanOperatorClassifier.CategoryOf(entry.Operator);
    }
}
