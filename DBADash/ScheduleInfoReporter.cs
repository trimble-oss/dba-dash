using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DBADash;

namespace DBADashService
{
    /// <summary>
    /// Reports each instance's effective collection schedule to the repository independently of the normal
    /// per-instance collection pipeline. Schedule info comes entirely from the service's own in-memory
    /// config and never needs to query the monitored instance, so it must not be gated behind a live
    /// connection to that instance - e.g. an offline instance, or an Azure DB discovered on the fly whose
    /// first connection attempt hasn't necessarily succeeded yet.
    /// </summary>
    public static class ScheduleInfoReporter
    {
        public static async Task ReportAsync(DBADashSource source, CollectionSchedules effectiveSchedule,
            Dictionary<string, CustomCollection> effectiveCustomCollections, CollectionConfig config)
        {
            if (string.IsNullOrEmpty(source.ConnectionID))
            {
                Log.Warning("Skipping schedule info report for {connection} - ConnectionID isn't known yet (instance has never connected)",
                    source.SourceConnection.ConnectionForPrint);
                return;
            }

            var ds = BuildDataSet(source, effectiveSchedule, effectiveCustomCollections);
            var fileName = DBADashSource.GenerateFileName(source.SourceConnection.ConnectionForFileName + "_ScheduleInfo");
            await DestinationHandling.WriteAllDestinationsAsync(ds, fileName, config);
        }

        private static DataSet BuildDataSet(DBADashSource source, CollectionSchedules effectiveSchedule,
            Dictionary<string, CustomCollection> effectiveCustomCollections)
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
            ds.Tables.Add(dtAgent);

            var dt = new DataTable("ScheduleInfo");
            dt.Columns.Add("Reference", typeof(string));
            dt.Columns.Add("Schedule", typeof(string));
            dt.Columns.Add("RunOnServiceStart", typeof(bool));
            dt.Columns.Add("MaxIntervalMinutes", typeof(decimal));
            dt.Columns.Add("IsInstanceOverride", typeof(bool));

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
                AddRow(dt, type.ToString(), schedule,
                    source.CollectionSchedules != null && source.CollectionSchedules.ContainsKey(type));
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

        private static void AddRow(DataTable dt, string reference, CollectionSchedule schedule, bool isInstanceOverride)
        {
            var row = dt.NewRow();
            row["Reference"] = reference;
            row["Schedule"] = schedule.NormalizedSchedule ?? string.Empty;
            row["RunOnServiceStart"] = schedule.RunOnServiceStart;
            row["MaxIntervalMinutes"] = CronParser.TryGetMaxIntervalMinutes(schedule.Schedule, out var minutes) ? (decimal)minutes : DBNull.Value;
            row["IsInstanceOverride"] = isInstanceOverride;
            dt.Rows.Add(row);
        }
    }
}
