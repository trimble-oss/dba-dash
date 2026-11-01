using System.Data;
using DBADashAI.Mcp;
using Microsoft.Data.SqlClient;

namespace DBADashAI.Services
{
    /// <summary>
    /// Keeps <see cref="AiDataDetailSetting"/> in step with AI.DataDetail in the repository, so a change the GUI saves
    /// reaches the built-in AI within a minute.  It runs in local and shared mode alike, because it needs only the
    /// repository connection, not the service registration.
    ///
    /// The first read happens before the service takes requests, and until one succeeds the setting is metadataOnly, so a
    /// restart never serves a request below the saved minimum.  After that, a read that fails leaves the last value in place;
    /// a repository with no saved setting reads as parametersAndLiterals.
    /// </summary>
    public sealed class AiDataDetailRefreshService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IConfiguration _configuration;
        private readonly AiDataDetailSetting _setting;
        private readonly ILogger<AiDataDetailRefreshService> _logger;
        private readonly int _sqlTimeoutSeconds;

        public AiDataDetailRefreshService(IConfiguration configuration, AiDataDetailSetting setting, ILogger<AiDataDetailRefreshService> logger)
        {
            _configuration = configuration;
            _setting = setting;
            _logger = logger;
            _sqlTimeoutSeconds = configuration.GetValue<int?>("AI:SqlTimeoutSeconds") ?? 30;
        }

        /// <summary>Hosted services start before the server listens, so this read is in place for the first request.</summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_configuration.GetConnectionString("Repository")))
            {
                _setting.Set(AiDataMask.Metadata);
                await RefreshAsync(cancellationToken);
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Interval, stoppingToken);
                await RefreshAsync(stoppingToken);
            }
        }

        /// <summary>Reads the saved setting once.  Public so the tests can call it without waiting a minute.</summary>
        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            var connectionString = _configuration.GetConnectionString("Repository");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return;
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = new SqlCommand("AI.DataDetail_Get", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = _sqlTimeoutSeconds
                };
                var word = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();

                if (!_setting.TrySetFromWord(word))
                {
                    _logger.LogWarning("AI.DataDetail holds '{Word}', which is not a data detail level; keeping {Current}", word, _setting.Value);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not read the built-in AI data detail; keeping {Current}", _setting.Value);
            }
        }
    }
}
