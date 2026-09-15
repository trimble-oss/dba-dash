using DBADash.Deadlock.Analysis;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashAI.Services
{
    /// <summary>
    /// Records what the model said about a deadlock, so the next person to open that deadlock - or another
    /// occurrence of its pattern - sees the analysis without paying for it again.  Every answer is kept.
    ///
    /// This writes only.  It started as a read-through cache, which stopped making sense once the
    /// viewer began showing a pattern's previous analysis automatically: by the time somebody presses
    /// the button they have already read the stored answer and are asking for another opinion.
    /// Returning the text they are looking at would have been the one thing they did not want - and
    /// two runs of the same model on the same graph do not say the same thing anyway.
    ///
    /// Writes are best effort.  A record that cannot be written costs the next reader an analysis,
    /// which is not worth failing the one this caller is waiting for.
    /// </summary>
    public class DeadlockAnalysisStore
    {
        private readonly string? _connectionString;
        private readonly ILogger<DeadlockAnalysisStore> _logger;
        private readonly int _timeoutSeconds;

        public DeadlockAnalysisStore(IConfiguration configuration, ILogger<DeadlockAnalysisStore> logger)
        {
            _connectionString = configuration.GetConnectionString("Repository");
            _timeoutSeconds = configuration.GetValue<int?>("AI:SqlTimeoutSeconds") ?? 30;
            _logger = logger;
        }

        /// <summary>True when there is a repository to record in - local mode has none.</summary>
        public bool IsAvailable => !string.IsNullOrWhiteSpace(_connectionString);

        public async Task SaveAsync(
            string? signature,
            string model,
            string payloadVersion,
            string analysis,
            int? instanceId,
            byte? signatureVersion,
            string? graphXml,
            CancellationToken cancellationToken)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(analysis)) return;

            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await using var command = new SqlCommand("AI.DeadlockAnalysis_Upd", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = _timeoutSeconds
                };

                command.Parameters.AddWithValue("@Signature", signature);
                command.Parameters.AddWithValue("@Model", model);
                command.Parameters.AddWithValue("@PayloadVersion", payloadVersion);
                command.Parameters.AddWithValue("@Analysis", analysis);
                command.Parameters.AddWithValue("@InstanceID", (object?)instanceId ?? DBNull.Value);
                command.Parameters.AddWithValue("@SignatureVersion", (object?)signatureVersion ?? DBNull.Value);
                // The graph the answer was produced from, so its signature can be recomputed when the version changes.
                command.Parameters.Add("@GraphXml", SqlDbType.NVarChar, -1).Value = (object?)graphXml ?? DBNull.Value;
                // Hashed here rather than taken from the caller, so every stored answer gets one.  The viewer sends the
                // graph as the parser re-serialised it, which is what the collector hashes too.
                command.Parameters.Add("@DeadlockHash", SqlDbType.Binary, DeadlockHash.Bytes).Value =
                    string.IsNullOrWhiteSpace(graphXml) ? DBNull.Value : (object)DeadlockHash.Compute(graphXml);

                await connection.OpenAsync(cancellationToken);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record the deadlock analysis for {signature}", signature);
            }
        }
    }
}
