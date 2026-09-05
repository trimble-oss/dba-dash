using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashAI.Services
{
    /// <summary>
    /// Records what the model said about a deadlock pattern, so the next person to open that deadlock
    /// sees the analysis without paying for it again.
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
