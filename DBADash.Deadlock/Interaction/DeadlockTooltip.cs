using System;
using System.Collections.Generic;

namespace DBADash.Deadlock.Interaction
{
    /// <summary>A label and value pair on a tooltip.</summary>
    public sealed class DeadlockTooltipRow
    {
        public DeadlockTooltipRow(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }

        public string Value { get; }

        public override string ToString() => $"{Label}: {Value}";
    }

    /// <summary>
    /// The content of a node's tooltip, as structured data rather than a formatted string.
    ///
    /// Built here rather than in the renderer so it can be unit tested, reused (the same rows suit a
    /// detail panel or a copied-to-clipboard summary), and kept identical across renderers.  The
    /// renderer decides only how to draw it.
    /// </summary>
    public sealed class DeadlockTooltip
    {
        public DeadlockTooltip(string title, string? subtitle, IReadOnlyList<DeadlockTooltipRow> rows)
        {
            Title = title;
            Subtitle = subtitle;
            Rows = rows;
        }

        public string Title { get; }

        /// <summary>Secondary heading, e.g. "Deadlock victim".  Null when there is nothing to add.</summary>
        public string? Subtitle { get; }

        public IReadOnlyList<DeadlockTooltipRow> Rows { get; }

        /// <summary>Convenience for tests and for copying a node's detail as text.</summary>
        public override string ToString()
        {
            var lines = new List<string> { Title };
            if (Subtitle is not null) lines.Add(Subtitle);
            foreach (var row in Rows)
            {
                lines.Add(row.ToString());
            }
            return string.Join(Environment.NewLine, lines);
        }
    }
}
