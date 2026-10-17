using DBADashService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Pauses all scheduled collections for one or more instances until a given time, or resumes them.  Uses the same
    /// service option as schedule changes (<see cref="CollectionConfig.AllowScheduleChanges"/>).
    ///
    /// <para>The pause is runtime state rather than config, kept in <see cref="CollectionPauseStore"/> - its own file
    /// beside the service, not ServiceConfig.json - so it survives a restart, and expires by itself.  It's reported to the repository with the schedule info, where it mutes alerts for
    /// the instance and stops its collections being reported as overdue until the pause ends.  Collections triggered
    /// manually from the GUI still run.</para>
    /// </summary>
    public class CollectionPauseMessage : MessageBase
    {
        /// <summary>The instances to pause or resume - all collected by the service the message is sent to.</summary>
        public List<string> ConnectionIDs { get; set; } = new();

        /// <summary>When collection resumes (UTC).  Null to resume now.</summary>
        public DateTime? PauseUntilUtc { get; set; }

        /// <summary>Longest pause allowed - a pause is meant for maintenance, not to switch an instance off indefinitely.</summary>
        public static readonly TimeSpan MaxPause = TimeSpan.FromDays(31);

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();

            if (!cfg.AllowScheduleChanges)
            {
                throw new Exception("Pausing collections is not enabled on the DBA Dash service.  Use the service configuration tool to allow schedule changes.");
            }

            Validate(DateTime.UtcNow);

            List<DBADashSource> sources;
            lock (cfg.SourceConnections)
            {
                sources = cfg.SourceConnections
                    .Where(s => ConnectionIDs.Contains(s.ConnectionID, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }
            var unknown = ConnectionIDs
                .Where(id => !sources.Exists(s => string.Equals(s.ConnectionID, id, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
            if (unknown.Count > 0)
            {
                throw new ArgumentException($"Instance(s) not collected by this service: {string.Join(", ", unknown)}");
            }

            CollectionPauseStore.Current.Update(current => Apply(current, DateTime.UtcNow));

            Log.Information("Collections {action} via message {id} for {instances}", PauseUntilUtc.HasValue ? $"paused until {PauseUntilUtc:u}" : "resumed",
                Id, ConnectionIDs);

            // Report straight away so the repository mutes (or unmutes) alerts now rather than at the next service start.
            // The pause is in effect on the service either way - if the repository couldn't be told, say so.
            var failures = await ScheduleInfoReporter.ReportAsync(sources, cfg, cancellationToken);
            var warning = MessageWarnings.ForScheduleInfoFailures(failures,
                PauseUntilUtc.HasValue ? "it won't show the pause or mute alerts" : "it will still show the instance as paused and mute its alerts");
            return MessageWarnings.ToDataSet(warning == null ? null : new[] { warning });
        }

        /// <summary>Throws if the request is malformed.</summary>
        public void Validate(DateTime utcNow)
        {
            if (ConnectionIDs is not { Count: > 0 })
            {
                throw new ArgumentException("No instances were specified.");
            }
            if (PauseUntilUtc.HasValue)
            {
                var until = DateTime.SpecifyKind(PauseUntilUtc.Value, DateTimeKind.Utc);
                if (until <= utcNow)
                {
                    throw new ArgumentException("The pause must end in the future.");
                }
                if (until - utcNow > MaxPause)
                {
                    throw new ArgumentException($"Collections can be paused for up to {MaxPause.TotalDays} days.");
                }
            }
        }

        /// <summary>
        /// Returns <paramref name="current"/> with the requested instances paused (or resumed), dropping pauses that have
        /// already ended so the pause file doesn't accumulate them.  A new dictionary, never the one passed in.  Null when
        /// nothing is paused.
        /// </summary>
        public Dictionary<string, DateTime> Apply(Dictionary<string, DateTime> current, DateTime utcNow)
        {
            var updated = new Dictionary<string, DateTime>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var (id, until) in current ?? new Dictionary<string, DateTime>())
            {
                var untilUtc = DateTime.SpecifyKind(until, DateTimeKind.Utc);
                if (untilUtc > utcNow)
                {
                    updated[id] = untilUtc;
                }
            }

            foreach (var id in ConnectionIDs)
            {
                if (PauseUntilUtc.HasValue)
                {
                    updated[id] = DateTime.SpecifyKind(PauseUntilUtc.Value, DateTimeKind.Utc);
                }
                else
                {
                    updated.Remove(id);
                }
            }

            return updated.Count == 0 ? null : updated;
        }
    }
}
