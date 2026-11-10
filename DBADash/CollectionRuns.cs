using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace DBADash
{
    /// <summary>
    /// How a collection run went.  Stored as a TINYINT in the repository - see dbo.CollectionRuns.
    /// </summary>
    public enum CollectionRunStatus : byte
    {
        Succeeded = 1,
        /// <summary>Succeeded, with a warning logged (a TableSize threshold, say)</summary>
        Warning = 2,
        /// <summary>Succeeded after one or more attempts failed</summary>
        Retried = 3,
        /// <summary>Succeeded, but part of it logged an error (one WMI query out of several, say)</summary>
        Partial = 4,
        Failed = 5,
        /// <summary>Collected, but the data couldn't be imported into the repository.  Set by the importer.</summary>
        ImportFailed = 6
    }

    /// <summary>
    /// The CollectionRuns table the collector adds to the data it sends: a row per collection run, imported by
    /// dbo.CollectionRuns_Upd.  Column order matches the dbo.CollectionRuns table type, which the import passes it as.
    /// </summary>
    public static class CollectionRunsTable
    {
        public const string TableName = "CollectionRuns";

        /// <summary>
        /// Extended property saying whether the repository keeps each run as well as the hourly rollup - the service's
        /// LogInternalPerformanceCounters setting.  A string, as extended properties are when the data is serialized.
        /// </summary>
        public const string StoreDetailProperty = "StoreDetail";

        public static DataTable Create() => new(TableName)
        {
            Columns =
            {
                new DataColumn("Reference", typeof(string)),
                new DataColumn("Step", typeof(string)),
                new DataColumn("SnapshotDate", typeof(DateTime)),
                new DataColumn("Status", typeof(byte)),
                new DataColumn("Attempts", typeof(byte)),
                new DataColumn("DurationMs", typeof(int)),
                new DataColumn("CpuMs", typeof(int)),
                new DataColumn("LogicalReads", typeof(long))
            }
        };

        /// <summary>
        /// How a run went, from whether it threw and the errors it logged while carrying on.  Many collections catch their own
        /// failure and log it rather than throwing - Drivers when WMI can't be reached, say - so an error the collection logged
        /// against itself is a failure when it collected nothing, and a partial failure when it still got data (one WMI query of
        /// several failing for ServerExtraProperties).  Retry entries aren't errors, and warnings only make it a warning.
        /// </summary>
        /// <param name="reference">The collection - errors logged against it are its own</param>
        /// <param name="failed">The collection threw after all its attempts</param>
        /// <param name="attempts">Attempts it took</param>
        /// <param name="collectedData">The run added data (a table) to send</param>
        /// <param name="errors">Errors logged while the run was in progress</param>
        public static CollectionRunStatus GetStatus(string reference, bool failed, int attempts, bool collectedData, IEnumerable<RunError> errors)
        {
            if (failed) return CollectionRunStatus.Failed;
            var hasError = false;
            var hasOwnError = false;
            var hasWarning = false;
            foreach (var error in errors)
            {
                if (error.Context.Contains("[Retry]", StringComparison.OrdinalIgnoreCase)) continue;
                if (error.Context.Contains("[Warning]", StringComparison.OrdinalIgnoreCase) || error.Message.StartsWith("Warning", StringComparison.OrdinalIgnoreCase))
                {
                    hasWarning = true;
                    continue;
                }
                hasError = true;
                hasOwnError |= string.Equals(error.Source, reference, StringComparison.OrdinalIgnoreCase);
            }
            if (hasOwnError && !collectedData) return CollectionRunStatus.Failed;
            if (hasError) return CollectionRunStatus.Partial;
            if (attempts > 1) return CollectionRunStatus.Retried;
            return hasWarning ? CollectionRunStatus.Warning : CollectionRunStatus.Succeeded;
        }

        public static bool GetStoreDetail(DataTable dt) =>
            dt.ExtendedProperties.ContainsKey(StoreDetailProperty)
            && bool.TryParse(Convert.ToString(dt.ExtendedProperties[StoreDetailProperty]), out var storeDetail)
            && storeDetail;

        public static void SetStoreDetail(DataTable dt, bool storeDetail) =>
            dt.ExtendedProperties[StoreDetailProperty] = storeDetail.ToString();

        /// <summary>
        /// Identifies the batch (file), so the repository imports it once.  Not the SnapshotDate alone: two schedules for the
        /// same instance can start within the same 10ms and send different batches with the same SnapshotDate.
        /// </summary>
        public const string BatchIdProperty = "BatchID";

        public static void SetBatchId(DataTable dt, Guid batchId) =>
            dt.ExtendedProperties[BatchIdProperty] = batchId.ToString();

        /// <summary>The batch's id, or null for a file from before batches had one - those are told apart by SnapshotDate only</summary>
        public static Guid? GetBatchId(DataTable dt) =>
            dt.ExtendedProperties.ContainsKey(BatchIdProperty) && Guid.TryParse(Convert.ToString(dt.ExtendedProperties[BatchIdProperty]), out var batchId)
                ? batchId
                : null;

        /// <summary>When the batch was first handed to a destination (UTC, the collector's clock) - see <see cref="MarkSent"/></summary>
        public const string SentProperty = "SentUtc";

        /// <summary>
        /// Records when the batch was first handed to a destination - after everything in it was collected - so the import
        /// lag is the time it then waited (in S3 or a folder, say) rather than the time collecting the rest of the batch took.
        /// Kept once set: a file imported from S3 or a folder goes through the destinations again on its way into the repository.
        /// </summary>
        public static void MarkSent(DataSet ds)
        {
            if (ds.Tables[TableName] is not { } dt || dt.ExtendedProperties.ContainsKey(SentProperty)) return;
            dt.ExtendedProperties[SentProperty] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        /// <summary>When the batch was first handed to a destination, or null for a file that predates it</summary>
        public static DateTime? GetSent(DataTable dt) =>
            dt.ExtendedProperties.ContainsKey(SentProperty)
            && DateTime.TryParse(Convert.ToString(dt.ExtendedProperties[SentProperty]), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var sent)
                ? sent
                : null;

        /// <summary>
        /// Tables a collection imports that aren't named after it.  The table is named after the collection for nearly every
        /// collection - see <see cref="GetImportTarget"/>.
        /// </summary>
        private static readonly Dictionary<string, (string Reference, string Step)> ImportTargets = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DBADash"] = ("Instance", ""),
            ["JobSteps"] = ("Jobs", ""),
            ["AgentJobs"] = ("Jobs", ""), // Older services
            ["SessionWaits"] = ("RunningQueries", ""),
            ["RunningQueriesCursors"] = ("RunningQueries", ""),
            ["BlockingSnapshot"] = ("RunningQueries", ""), // Older services
            [LiveProfileCollector.TableName] = ("RunningQueries", "LiveProfiles"),
            ["SlowQueriesStats"] = ("SlowQueries", ""),
            ["ObjectExecutionStatsLegacy"] = ("ObjectExecutionStats", "")
        };

        /// <summary>
        /// The collection run (and step) an imported table belongs to.  Text and plans are fetched by a step of running queries
        /// and of query stats, into one table - counted against running queries' step where it ran, otherwise query stats'.
        /// </summary>
        public static (string Reference, string Step) GetImportTarget(string tableName, Func<(string Reference, string Step), bool> hasRun)
        {
            if (ImportTargets.TryGetValue(tableName, out var target)) return target;
            var step = tableName.ToUpperInvariant() switch
            {
                "QUERYTEXT" => "Text",
                "QUERYPLANS" => "Plans",
                _ => null
            };
            if (step == null) return (tableName, "");
            return hasRun(("RunningQueries", step)) ? ("RunningQueries", step) : ("QueryStats", step);
        }

        /// <summary>
        /// The import columns are added by the importer - they're measured where the data is imported, which can be a
        /// different service from the one that collected it.  After the collector's columns, as the table type has them.
        /// </summary>
        public static void EnsureImportColumns(DataTable dt)
        {
            if (!dt.Columns.Contains("ImportMs")) dt.Columns.Add("ImportMs", typeof(int));
            if (!dt.Columns.Contains("ImportCpuMs")) dt.Columns.Add("ImportCpuMs", typeof(int));
            if (!dt.Columns.Contains("ImportLogicalReads")) dt.Columns.Add("ImportLogicalReads", typeof(long));
        }

        /// <summary>
        /// Adds how the import went to the runs, by the collection each table belongs to (<see cref="GetImportTarget"/>):
        /// <list type="bullet">
        /// <item>The time spent importing its tables, and the repository CPU and reads where they were measured.</item>
        /// <item>A run whose data couldn't be imported is a failure too - <see cref="CollectionRunStatus.ImportFailed"/>.</item>
        /// </list>
        /// Tables that don't belong to a run (a schema snapshot, say) only count towards the file's import time.
        /// The import columns are set from <paramref name="tables"/> rather than added to, so it can be applied again - the
        /// import of the runs is retried on a transient error - without counting anything twice.
        /// </summary>
        public static void ApplyImport(DataTable dt, IEnumerable<ImportTableStats> tables, ICollection<string> failedTables)
        {
            EnsureImportColumns(dt);
            var runs = new Dictionary<(string, string), DataRow>(new ReferenceComparer());
            foreach (DataRow row in dt.Rows)
            {
                runs.TryAdd(((string)row["Reference"], row["Step"] as string ?? ""), row);
            }

            var totals = new Dictionary<DataRow, (double Ms, long? Cpu, long? Reads)>();
            foreach (var table in tables)
            {
                if (!runs.TryGetValue(GetImportTarget(table.TableName, runs.ContainsKey), out var row)) continue;
                var total = totals.GetValueOrDefault(row);
                totals[row] = (total.Ms + table.DurationMs,
                    table.CpuMs is { } cpu ? (total.Cpu ?? 0) + cpu : total.Cpu,
                    table.LogicalReads is { } reads ? (total.Reads ?? 0) + reads : total.Reads);
            }
            foreach (DataRow row in dt.Rows)
            {
                if (totals.TryGetValue(row, out var total))
                {
                    row["ImportMs"] = (int)Math.Clamp(Math.Round(total.Ms), 0, int.MaxValue);
                    row["ImportCpuMs"] = total.Cpu is { } cpu ? (int)Math.Clamp(cpu, 0, int.MaxValue) : DBNull.Value;
                    row["ImportLogicalReads"] = total.Reads is { } reads ? reads : DBNull.Value;
                }
                else
                {
                    row["ImportMs"] = DBNull.Value;
                    row["ImportCpuMs"] = DBNull.Value;
                    row["ImportLogicalReads"] = DBNull.Value;
                }
            }

            foreach (var tableName in failedTables)
            {
                if (!runs.TryGetValue(GetImportTarget(tableName, runs.ContainsKey), out var row)) continue;
                if (row["Status"] is byte status && status == (byte)CollectionRunStatus.Failed) continue;
                row["Status"] = (byte)CollectionRunStatus.ImportFailed;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<(string, string)>
        {
            public bool Equals((string, string) x, (string, string) y) =>
                StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) && StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2);

            public int GetHashCode((string, string) obj) =>
                HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
        }
    }

    /// <summary>An error logged while a collection run was in progress - see <see cref="CollectionRunsTable.GetStatus"/></summary>
    public readonly record struct RunError(string Source, string Context, string Message);

    /// <summary>
    /// The time a table took to import - all attempts - and the CPU and logical reads it caused in the repository, where
    /// measured.  See <see cref="CollectionRunsTable.ApplyImport"/>.
    /// </summary>
    public sealed record ImportTableStats(string TableName, double DurationMs, long? CpuMs, long? LogicalReads);
}
