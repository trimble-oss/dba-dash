using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using DBADashGUI.Viewers;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Follows a running request for the plan viewer, through messaging: <see cref="LivePlanMessage"/> for the whole plan
    /// and <see cref="LiveQueryProfileMessage"/> for each refresh's counters.  How they are put together is
    /// <see cref="LivePlanFollower"/>'s.
    /// </summary>
    internal sealed class LivePlanSource : LivePlanFollower
    {
        private readonly DBADashContext _context;
        private readonly DateTime _startTimeUtc;
        private readonly int _snapshotUtcOffset;

        /// <param name="initial">The LivePlan row the plan was first opened from, which says which statement it is for.</param>
        /// <param name="snapshotUtcOffset">The instance UTC offset, in minutes, captured with the snapshot. Sent on every poll so a daylight saving change is the only start-time shift that still matches.</param>
        public LivePlanSource(DBADashContext context, int sessionId, DateTime startTimeUtc, int snapshotUtcOffset, DataRow initial)
            : base(sessionId, initial)
        {
            _context = context;
            _startTimeUtc = startTimeUtc;
            _snapshotUtcOffset = snapshotUtcOffset;
        }

        public override string Description => $"Session {SessionId} on {_context.InstanceName}";

        public override string RequestKey => RequestKeyOf(_context.ConnectionID, _startTimeUtc);

        protected override DateTime ToDisplayTime(DateTime utc) => utc.ToAppTimeZone();

        protected override Task<DataSet> ReadProfileAsync(CancellationToken cancellationToken) =>
            SendAsync(new LiveQueryProfileMessage
            {
                ConnectionID = _context.ConnectionID,
                CollectAgent = _context.CollectAgent,
                ImportAgent = _context.ImportAgent,
                SessionID = SessionId,
                StartTimeUtc = _startTimeUtc,
                SnapshotUtcOffset = _snapshotUtcOffset,
                Lifetime = 30
            }, cancellationToken);

        protected override Task<DataSet> ReadPlanAsync(CancellationToken cancellationToken) =>
            SendAsync(new LivePlanMessage
            {
                ConnectionID = _context.ConnectionID,
                CollectAgent = _context.CollectAgent,
                ImportAgent = _context.ImportAgent,
                SessionID = SessionId,
                StartTimeUtc = _startTimeUtc,
                SnapshotUtcOffset = _snapshotUtcOffset,
                Lifetime = 60
            }, cancellationToken);

        private async Task<DataSet> SendAsync(MessageBase message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_context.ImportAgentID is not { } importAgent)
            {
                throw new InvalidOperationException("Messaging is not available for this instance.");
            }

            // Failures to send are reported through the status rather than the outcome, so the last one is kept to say
            // why there was no reply.
            string failure = null;
            var outcome = await MessagingHelper.SendAndWaitAsync(message, importAgent, (text, _, colour) =>
            {
                if (colour == DashColors.Fail) failure = text;
            });
            cancellationToken.ThrowIfCancellationRequested();

            if (!outcome.Succeeded) throw new InvalidOperationException(failure ?? outcome.Error);
            return outcome.Data ?? throw new InvalidOperationException("The service returned no data.");
        }
    }
}
