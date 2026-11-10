using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// The server-side work a collection run caused on the monitored instance - CPU and logical reads, added up over the
    /// connections it measured (<see cref="SessionUsageMeasurement"/>).  Null until a connection has been measured, so a
    /// collection that doesn't go through a measured connection (WMI, SMO scripting) reports nothing rather than zero.
    /// </summary>
    public sealed class CollectionRunMeter
    {
        private long cpuMs;
        private long logicalReads;
        private int measuredCount;

        public void Add(long cpu, long reads)
        {
            Interlocked.Add(ref cpuMs, cpu);
            Interlocked.Add(ref logicalReads, reads);
            Interlocked.Increment(ref measuredCount);
        }

        public long? CpuMs => Volatile.Read(ref measuredCount) > 0 ? Interlocked.Read(ref cpuMs) : null;

        public long? LogicalReads => Volatile.Read(ref measuredCount) > 0 ? Interlocked.Read(ref logicalReads) : null;
    }

    /// <summary>
    /// Measures the CPU and logical reads used on one open connection between <see cref="StartAsync"/> and dispose, from the
    /// session's own counters in sys.dm_exec_sessions - a session can always see its own row, so no extra permission is
    /// needed.  Two small queries per connection.  Only made when there is a meter to add to (LogInternalPerformanceCounters),
    /// otherwise no query is run at all.
    /// <para>Declare it straight after opening the connection and before any reader, so it's disposed after the reader is
    /// closed - the end reading needs the connection free.  A reading that fails (a connection broken by the collection's own
    /// error, say) is skipped rather than failing the collection.</para>
    /// </summary>
    public sealed class SessionUsageMeasurement : IDisposable, IAsyncDisposable
    {
        private const string UsageSQL = "SELECT CAST(cpu_time AS BIGINT), logical_reads FROM sys.dm_exec_sessions WHERE session_id = @@SPID";
        private const int UsageCommandTimeout = 10;

        private readonly SqlConnection connection;
        private readonly CollectionRunMeter meter;
        private readonly (long Cpu, long Reads) start;
        private bool disposed;

        private SessionUsageMeasurement(SqlConnection connection, CollectionRunMeter meter, (long Cpu, long Reads) start)
        {
            this.connection = connection;
            this.meter = meter;
            this.start = start;
        }

        public static async Task<SessionUsageMeasurement> StartAsync(SqlConnection connection, CollectionRunMeter meter)
        {
            if (meter == null || connection.State != ConnectionState.Open) return null;
            var usage = await ReadAsync(connection);
            return usage == null ? null : new SessionUsageMeasurement(connection, meter, usage.Value);
        }

        private static readonly AsyncLocal<CollectionRunMeter> RunMeter = new();

        /// <summary>
        /// Makes <paramref name="meter"/> the meter for the collection run in progress, for code that opens its own connections
        /// from a connection string rather than being given a meter - the slow query and deadlock collectors and the XE readers
        /// they share - until the result is disposed.  Flows with the async calls the run makes, and only those, so the same
        /// code run for anything else (the GUI's XE viewer, say) measures nothing.
        /// </summary>
        public static IDisposable ForRun(CollectionRunMeter meter)
        {
            var previous = RunMeter.Value;
            RunMeter.Value = meter;
            return new RestoreRunMeter(previous);
        }

        private sealed class RestoreRunMeter(CollectionRunMeter previous) : IDisposable
        {
            public void Dispose() => RunMeter.Value = previous;
        }

        /// <summary>Measures <paramref name="connection"/> for the collection run in progress, if any - see <see cref="ForRun"/></summary>
        public static Task<SessionUsageMeasurement> StartForRunAsync(SqlConnection connection) => StartAsync(connection, RunMeter.Value);

        public static SessionUsageMeasurement Start(SqlConnection connection, CollectionRunMeter meter)
        {
            if (meter == null || connection.State != ConnectionState.Open) return null;
            var usage = Read(connection);
            return usage == null ? null : new SessionUsageMeasurement(connection, meter, usage.Value);
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            disposed = true;
            if (connection.State != ConnectionState.Open) return;
            Record(await ReadAsync(connection));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (connection.State != ConnectionState.Open) return;
            Record(Read(connection));
        }

        private void Record((long Cpu, long Reads)? end)
        {
            if (end == null) return;
            var cpu = end.Value.Cpu - start.Cpu;
            var reads = end.Value.Reads - start.Reads;
            if (cpu < 0 || reads < 0) return; // The session was reset under us - nothing to say about this connection
            meter.Add(cpu, reads);
        }

        private static async Task<(long Cpu, long Reads)?> ReadAsync(SqlConnection connection)
        {
            try
            {
                await using var cmd = new SqlCommand(UsageSQL, connection) { CommandTimeout = UsageCommandTimeout };
                await using var rdr = await cmd.ExecuteReaderAsync();
                return await rdr.ReadAsync() ? (rdr.GetInt64(0), rdr.GetInt64(1)) : null;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to read session CPU and reads for collection overhead");
                return null;
            }
        }

        private static (long Cpu, long Reads)? Read(SqlConnection connection)
        {
            try
            {
                using var cmd = new SqlCommand(UsageSQL, connection) { CommandTimeout = UsageCommandTimeout };
                using var rdr = cmd.ExecuteReader();
                return rdr.Read() ? (rdr.GetInt64(0), rdr.GetInt64(1)) : null;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to read session CPU and reads for collection overhead");
                return null;
            }
        }
    }
}
