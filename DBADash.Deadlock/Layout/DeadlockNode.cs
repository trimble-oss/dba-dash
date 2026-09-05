using System;
using System.Collections.Generic;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Layout
{
    /// <summary>
    /// A positioned node in a laid out deadlock graph.  The renderer draws it and the interaction
    /// layer hit tests against <see cref="Bounds"/>; neither needs to know which subtype it is
    /// except when choosing how to paint it.
    /// </summary>
    public abstract class DeadlockNode
    {
        /// <summary>Stable identifier, taken from the underlying graph (process id or resource id).</summary>
        public string Id { get; internal set; } = string.Empty;

        /// <summary>The heading line, e.g. "SPID 61" or "Sales.dbo.Orders (PK_Orders)".</summary>
        public string Title { get; internal set; } = string.Empty;

        /// <summary>
        /// Supporting lines shown under the title, e.g. login and wait time.  Chosen here rather than
        /// in the renderer because the node is sized from them.
        /// </summary>
        public IReadOnlyList<string> DetailLines { get; internal set; } = Array.Empty<string>();

        /// <summary>
        /// A single-line preview of the process statement, drawn as a link under the detail lines and
        /// sized into the node.  Null when there is no statement to show (all resource nodes, and
        /// processes with neither an execution stack nor an input buffer).  The full text is opened
        /// by activating the node.
        /// </summary>
        public string? StatementPreview { get; internal set; }

        /// <summary>
        /// The clickable region of <see cref="StatementPreview"/>, relative to the node's top-left
        /// corner.  Null when there is no statement.  Kept relative so it survives the node being
        /// moved during placement; <see cref="StatementLinkBounds"/> turns it back into layout space.
        /// </summary>
        public LayoutRect? StatementLinkLocalBounds { get; internal set; }

        /// <summary>
        /// The clickable region of <see cref="StatementPreview"/> in layout space, or null when there
        /// is no statement.  The interaction layer hit tests against this to make the preview behave
        /// like a hyperlink rather than requiring the whole node to be activated.
        /// </summary>
        public LayoutRect? StatementLinkBounds =>
            StatementLinkLocalBounds is { } local
                ? new LayoutRect(Bounds.Left + local.X, Bounds.Top + local.Y, local.Width, local.Height)
                : null;

        public LayoutRect Bounds { get; internal set; }

        /// <summary>
        /// True when this node is part of the deadlock cycle - see <see cref="DeadlockLayout.Cycle"/>.
        /// Lets a renderer emphasise the cycle over incidental nodes.
        /// </summary>
        public bool IsInCycle { get; internal set; }
    }

    /// <summary>A process (session or parallel task) taking part in the deadlock.</summary>
    public sealed class DeadlockProcessNode : DeadlockNode
    {
        public DeadlockProcess Process { get; internal set; } = null!;

        /// <summary>Convenience for renderers, which almost always style the victim differently.</summary>
        public bool IsVictim => Process.IsVictim;
    }

    /// <summary>A resource the processes are contending for.</summary>
    public sealed class DeadlockResourceNode : DeadlockNode
    {
        public DeadlockResource Resource { get; internal set; } = null!;
    }
}
