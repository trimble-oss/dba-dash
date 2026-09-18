using System.Collections.Generic;
using System.Text;

namespace DBADash.QueryPlan.Interaction
{
    /// <summary>
    /// One row of a tooltip: a label, its value, and whether it is worth drawing attention to.
    /// </summary>
    public readonly struct PlanTooltipRow
    {
        public PlanTooltipRow(string label, string value, bool isEmphasised = false, bool wraps = false)
        {
            Label = label;
            Value = value;
            IsEmphasised = isEmphasised;
            Wraps = wraps;
        }

        public string Label { get; }

        public string Value { get; }

        /// <summary>
        /// Drawn in the warning colour.  Used for the figures that are the reason the reader is
        /// hovering - a spill, an estimate that was out by orders of magnitude - so the answer is
        /// visible without reading every row.
        /// </summary>
        public bool IsEmphasised { get; }

        /// <summary>
        /// Wrapped onto as many lines as it needs, up to a limit, rather than cut short at the edge of
        /// the tooltip.  For the long values that are read rather than glanced at - a seek predicate
        /// is the difference between an index working and not, and its end matters as much as its start.
        /// </summary>
        public bool Wraps { get; }
    }

    /// <summary>
    /// What a node's tooltip says: a heading, a subheading and a set of rows.
    ///
    /// Built once when the hover changes rather than on every frame, and framework free so the same
    /// text can be drawn by the renderer, put on the clipboard, or sent to the AI assistant.
    /// </summary>
    public sealed class PlanTooltip
    {
        internal PlanTooltip(string title, string? subtitle, IReadOnlyList<PlanTooltipRow> rows, string? description = null)
        {
            Title = title;
            Subtitle = subtitle;
            Rows = rows;
            Description = description;
        }

        public string Title { get; }

        /// <summary>The object the operator touches, or null when it touches none.</summary>
        public string? Subtitle { get; }

        public IReadOnlyList<PlanTooltipRow> Rows { get; }

        /// <summary>
        /// What the operator does, in a sentence or two, for readers who do not know every operator
        /// by heart - see <see cref="Model.PlanOperatorDescriptions"/>.  Null for arrows and statements.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The tooltip as plain text, for pasting into a ticket or a chat.  This is the summary of
        /// everything worth knowing about an operator, so it is the one thing worth copying.
        /// </summary>
        public override string ToString()
        {
            var text = new StringBuilder(Title);
            if (Subtitle is not null) text.Append('\n').Append(Subtitle);

            foreach (var row in Rows)
            {
                text.Append('\n').Append(row.Label).Append(": ").Append(row.Value);
            }

            return text.ToString();
        }
    }
}
