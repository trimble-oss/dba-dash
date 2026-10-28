using DBADash;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashAI.Services;

/// <summary>
/// Background service that registers this AI service in the repository database
/// and sends periodic heartbeats so GUI clients can discover it.
/// </summary>
public class ServiceRegistrationBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceRegistrationBackgroundService> _logger;
    private readonly ApiKeyManager _apiKeyManager;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(1);
    private readonly int _sqlTimeoutSeconds;
    private DateTime _nextRotationDue = DateTime.MinValue;

    public ServiceRegistrationBackgroundService(
        IConfiguration configuration,
        ILogger<ServiceRegistrationBackgroundService> logger,
        ApiKeyManager apiKeyManager)
    {
        _configuration = configuration;
        _logger = logger;
        _apiKeyManager = apiKeyManager;
        _sqlTimeoutSeconds = configuration.GetValue<int?>("AI:SqlTimeoutSeconds") ?? 30;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Mode is determined by Registration:ServiceUrl.
        // If set  → shared/repository mode: register in DB, send heartbeats.
        // If empty → local mode: skip registration entirely (loopback only, no DB required).
        var serviceUrl = _configuration["Registration:ServiceUrl"];
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            _logger.LogInformation("AI service running in local mode - skipping repository registration");
            return;
        }

        // Wait a bit for the service to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Register on startup
        var registered = await RegisterServiceAsync(stoppingToken);

        // Send periodic heartbeats. Registration is retried until it succeeds, as the repository
        // database might not be available yet or might still be waiting to be upgraded.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, stoppingToken);
                if (!registered)
                {
                    registered = await RegisterServiceAsync(stoppingToken);
                    continue;
                }
                await SendHeartbeatAsync(stoppingToken);
                await RotateKeyIfDueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send AI service heartbeat");
            }
        }
    }

    /// <summary>
    /// Registers the service in the repository database.  Returns false if registration should be retried.
    /// </summary>
    private async Task<bool> RegisterServiceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Repository");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Repository connection string not configured - AI service will not be discoverable");
                return true; // Retrying won't help
            }

            var serviceUrl = NormalizeServiceUrl(_configuration["Registration:ServiceUrl"]!);
            var version = GetServiceVersion();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Retrieve existing API key so we don't rotate it on every restart
            string? apiKey = null;
            await using (var getCmd = new SqlCommand("AI.ServiceConfig_Get", connection)
                         { CommandType = System.Data.CommandType.StoredProcedure, CommandTimeout = _sqlTimeoutSeconds })
            await using (var reader = await getCmd.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                    apiKey = reader["ApiKey"]?.ToString();
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = ApiKeyManager.GenerateApiKey();
                _logger.LogInformation("Generated new API key - first service startup");
            }
            else
            {
                _logger.LogDebug("Using existing API key from database");
            }

            // Upsert registration and set initial heartbeat
            await using var command = new SqlCommand("AI.ServiceConfig_Upd", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = _sqlTimeoutSeconds
            };
            var thumbprint = _configuration[AiHttpsCertificate.ActiveThumbprintConfigurationKey];
            command.Parameters.AddWithValue("@ServiceUrl", serviceUrl);
            command.Parameters.AddWithValue("@ApiKey", apiKey);
            command.Parameters.AddWithValue("@ServiceVersion", version ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CertificateThumbprint", string.IsNullOrWhiteSpace(thumbprint) ? DBNull.Value : thumbprint);
            await command.ExecuteNonQueryAsync(cancellationToken);

            // Record initial heartbeat so IsActive is true immediately after registration
            await SendHeartbeatAsync(cancellationToken);

            _logger.LogInformation("AI service registered: URL={ServiceUrl}, Version={Version}", serviceUrl, version);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SqlException ex) when (ex.Number is 2812 or 8144 or 8145)
        {
            // Could not find stored procedure (2812) or parameter mismatch (8144/8145).
            _logger.LogWarning("Failed to register AI service in repository database. The repository database might not be upgraded yet - will retry in {Interval}. {Message}", _heartbeatInterval, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register AI service in repository database - will retry in {Interval}", _heartbeatInterval);
            return false;
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Repository");
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            var version = GetServiceVersion();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("AI.ServiceConfig_Heartbeat", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandTimeout = _sqlTimeoutSeconds;
            command.Parameters.AddWithValue("@ServiceVersion", version ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("AI service heartbeat sent");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send AI service heartbeat");
        }
    }

    private async Task RotateKeyIfDueAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow < _nextRotationDue)
            return;

        try
        {
            var rotationDays = _configuration.GetValue<int?>("Security:KeyRotationDays") ?? 30;
            if (rotationDays <= 0)
            {
                _nextRotationDue = DateTime.MaxValue; // Rotation disabled
                return;
            }

            var nextDue = await _apiKeyManager.GetNextRotationDueAsync(rotationDays, cancellationToken);
            _nextRotationDue = nextDue;

            if (DateTime.UtcNow >= _nextRotationDue)
            {
                _logger.LogInformation("API key is older than {RotationDays} days - rotating", rotationDays);
                await _apiKeyManager.RegenerateApiKeyAsync(cancellationToken);
                // Set next rotation date based on newly created key
                _nextRotationDue = DateTime.UtcNow.AddDays(rotationDays);
            }
        }
        catch (Exception ex)
        {
            // Retry in 1 hour on failure
            _nextRotationDue = DateTime.UtcNow.AddHours(1);
            _logger.LogWarning(ex, "API key rotation check failed - will retry in 1 hour");
        }
    }

    private static string? GetServiceVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Ensures the service URL stored in the database includes an explicit port number.
    /// If the configured URL has no port (e.g. "http://lab2022"), the port from
    /// Registration:Port (default 5055) is appended so GUI clients can connect correctly.
    /// URLs that already include a port are returned unchanged.
    /// </summary>
    private string NormalizeServiceUrl(string serviceUrl)
    {
        try
        {
            var port = _configuration.GetValue<int?>("Registration:Port") ?? 5055;
            return NormalizeServiceUrl(serviceUrl, port, AiHttpsHosting.ReadSchemeOverride(_configuration));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not normalize service URL '{ServiceUrl}' - using as-is", serviceUrl);
            return serviceUrl;
        }
    }

    internal static string NormalizeServiceUrl(string serviceUrl, int port, bool? useHttps = null)
    {
        // UriBuilder handles malformed strings gracefully during instantiation
        var builder = new UriBuilder(serviceUrl);

        // Check if the original string explicitly contains the port.
        // We use uri.Host to skip the scheme, and check if the remainder contains ":port"
        string portString = $":{builder.Port}";
        bool hasExplicitPort = serviceUrl.Contains(portString, StringComparison.OrdinalIgnoreCase);
        var explicitPort = hasExplicitPort ? builder.Port : port;

        // Changing the scheme resets a default port (80 or 443). Put the service port back afterwards.
        if (useHttps.HasValue)
            builder.Scheme = useHttps.Value ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
        builder.Port = explicitPort;

        // UriBuilder.ToString() automatically handles trailing slashes or formatting cleanly
        return builder.ToString().TrimEnd('/');
    }
}
