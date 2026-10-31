using DBADash.Deadlock.Model;
using DBADash.QueryPlan.Model;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// What the plan and deadlock viewers can do only when they know where the artifact came from: run an AI analysis,
    /// collect the current plan for a statement from the instance, look a module up in Query Store.
    ///
    /// Everything is optional.  A viewer opened with no host - a file from Explorer, the stand-alone viewer app -
    /// shows everything the plan or graph itself carries and simply doesn't offer these.  The DBA Dash GUI supplies a
    /// host that is backed by the monitored instance the artifact was opened from.
    /// </summary>
    public interface IViewerHost
    {
        /// <summary>The instance the artifact came from, where known.</summary>
        string InstanceName => null;

        /// <summary>The repository's InstanceID for <see cref="InstanceName"/>, where known.</summary>
        int? InstanceId => null;

        /// <summary>
        /// The panel for the plan viewer's AI Analysis tab.  Null when there is no AI analysis to offer, which
        /// leaves the tab out.
        /// </summary>
        IPlanAiPanel CreatePlanAiPanel() => null;

        /// <summary>The panel for the deadlock viewer's AI Analysis tab.  Null leaves the tab out.</summary>
        IDeadlockAiPanel CreateDeadlockAiPanel() => null;

        /// <summary>
        /// True when the cached plans for a deadlocked statement can be looked up on the source instance.
        /// Evaluated once per viewer: it can take a round trip to answer, and the answer can't change while
        /// the viewer is open.
        /// </summary>
        bool CanLookupPlans => false;

        /// <summary>
        /// The panel under the deadlock viewer's process grid that lists an instance's cached plans for a
        /// statement.  Only asked for when <see cref="CanLookupPlans"/> is true.
        /// </summary>
        /// <param name="status">Status bar label of the viewer, for progress and errors.</param>
        IDeadlockPlansPanel CreateDeadlockPlansPanel(ToolStripStatusLabel status) => null;

        /// <summary>True when <see cref="ShowQueryStore"/> can open a Query Store view of a module.</summary>
        bool CanShowQueryStore => false;

        /// <summary>Opens Query Store for a module of the source instance.</summary>
        void ShowQueryStore(string databaseName, string objectName)
        {
        }

        /// <summary>
        /// True when <see cref="ShowHashDetail"/> can show the detail for a query or plan hash.  Separate from
        /// <see cref="CanShowQueryStore"/> so a host that only implements the module lookup doesn't get hash links that
        /// do nothing.
        /// </summary>
        bool CanShowHashDetail => false;

        /// <summary>
        /// Shows the detail for a statement's query hash or query plan hash on the source instance, as the plan
        /// writes them ("0x..."). Only offered when <see cref="CanShowHashDetail"/> is true.
        /// </summary>
        /// <param name="queryHash">The query hash to look for, or null.</param>
        /// <param name="queryPlanHash">The query plan hash to look for, or null.</param>
        void ShowHashDetail(string queryHash, string queryPlanHash)
        {
        }

        /// <summary>
        /// A toolbar button that hands what the viewer shows to an AI tool - the DBA Dash GUI's Ask menu of Claude Code,
        /// Cursor and the like.  Null leaves it out, which every host but the GUI does.
        /// </summary>
        /// <param name="request">What the viewer is showing, read when a tool is picked.</param>
        ToolStripItem CreateAskButton(System.Func<ViewerAskRequest> request) => null;
    }

    /// <summary>
    /// What a viewer hands to an AI tool: the deadlock shown, or the plan and the statement shown in it (numbered from 1
    /// among those with plans).
    /// </summary>
    public sealed record ViewerAskRequest(DeadlockGraph Deadlock, string PlanXml, string Title, int Statement);

    /// <summary>The AI Analysis tab of the plan viewer.</summary>
    public interface IPlanAiPanel
    {
        Control Control { get; }

        /// <summary>
        /// Points the panel at a statement and shows what would be sent about it.  Contacts nothing: the tab can
        /// be opened, read and closed again without a byte leaving the machine.  Called again whenever the reader
        /// picks a different statement.
        /// </summary>
        void Show(ExecutionPlan plan, PlanStatement statement, string fileName);
    }

    /// <summary>The AI Analysis tab of the deadlock viewer.</summary>
    public interface IDeadlockAiPanel
    {
        Control Control { get; }

        /// <summary>Points the panel at a deadlock and shows what would be sent about it.  Contacts nothing.</summary>
        void Show(DeadlockGraph graph);
    }

    /// <summary>The panel listing an instance's cached plans for a deadlocked statement.</summary>
    public interface IDeadlockPlansPanel
    {
        Control Control { get; }

        /// <summary>Raised when the user closes the panel, so the viewer can collapse it.</summary>
        event EventHandler CloseRequested;

        /// <summary>Lists the cached plans for a statement, replacing whatever the panel was showing.</summary>
        Task ShowStatementAsync(string sqlHandle, int statementStart, string databaseName, string caption);
    }
}
