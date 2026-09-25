using DBADash;
using DBADashGUI.Deadlocks;
using DBADashGUI.Performance;
using DBADashGUI.QueryPlans;
using DBADashGUI.Viewers;
using System.Windows.Forms;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Backs the plan and deadlock viewers with the DBA Dash GUI: AI analysis through the repository's AI service,
    /// collecting a statement's current plans from the source instance, and the Query Store lookup.
    ///
    /// <paramref name="context"/> is the instance the plan or graph came from.  It is null for one opened from a file or
    /// from a grid with no instance behind it, which still gets the AI analysis but not the lookups that need an
    /// instance to go back to.
    /// </summary>
    internal sealed class DBADashViewerHost : IViewerHost
    {
        private readonly DBADashContext _context;

        internal DBADashViewerHost(DBADashContext context = null)
        {
            _context = context;
        }

        /// <summary>A host for the instance, or null when there isn't one - the viewer then uses the default host.</summary>
        internal static IViewerHost Create(DBADashContext context) => context == null ? null : new DBADashViewerHost(context);

        public string InstanceName => _context?.InstanceName;

        public IPlanAiPanel CreatePlanAiPanel() => new QueryPlanAiControl(_context);

        public IDeadlockAiPanel CreateDeadlockAiPanel() => new DeadlockAiControl(_context);

        public bool CanLookupPlans => DeadlockPlansControl.CanShow(_context);

        public IDeadlockPlansPanel CreateDeadlockPlansPanel(ToolStripStatusLabel status) =>
            new DeadlockPlansControl(_context, status);

        public bool CanShowQueryStore => CanLookupPlans;

        public void ShowQueryStore(string databaseName, string objectName)
        {
            if (_context == null) return;

            var context = _context.DeepCopy();
            context.DatabaseName = databaseName;
            context.ObjectName = objectName;
            context.Type = SQLTreeItem.TreeType.StoredProcedure;

            var frm = new QueryStoreViewer { Context = context };
            frm.ShowSingleInstance();
        }
    }
}
