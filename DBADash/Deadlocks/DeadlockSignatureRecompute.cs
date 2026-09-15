using DBADash.Deadlock;
using DBADash.Deadlock.Analysis;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Deadlocks
{
    /// <summary>
    /// Brings the signatures stored in the repository up to the current <see cref="DeadlockSignature.VersionNumber"/>.
    ///
    /// <para>A signature is computed from the parsed graph, so a change to what goes into one leaves every stored
    /// deadlock grouped the old way.  This re-reads the stored graphs a batch at a time and writes the signature the
    /// current version gives them - see dbo.DeadlockSignatureRecompute_Get and dbo.DeadlockSignatureRecompute_Upd.
    /// Deadlocks whose graph has aged out of the repository can't be recomputed and keep their old signature.</para>
    ///
    /// <para>AI analyses are keyed on the signature too, and are recomputed from the graph each one was produced
    /// from - see AI.DeadlockAnalysisSignatureRecompute_Upd.  Analyses stored without a graph keep their signature.</para>
    /// </summary>
    public static class DeadlockSignatureRecompute
    {
        public sealed class BatchResult
        {
            /// <summary>Rows read in the batch.</summary>
            public int Read { get; init; }

            /// <summary>Rows whose graph didn't parse, and so keep the signature they had.</summary>
            public int Failed { get; init; }

            /// <summary>
            /// True when the batch came back short - there is nothing left to do.  No position is carried between
            /// batches: every row read is written back, so it isn't returned again.
            /// </summary>
            public bool IsEnd { get; init; }
        }

        private const int CommandTimeout = 60;

        /// <summary>
        /// Recomputes the signatures of up to <paramref name="batchSize"/> deadlocks stored under an older signature
        /// version.  Every row read is written back - one whose graph doesn't parse keeps its signature but takes the new
        /// version - so the next batch reads the rows that are left.
        /// </summary>
        public static async Task<BatchResult> RecomputeBatchAsync(string connectionString, int batchSize,
            CancellationToken cancellationToken)
        {
            var signatures = new DataTable("DeadlockSignatures");
            // Mirrors the dbo.DeadlockSignatures table type.  Bound by ordinal, so column order matters.
            signatures.Columns.Add("InstanceID", typeof(int));
            signatures.Columns.Add("EventTime", typeof(DateTime));
            signatures.Columns.Add("DeadlockHash", typeof(byte[]));
            signatures.Columns.Add("Signature", typeof(string));
            signatures.Columns.Add("SignatureVersion", typeof(byte));

            var read = 0;
            var failed = 0;

            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(cancellationToken);

            await using (var cmd = new SqlCommand("dbo.DeadlockSignatureRecompute_Get", cn)
                         { CommandType = CommandType.StoredProcedure, CommandTimeout = CommandTimeout })
            {
                cmd.Parameters.AddWithValue("@SignatureVersion", (byte)DeadlockSignature.VersionNumber);
                cmd.Parameters.AddWithValue("@BatchSize", batchSize);

                await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    read++;
                    var signature = ComputeSignature(rdr.IsDBNull(3) ? null : rdr.GetString(3));
                    if (signature is null) failed++;

                    // A NULL signature is still sent: the row keeps its signature but is marked as looked at, so it isn't
                    // returned by every batch that follows.
                    signatures.Rows.Add(rdr.GetInt32(0), rdr.GetDateTime(1), (byte[])rdr[2], (object)signature ?? DBNull.Value,
                        (byte)DeadlockSignature.VersionNumber);
                }
            }

            await UpdateAsync(cn, "dbo.DeadlockSignatureRecompute_Upd", signatures, cancellationToken);

            return new BatchResult { Read = read, Failed = failed, IsEnd = read < batchSize };
        }

        /// <summary>
        /// Recomputes the signatures of up to <paramref name="batchSize"/> AI analyses stored under an older signature
        /// version, from the graph each was produced from.  As with deadlocks, every row read is written back.
        /// </summary>
        public static async Task<BatchResult> RecomputeAnalysisBatchAsync(string connectionString, int batchSize,
            CancellationToken cancellationToken)
        {
            var signatures = new DataTable("DeadlockAnalysisSignatures");
            // Mirrors the AI.DeadlockAnalysisSignatures table type.  Bound by ordinal, so column order matters.
            signatures.Columns.Add("DeadlockAnalysisID", typeof(long));
            signatures.Columns.Add("Signature", typeof(string));
            signatures.Columns.Add("SignatureVersion", typeof(byte));

            var read = 0;
            var failed = 0;

            await using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(cancellationToken);

            await using (var cmd = new SqlCommand("AI.DeadlockAnalysisSignatureRecompute_Get", cn)
                         { CommandType = CommandType.StoredProcedure, CommandTimeout = CommandTimeout })
            {
                cmd.Parameters.AddWithValue("@SignatureVersion", (byte)DeadlockSignature.VersionNumber);
                cmd.Parameters.AddWithValue("@BatchSize", batchSize);

                await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    read++;
                    var signature = ComputeSignature(rdr.IsDBNull(1) ? null : rdr.GetString(1));
                    if (signature is null) failed++;

                    // Sent even when NULL, as for deadlocks: the analysis keeps its signature but is marked as looked at.
                    signatures.Rows.Add(rdr.GetInt64(0), (object)signature ?? DBNull.Value, (byte)DeadlockSignature.VersionNumber);
                }
            }

            await UpdateAsync(cn, "AI.DeadlockAnalysisSignatureRecompute_Upd", signatures, cancellationToken);

            return new BatchResult { Read = read, Failed = failed, IsEnd = read < batchSize };
        }

        private static async Task UpdateAsync(SqlConnection cn, string procedure, DataTable signatures,
            CancellationToken cancellationToken)
        {
            if (signatures.Rows.Count == 0) return;

            await using var cmd = new SqlCommand(procedure, cn)
                { CommandType = CommandType.StoredProcedure, CommandTimeout = CommandTimeout };
            cmd.Parameters.AddWithValue("@Signatures", signatures);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Brings the signatures in a collected Deadlocks table (<see cref="DeadlockTables.CreateDeadlocksTable"/>) up to
        /// the current version before it is imported.  A remote agent on an older version collects with its own
        /// signature version, and those rows would otherwise be stored under it - after the background recompute has
        /// finished and stopped looking.  Rows at or above the current version are left alone, as are rows without a
        /// graph or whose graph doesn't parse; the background recompute handles those the next time it runs.
        /// </summary>
        /// <returns>The number of rows whose signature was recomputed.</returns>
        public static int UpgradeCollectedSignatures(DataTable deadlocks)
        {
            if (deadlocks is not { Rows.Count: > 0 } || !deadlocks.Columns.Contains("Signature") ||
                !deadlocks.Columns.Contains("SignatureVersion") || !deadlocks.Columns.Contains("DeadlockXmlCompressed"))
            {
                return 0;
            }

            // One collection comes from one agent, so every row carries the same signature version.  The usual case is
            // an agent on the current version, and one row is enough to tell.
            if (IsCurrent(deadlocks.Rows[0])) return 0;

            var upgraded = 0;
            foreach (DataRow row in deadlocks.Rows)
            {
                if (IsCurrent(row)) continue;
                if (row["DeadlockXmlCompressed"] is not byte[] { Length: > 0 } compressed) continue;

                var signature = ComputeSignature(SMOBaseClass.Unzip(compressed));
                if (signature is null) continue;

                row["Signature"] = signature;
                row["SignatureVersion"] = (byte)DeadlockSignature.VersionNumber;
                upgraded++;
            }

            return upgraded;

            static bool IsCurrent(DataRow row) =>
                row["SignatureVersion"] is not DBNull &&
                Convert.ToInt32(row["SignatureVersion"]) >= DeadlockSignature.VersionNumber;
        }

        /// <summary>
        /// The current version's signature for a stored graph, or null when it doesn't parse.  Computed exactly as the
        /// collector does, from the graph as stored - the collector stores the XML the parser re-serialised.
        /// </summary>
        public static string ComputeSignature(string graphXml)
        {
            if (!DeadlockParser.TryParse(graphXml, out var graphs) || graphs.Count == 0) return null;
            return DeadlockSignature.Compute(graphs.First()).Value;
        }
    }
}
