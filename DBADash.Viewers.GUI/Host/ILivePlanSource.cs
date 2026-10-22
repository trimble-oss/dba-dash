using DBADash.QueryPlan.Live;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Where a live plan comes from: a query that is still running, which the viewer can refresh to follow its progress.
    /// The DBA Dash GUI gets it through messaging; the viewer only asks for the latest capture and draws it.
    /// </summary>
    public interface ILivePlanSource
    {
        /// <summary>What is being followed, for the viewer's toolbar - the session and instance.</summary>
        string Description { get; }

        /// <summary>
        /// Identifies the request being followed, so opening it again selects the tab that is already following it.
        /// Plan XML is not an identity: it changes on every refresh, and the same XML can be a static plan or another session.
        /// </summary>
        string RequestKey { get; }

        /// <summary>
        /// The latest capture of the running query.  Called repeatedly, one call at a time: the source keeps what it needs
        /// from the last capture to say which operators have moved since.
        /// </summary>
        Task<LivePlanCapture> CaptureAsync(CancellationToken cancellationToken);
    }

    /// <summary>One capture of a running query - see <see cref="ILivePlanSource"/>.</summary>
    public sealed class LivePlanCapture
    {
        /// <summary>The plan with its counters as they are now.  Null once the query has completed.</summary>
        public string PlanXml { get; init; }

        /// <summary>Where each operator has got to.  Null when the source couldn't tell.</summary>
        public PlanLiveSnapshot Live { get; init; }

        /// <summary>When it was captured, in the time zone the host shows times in.</summary>
        public DateTime CapturedAt { get; init; }

        /// <summary>
        /// True when the request has moved on to another statement, so this is a different plan rather than newer
        /// counters for the one being shown - the viewer starts again rather than keeping the reader's place.
        /// </summary>
        public bool PlanChanged { get; init; }

        /// <summary>True when the query is no longer running.  There is nothing more to capture.</summary>
        public bool Completed { get; init; }

        /// <summary>Anything to tell the reader about the capture - why it completed, for instance.</summary>
        public string Message { get; init; }

        /// <summary>How long the request has been running, where known.</summary>
        public TimeSpan? Elapsed { get; init; }
    }
}
