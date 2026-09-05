using System;
using DBADash.Deadlock.Layout;

namespace DBADash.Deadlock.Interaction
{
    /// <summary>
    /// Raised when the statement preview link of a node is activated - see
    /// <see cref="DeadlockViewController.StatementActivated"/>.  Carries the node and the full
    /// statement text so a host can open it however it likes (a code viewer, an editor, the
    /// clipboard) without reaching back into the layout.
    /// </summary>
    public sealed class DeadlockStatementActivatedEventArgs : EventArgs
    {
        public DeadlockStatementActivatedEventArgs(DeadlockNode node, string statement)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Statement = statement ?? throw new ArgumentNullException(nameof(statement));
        }

        /// <summary>The node whose statement was activated.</summary>
        public DeadlockNode Node { get; }

        /// <summary>The full statement text, not the truncated preview shown on the node.</summary>
        public string Statement { get; }
    }
}
