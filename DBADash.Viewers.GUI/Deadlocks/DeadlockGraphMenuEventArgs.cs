using DBADash.Deadlock.Layout;
using System;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Raised while a right click menu is being built on the graph, so the host can add the actions
    /// that need more than the graph itself.
    ///
    /// The split is deliberate: <see cref="DeadlockGraphControl"/> contributes what can be answered
    /// from the deadlock alone - copying a statement, a SPID, a node's detail - and the host adds
    /// everything that needs a repository or the rest of the viewer (cached plans, Query Store, the
    /// grids).  A graph opened from a file with no connection therefore still gets a useful menu,
    /// without the control learning about any of that.
    /// </summary>
    public sealed class DeadlockGraphMenuEventArgs : EventArgs
    {
        public DeadlockGraphMenuEventArgs(DeadlockNode node, ToolStripItemCollection items)
        {
            Node = node;
            Items = items;
        }

        /// <summary>The node the menu was opened over, or null for empty canvas.</summary>
        public DeadlockNode Node { get; }

        /// <summary>
        /// The menu being built.  Items added here are appended after the control's own, so the
        /// handler should add its own separator when it wants one.
        /// </summary>
        public ToolStripItemCollection Items { get; }
    }
}
