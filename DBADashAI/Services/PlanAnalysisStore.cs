using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADashAI.Services
{
    /// <summary>
    /// Records what the model said about a query plan, so the next person to open that plan - or the
    /// same query with a different plan - sees the analysis without paying for it again.  Every answer
    /// is kept, and a follow-up is stored alongside the answer it followed.
    ///
    /// The query plan counterpart of <see cref="DeadlockAnalysisStore"/>, and write-only for the same
    /// reason: by the time somebody presses the button they have already read the stored answer the
    /// viewer showed them, and are asking for another opinion.
    ///
    /// Writes are best effort.  A record that cannot be written costs the next reader an analysis,
    /// which is not worth failing the one this caller is waiting for.
    /// </summary>
    public class PlanAnalysisStore
    {
        private readonly string? _connectionString;
        private readonly ILogger<PlanAnalysisStore> _logger;
        private readonly int _timeoutSeconds;

        public PlanAnalysisStore(IConfiguration configuration, ILogger<PlanAnalysisStore> logger)
        {
            _connectionString = configuration.GetConnectionString("Repository");
            _timeoutSeconds = configuration.GetValue<int?>("AI:SqlTimeoutSeconds") ?? 30;
            _logger = logger;
        }

        /// <summary>True when there is a repository to record in - local mode has none.</summary>
        public bool IsAvailable => !string.IsNullOrWhiteSpace(_connectionString);

        public async Task SaveAsync(
            string? signature,
            string? planHash,
            string model,
            string payloadVersion,
            string analysis,
            int? instanceId,
            string? statementText,
            Guid conversationId,
            int turnNumber,
            string? question,
            CancellationToken cancellationToken)
        {
            // Without an identity there is nothing to find the answer by again, which is the only
            // reason to store it.
            if (!IsAvailable || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(analysis)) return;

            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await using var command = new SqlCommand("AI.QueryPlanAnalysis_Upd", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = _timeoutSeconds
                };

                command.Parameters.AddWithValue("@Signature", signature);
                command.Parameters.AddWithValue("@PlanHash", (object?)planHash ?? DBNull.Value);
                command.Parameters.AddWithValue("@Model", model);
                command.Parameters.AddWithValue("@PayloadVersion", payloadVersion);
                command.Parameters.AddWithValue("@Analysis", analysis);
                command.Parameters.AddWithValue("@InstanceID", (object?)instanceId ?? DBNull.Value);
                // Kept so the drop-down can say which query an answer is about.  A hash identifies a
                // statement but does not describe it, and a list of hashes is a list of nothing.
                command.Parameters.Add("@StatementText", SqlDbType.NVarChar, -1).Value =
                    string.IsNullOrWhiteSpace(statementText) ? DBNull.Value : statementText;
                command.Parameters.AddWithValue("@ConversationID", conversationId);
                command.Parameters.AddWithValue("@TurnNumber", turnNumber);
                // Null on the opening turn: the question there is the plan itself.
                command.Parameters.Add("@Question", SqlDbType.NVarChar, -1).Value =
                    string.IsNullOrWhiteSpace(question) ? DBNull.Value : question;

                await connection.OpenAsync(cancellationToken);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record the query plan analysis for {signature}", signature);
            }
        }
    }
}
