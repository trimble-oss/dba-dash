using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.PerformanceAnalysis
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
    ///
    /// Shared by the GUI's analysis dialog and the MCP servers' get-performance-summary tool, which pass their own
    /// repository connection.
    /// </summary>
    public static class PerformanceAnalysisLoader
    {
        public static async Task<PerformanceAnalysisData> LoadAsync(string connectionString, int commandTimeout, int instanceId, string instance, PerformanceAnalysisWindow window, CancellationToken cancellationToken)
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
                QueryAsync(connectionString, commandTimeout, procedure, instanceId, parameters, cancellationToken);

            await Task.WhenAll(
                Section(PerformanceSection.Context, async () =>
                {
                    data.InstanceInfo = First(await Query("dbo.InstanceInfo_Get"));
                    data.Configuration = First(await QueryAsync(connectionString, commandTimeout, "dbo.Configuration_Get", null, [("InstanceIDs", instanceId.ToString())], cancellationToken));
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
                Section(PerformanceSection.RunningQueries, async () =>
                {
                    data.RunningQueries = First(await Query("dbo.RunningQueriesSummary_Get", ("FromDate", from), ("ToDate", to), ("MaxRows", data.RunningQueriesSnapshotLimit)));
                    data.RunningQueriesBaseline = First(await Query("dbo.RunningQueriesSummary_Get", ("FromDate", baseFrom), ("ToDate", baseTo), ("MaxRows", data.RunningQueriesSnapshotLimit)));
                }),
                Section(PerformanceSection.Deadlocks, async () =>
                {
                    // Both take a list of instances as well as the one - the list is what decides whether the collection is enabled.
                    data.Deadlocks = await Query("dbo.DeadlockCharts_Get", ("InstanceIDs", InstanceIds(instanceId)), ("FromDate", from), ("ToDate", to),
                        ("DateGroupingMin", bucket), ("TopN", PerformanceAnalysisOptions.MaxTopN));
                    data.DeadlocksBaseline = await Query("dbo.DeadlockCharts_Get", ("InstanceIDs", InstanceIds(instanceId)), ("FromDate", baseFrom), ("ToDate", baseTo),
                        ("DateGroupingMin", 60), ("TopN", 1));
                    // Only the signatures: the later results carry every participant's statement and input buffer.
                    data.DeadlockSignatures = await FirstResultAsync(connectionString, commandTimeout, "dbo.DeadlockSummary_Get", instanceId,
                        [("InstanceIDs", InstanceIds(instanceId)), ("FromDate", from), ("ToDate", to)], cancellationToken);
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

        /// <summary>One instance as a dbo.IDs table, for the procedures that take a list of instances.</summary>
        private static DataTable InstanceIds(int instanceId)
        {
            var table = new DataTable();
            table.Columns.Add("ID", typeof(int));
            table.Rows.Add(instanceId);
            return table;
        }

        private static async Task<DataSet> QueryAsync(string connectionString, int commandTimeout, string procedure, int? instanceId, IEnumerable<(string Name, object Value)> parameters, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = Command(procedure, cn, commandTimeout, instanceId, parameters);
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

        /// <summary>The first result set only, for a procedure whose later results are large and not needed.  Null when it returned a message in place of data.</summary>
        private static async Task<DataTable> FirstResultAsync(string connectionString, int commandTimeout, string procedure, int? instanceId, IEnumerable<(string Name, object Value)> parameters, CancellationToken cancellationToken)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = Command(procedure, cn, commandTimeout, instanceId, parameters);
            await cn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var table = new DataTable();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            }

            var values = new object[reader.FieldCount];
            while (await reader.ReadAsync(cancellationToken))
            {
                reader.GetValues(values);
                table.Rows.Add(values);
            }

            // Cancel rather than drain the rest - closing the reader would otherwise read every remaining result to the end.
            cmd.Cancel();
            return table.Columns.Contains("Message") ? null : table;
        }

        private static SqlCommand Command(string procedure, SqlConnection cn, int commandTimeout, int? instanceId, IEnumerable<(string Name, object Value)> parameters)
        {
            var cmd = new SqlCommand(procedure, cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = commandTimeout };
            if (instanceId.HasValue)
            {
                cmd.Parameters.AddWithValue("InstanceID", instanceId.Value);
            }

            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            return cmd;
        }

        /// <summary>The sections that came back with nothing at all - for the dialog to grey out rather than offer.</summary>
        public static IEnumerable<PerformanceSection> Empty(PerformanceAnalysisData data)
        {
            if (data.Cpu is not { Rows.Count: > 0 }) yield return PerformanceSection.CPU;
            if (data.Waits is not { Rows.Count: > 0 }) yield return PerformanceSection.Waits;
            if (data.IODrives is not { Rows.Count: > 0 }) yield return PerformanceSection.IO;
            if (data.Metrics is null || !data.Metrics.Rows.Cast<DataRow>().Any(r => !PerformanceAnalysisPayload.IsInternalCounter(r))) yield return PerformanceSection.Metrics;
            if (data.Memory is not { Rows.Count: > 0 }) yield return PerformanceSection.Memory;
            if (data.ObjectExecution is null || !data.ObjectExecution.Rows.Cast<DataRow>().Any(r => !PerformanceAnalysisPayload.IsIdleWait(r))) yield return PerformanceSection.ObjectExecution;
            if (NotCollected(data.Latches)) yield return PerformanceSection.Latches;
            if (NotCollected(data.Spinlocks)) yield return PerformanceSection.Spinlocks;
            if (data.RunningQueries is not { Rows.Count: > 0 }) yield return PerformanceSection.RunningQueries;
            if (NotCollected(data.Deadlocks)) yield return PerformanceSection.Deadlocks;
        }

        private static bool NotCollected(DataSet set) =>
            set is null || set.Tables.Count == 0 || set.Tables[0].Columns.Contains("Message");
    }
}
