using DBADash.PerformanceAnalysis;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Reads what a performance analysis is built from, using the procedures behind the charts the reader
    /// is looking at - see <see cref="PerformanceAnalysisData"/> for why those rather than one of its own.
    ///
    /// Everything is read once, at full depth, for both the window and the baseline, so changing which
    /// sections go or how many rows each sends rebuilds the payload without another round trip.  Only a
    /// new window reads again.
    ///
    /// Sections load independently and in parallel.  One that fails - a timeout on a large IO table, a
    /// procedure from a newer repository version - is recorded and said in its section, and the rest of
    /// the analysis still goes.
    /// </summary>
    internal static class PerformanceAnalysisLoader
    {
        public static async Task<PerformanceAnalysisData> LoadAsync(int instanceId, string instance, PerformanceAnalysisWindow window, CancellationToken cancellationToken)
        {
            var data = new PerformanceAnalysisData { Window = window, Instance = instance, InstanceId = instanceId };
            var from = window.FromUtc;
            var to = window.ToUtc;
            var (baseFrom, baseTo) = window.Baseline;
            var bucket = window.BucketMinutes;
            var seriesTop = PerformanceAnalysisOptions.SeriesTopN;

            async Task Section(PerformanceSection section, Func<Task> load)
            {
                try
                {
                    await load();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lock (data.Errors)
                    {
                        data.Errors[section] = ex.Message;
                    }
                }
            }

            Task<DataSet> Query(string procedure, params (string Name, object Value)[] parameters) =>
                QueryAsync(procedure, instanceId, parameters, cancellationToken);

            await Task.WhenAll(
                Section(PerformanceSection.Context, async () =>
                {
                    data.InstanceInfo = First(await Query("dbo.InstanceInfo_Get"));
                    data.Configuration = First(await QueryAsync("dbo.Configuration_Get", null, [("InstanceIDs", instanceId.ToString())], cancellationToken));
                }),
                Section(PerformanceSection.CPU, async () =>
                {
                    data.Cpu = First(await Query("dbo.CPU_Get", ("FromDate", from), ("ToDate", to), ("DateGroupingMin", bucket)));
                    data.CpuBaseline = First(await Query("dbo.CPU_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("DateGroupingMin", 60)));
                }),
                Section(PerformanceSection.Waits, async () =>
                {
                    data.Waits = First(await Query("dbo.WaitsSummary_Get", ("FromDate", from), ("ToDate", to)));
                    data.WaitsBaseline = First(await Query("dbo.WaitsSummary_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("Use60MIN", true)));
                    data.WaitsSeries = First(await Query("dbo.Waits_Get", ("FromDate", from), ("ToDate", to), ("DateGroupingMin", bucket), ("Top", seriesTop)));
                }),
                Section(PerformanceSection.IO, async () =>
                {
                    data.IODrives = First(await Query("dbo.IOSummary_Get", ("FromDate", from), ("ToDate", to), ("GroupBy", "Drive")));
                    data.IODrivesBaseline = First(await Query("dbo.IOSummary_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("GroupBy", "Drive"), ("Use60MIN", true)));
                    data.IODatabases = First(await Query("dbo.IOSummary_Get", ("FromDate", from), ("ToDate", to), ("GroupBy", "Database")));
                    data.IOSeries = First(await Query("dbo.IOStats_Get", ("FromDate", from), ("ToDate", to), ("DateGroupingMin", bucket)));
                }),
                Section(PerformanceSection.Metrics, async () =>
                {
                    data.Metrics = First(await Query("dbo.PerformanceCounterSummary_Get", ("FromDate", from), ("ToDate", to)));
                    data.MetricsBaseline = First(await Query("dbo.PerformanceCounterSummary_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("Use60Min", true)));
                }),
                Section(PerformanceSection.Memory, async () =>
                {
                    data.Memory = First(await Query("dbo.MemoryUsage_Get", ("FromDate", from), ("ToDate", to)));
                    data.MemoryBaseline = First(await Query("dbo.MemoryUsage_Get", ("FromDate", baseFrom), ("ToDate", baseTo)));
                }),
                Section(PerformanceSection.Blocking, async () =>
                {
                    data.Blocking = First(await Query("dbo.BlockingSnapshots_Get", ("FromDate", from), ("ToDate", to), ("Top", data.BlockingSnapshotLimit)));
                    data.BlockingBaseline = First(await Query("dbo.BlockingSnapshots_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("Top", data.BlockingSnapshotLimit)));
                }),
                Section(PerformanceSection.ObjectExecution, async () =>
                {
                    data.ObjectExecution = First(await Query("dbo.ObjectExecutionStatsSummary_Get", ("FromDate", from), ("ToDate", to),
                        ("CompareFrom", baseFrom), ("CompareTo", baseTo), ("Use60MINCompare", true)));
                }),
                Section(PerformanceSection.Latches, async () =>
                {
                    data.Latches = await Query("dbo.LatchStats_Get", ("FromDate", from), ("ToDate", to), ("DateGroupingMin", bucket), ("TopN", seriesTop));
                    data.LatchesBaseline = await Query("dbo.LatchStats_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("DateGroupingMin", 60), ("TopN", seriesTop));
                }),
                Section(PerformanceSection.Spinlocks, async () =>
                {
                    data.Spinlocks = await Query("dbo.SpinlockStats_Get", ("FromDate", from), ("ToDate", to), ("DateGroupingMin", bucket), ("TopN", seriesTop), ("Measure", "Spins"));
                    data.SpinlocksBaseline = await Query("dbo.SpinlockStats_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("DateGroupingMin", 60), ("TopN", seriesTop), ("Measure", "Spins"));
                }));

            return data;
        }

        private static DataTable First(DataSet set) => set.Tables.Count > 0 ? set.Tables[0] : null;

        private static async Task<DataSet> QueryAsync(string procedure, int? instanceId, IEnumerable<(string Name, object Value)> parameters, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand(procedure, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Config.DefaultCommandTimeout };
            if (instanceId.HasValue)
            {
                cmd.Parameters.AddWithValue("InstanceID", instanceId.Value);
            }

            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            // DataTable.Load moves the reader on to the next result set, and closes it after the last.
            var set = new DataSet();
            while (!reader.IsClosed)
            {
                var table = new DataTable();
                table.Load(reader);
                set.Tables.Add(table);
            }

            return set;
        }

        /// <summary>The sections that came back with nothing at all - for the dialog to grey out rather than offer.</summary>
        public static IEnumerable<PerformanceSection> Empty(PerformanceAnalysisData data)
        {
            if (data.Cpu is not { Rows.Count: > 0 }) yield return PerformanceSection.CPU;
            if (data.Waits is not { Rows.Count: > 0 }) yield return PerformanceSection.Waits;
            if (data.IODrives is not { Rows.Count: > 0 }) yield return PerformanceSection.IO;
            if (data.Metrics is null || !data.Metrics.Rows.Cast<DataRow>().Any(r => !PerformanceAnalysisPayload.IsInternalCounter(r))) yield return PerformanceSection.Metrics;
            if (data.Memory is not { Rows.Count: > 0 }) yield return PerformanceSection.Memory;
            if (data.ObjectExecution is not { Rows.Count: > 0 }) yield return PerformanceSection.ObjectExecution;
            if (NotCollected(data.Latches)) yield return PerformanceSection.Latches;
            if (NotCollected(data.Spinlocks)) yield return PerformanceSection.Spinlocks;
        }

        private static bool NotCollected(DataSet set) =>
            set is null || set.Tables.Count == 0 || set.Tables[0].Columns.Contains("Message");
    }
}
