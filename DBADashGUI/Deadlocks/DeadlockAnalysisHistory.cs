#nullable enable
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// Previous AI analyses of a deadlock pattern, read straight from the repository.
    ///
    /// This asks a different question from the service's cache.  The cache decides whether an answer
    /// can be <em>reused</em>, so it matches on model and payload version too; this asks whether
    /// anyone has ever looked at this deadlock, and shows what they found.  An answer from an older
    /// model, or from before object definitions were being sent, is still worth reading - it is
    /// labelled for what it is, and asking again is one button away.
    ///
    /// Reading the repository directly rather than going through the AI service means a pattern's
    /// history is there even when no AI service is configured any more.
    /// </summary>
    internal static class DeadlockAnalysisHistory
    {
        internal sealed record Entry(
            string Analysis,
            string Model,
            string PayloadVersion,
            DateTime GeneratedUtc,
            string? Instance)
        {
            /// <summary>True when the answer was produced without the object definitions we now send.</summary>
            public bool WithoutSchema => !PayloadVersion.Contains("schema", StringComparison.OrdinalIgnoreCase);
        }

        internal static async Task<IReadOnlyList<Entry>> FetchAsync(
            string? signature,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(Common.ConnectionString))
            {
                return Array.Empty<Entry>();
            }

            try
            {
                var entries = new List<Entry>();

                await using var connection = new SqlConnection(Common.ConnectionString);
                await using var command = new SqlCommand("AI.DeadlockAnalysisHistory_Get", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };

                command.Parameters.AddWithValue("Signature", signature);

                await connection.OpenAsync(cancellationToken);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    entries.Add(new Entry(
                        (string)reader["Analysis"],
                        (string)reader["Model"],
                        (string)reader["PayloadVersion"],
                        (DateTime)reader["GeneratedUtc"],
                        reader["InstanceDisplayName"] as string));
                }

                return entries;
            }
            catch (Exception ex)
            {
                // An older repository without the table, or a user without the grant: the viewer works
                // the same, it just cannot offer what was found before.
                Log.Debug(ex, "Could not read deadlock analysis history for {signature}", signature);
                return Array.Empty<Entry>();
            }
        }
    }
}
