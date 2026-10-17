using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DBADash;

namespace DBADashService
{
    /// <summary>
    /// Reports each instance's effective collection schedule to the repository independently of the normal
    /// per-instance collection pipeline. Schedule info comes entirely from the service's own in-memory
    /// config, so it doesn't need to query the monitored instance and must not be gated behind a live
    /// connection to it - e.g. an offline instance, or an Azure DB discovered on the fly whose first
    /// connection attempt hasn't necessarily succeeded yet. The one exception is a source whose
    /// ConnectionID isn't already known (normally only happens if it wasn't added via the config tools),
    /// where a live connection is attempted as a best-effort fallback to resolve it - see ReportAsync.
    /// </summary>
    public static class ScheduleInfoReporter
    {
        private const int ReportParallelism = 4;

        /// <summary>
        /// Reports the effective schedule for each of <paramref name="sources"/> - after a schedule change, pause or
        /// temporary schedule ending, so the repository reflects it straight away rather than at the next service start.
        /// A failure for one source is logged and doesn't stop the others.  Returns a description of each failure, so a
        /// caller can tell the user the repository doesn't reflect the change (e.g. that a pause isn't muting alerts).
        /// </summary>
        public static async Task<List<string>> ReportAsync(IEnumerable<DBADashSource> sources, CollectionConfig config, CancellationToken cancellationToken = default)
        {
            var failures = new ConcurrentBag<string>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = ReportParallelism, CancellationToken = cancellationToken };
            await Parallel.ForEachAsync(sources, options, async (src, _) =>
            {
                try
                {
                    var effectiveCustomCollections = src.CustomCollections.CombineCollections(config.CustomCollections);
                    await ReportAsync(src, config.GetEffectiveSchedules(src), effectiveCustomCollections, config);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error reporting schedule info for {instance}", src.SourceConnection.ConnectionForPrint);
                    failures.Add($"{src.ConnectionID ?? src.SourceConnection.ConnectionForPrint}: {(ex as AggregateException)?.InnerExceptions.FirstOrDefault()?.Message ?? ex.Message}");
                }
            });
            return failures.ToList();
        }

        public static async Task ReportAsync(DBADashSource source, CollectionSchedules effectiveSchedule,
            Dictionary<string, CustomCollection> effectiveCustomCollections, CollectionConfig config)
        {
            if (string.IsNullOrEmpty(source.ConnectionID))
            {
                // Normally already set when the source was added via the config tools (DBADashConfig/
                // ServiceConfig both call this on save) - if it wasn't, e.g. a hand-edited config, fall
                // back to resolving it live rather than giving up outright. This is the one case where
                // schedule info reporting still depends on the instance being reachable; once resolved it's
                // cached on the source (GetGeneratedConnectionIDAsync) for the rest of this process's
                // lifetime, so this only pays the live-connection cost once.
                //
                // Skip the attempt entirely if the instance is already known offline (e.g. from an earlier
                // collection this run)
                if (!OfflineInstances.IsOffline(source))
                {
                    try
                    {
                        source.ConnectionID = await source.GetGeneratedConnectionIDAsync();
                    }
                    catch (DatabaseConnectionException)
                    {
                        // Instance is offline and ConnectionID still isn't known - nothing to attach a report to.
                    }
                }

                if (string.IsNullOrEmpty(source.ConnectionID))
                {
                    Log.Warning("Skipping schedule info report for {connection} - ConnectionID isn't known and couldn't be resolved (instance may be offline, or this source wasn't added via the config tools)",
                        source.SourceConnection.ConnectionForPrint);
                    return;
                }
            }

            var ds = BuildDataSet(source, effectiveSchedule, effectiveCustomCollections,
                CollectionPauseStore.Current.IsPaused(source.ConnectionID, out var pausedUntil) ? pausedUntil : null);
            var fileName = DBADashSource.GenerateFileName(source.SourceConnection.ConnectionForFileName + "_ScheduleInfo");
            await DestinationHandling.WriteAllDestinationsAsync(ds, fileName, config);
        }

        private static DataSet BuildDataSet(DBADashSource source, CollectionSchedules effectiveSchedule,
            Dictionary<string, CustomCollection> effectiveCustomCollections, DateTime? collectionsPausedUntil)
        {
            var ds = new DataSet("ScheduleInfo");

            var dtAgent = new DataTable("DBADash");
            DBCollector.AddDBADashServiceMetadata(ref dtAgent);
            dtAgent.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            // Folder/bucket destinations (DirectoryWorkItem/S3WorkItem) identify a deserialized file by
            // Tables["DBADash"].Rows[0]["Instance"]/["DBName"] before importing it - these columns aren't
            // otherwise needed here, but must be present or that lookup throws (logged as "Error getting
            // ID from DataSet") and falls back to a shared "DEFAULT" key, serializing unrelated instances'
            // file processing against each other.
            dtAgent.Columns.Add("Instance", typeof(string));
            dtAgent.Columns.Add("DBName", typeof(string));
            dtAgent.Rows[0]["ConnectionID"] = source.ConnectionID;
            dtAgent.Rows[0]["SnapshotDateUTC"] = DateTime.UtcNow;
            dtAgent.Rows[0]["Instance"] = source.ConnectionID;
            dtAgent.Rows[0]["DBName"] = "ScheduleInfo";
            // When scheduled collections for the instance are paused (null = not paused).  The repository mutes alerts
            // for the instance and doesn't report its collections as overdue until then.
            dtAgent.Columns.Add("CollectionsPausedUntil", typeof(DateTime));
            dtAgent.Rows[0]["CollectionsPausedUntil"] = (object)collectionsPausedUntil ?? DBNull.Value;
            ds.Tables.Add(dtAgent);

            var dt = new DataTable("ScheduleInfo");
            dt.Columns.Add("Reference", typeof(string));
            dt.Columns.Add("Schedule", typeof(string));
            dt.Columns.Add("RunOnServiceStart", typeof(bool));
            dt.Columns.Add("MaxIntervalMinutes", typeof(decimal));
            dt.Columns.Add("IsInstanceOverride", typeof(bool));
            // When the schedule is a temporary one (see TemporaryScheduleStore), the time it reverts.  Null otherwise.
            dt.Columns.Add("TemporaryUntil", typeof(DateTime));

            var temporary = TemporaryScheduleStore.Current.Get(source.ConnectionID);
            foreach (var (type, schedule) in effectiveSchedule)
            {
                if (type == CollectionType.SchemaSnapshot && source.SchemaSnapshotDBs is not { Length: > 0 })
                {
                    // SchedulerService only ever schedules a SchemaSnapshot job when at least one DB is
                    // configured for it - otherwise it never collects, so reporting a schedule for it here
                    // would make it look like a permanently-stale/Critical reference instead of simply not
                    // applicable to this instance.
                    continue;
                }
                AddRow(dt, type.ToString(),
                    source.IsCollectionDisabledByConfiguration(type) ? DisabledSchedule : schedule,
                    source.CollectionSchedules != null && source.CollectionSchedules.ContainsKey(type),
                    temporary != null && temporary.TryGetValue(type, out var temporarySchedule) ? temporarySchedule.UntilUtc : null);
            }

            // Custom collections self-report to dbo.CollectionDates as 'UserData.{name}' (see the generated
            // _Upd proc in ManageCustomCollections) - matching that same Reference here lets the existing
            // CollectionDatesStatus join pick them up with no further schema/view changes.
            foreach (var (name, custom) in effectiveCustomCollections ?? new())
            {
                AddRow(dt, "UserData." + name, custom,
                    source.CustomCollections != null && source.CustomCollections.ContainsKey(name));
            }
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>An empty schedule is what dbo.ScheduleInfo already reads as disabled - see its IsEnabled
        /// computed column, which the CollectionDatesStatus view reports as "Schedule disabled".  A collection
        /// switched off by configuration rather than by its schedule is reported this way too, so the instance
        /// doesn't show as overdue for a collection that was never going to run - see
        /// <see cref="DBADashSource.IsCollectionDisabledByConfiguration"/>.</summary>
        private static readonly CollectionSchedule DisabledSchedule =
            new() { Schedule = string.Empty, RunOnServiceStart = false };

        private static void AddRow(DataTable dt, string reference, CollectionSchedule schedule, bool isInstanceOverride, DateTime? temporaryUntil = null)
        {
            var row = dt.NewRow();
            row["Reference"] = reference;
            row["Schedule"] = schedule.NormalizedSchedule ?? string.Empty;
            row["RunOnServiceStart"] = schedule.RunOnServiceStart;
            row["MaxIntervalMinutes"] = CronParser.TryGetMaxIntervalMinutes(schedule.Schedule, out var minutes) ? (decimal)minutes : DBNull.Value;
            row["IsInstanceOverride"] = isInstanceOverride;
            row["TemporaryUntil"] = (object)temporaryUntil ?? DBNull.Value;
            dt.Rows.Add(row);
        }
    }
}
