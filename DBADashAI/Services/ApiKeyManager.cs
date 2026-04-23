using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace DBADashAI.Services;

public class ApiKeyManager
{
    private const string CacheKey = "ApiKeyManager_ValidKeys";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiKeyManager> _logger;
    private readonly int _sqlTimeoutSeconds;

    public ApiKeyManager(IConfiguration configuration, IMemoryCache cache, ILogger<ApiKeyManager> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        _sqlTimeoutSeconds = configuration.GetValue<int?>("AI:SqlTimeoutSeconds") ?? 30;
    }

    /// <summary>
    /// Gets the current API key from AIServiceConfig. Returns only the active key (not previous).
    /// Used by GUI clients that should always send the latest key.
    /// </summary>
    public async Task<string?> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetServiceConfigFromDbAsync(cancellationToken);
        if (config is not null && !string.IsNullOrWhiteSpace(config.ApiKey))
        {
            _logger.LogDebug("Retrieved API key from AIServiceConfig");
            return config.ApiKey;
        }

        _logger.LogWarning("No API key found in AIServiceConfig - service may not have initialized yet");
        return null;
    }

    /// <summary>
    /// Retrieves all valid API keys for authentication validation (server-side).
    /// Includes the current key and previous key (if within grace period) loaded
    /// from the repository database.
    /// Results are cached for <see cref="CacheTtl"/> to avoid a DB round-trip on
    /// every authenticated request.
    /// </summary>
    public async Task<string[]> GetAllValidKeysAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out string[]? cached) && cached is not null)
            return cached;

        var keys = new List<string>();

        // Primary key and previous key (grace period) from database
        try
        {
            var config = await GetServiceConfigFromDbAsync(cancellationToken);
            if (config is not null)
            {
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                    keys.Add(config.ApiKey);

                // Accept previous key if within grace period
                if (!string.IsNullOrWhiteSpace(config.PreviousApiKey)
                    && config.PreviousApiKeyExpiryUtc.HasValue
                    && DateTime.UtcNow < config.PreviousApiKeyExpiryUtc.Value)
                {
                    keys.Add(config.PreviousApiKey);
                    _logger.LogDebug("Previous API key still valid until {Expiry}", config.PreviousApiKeyExpiryUtc.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve API key from AIServiceConfig");
        }

        var result = keys.Distinct().ToArray();
        if (result.Length > 0)
        {
            _cache.Set(CacheKey, result, CacheTtl);
        }
        else
        {
            // Cache empty results with a shorter TTL to prevent a storm of DB
            // connections when the database is unreachable or the key hasn't been
            // written yet. Requests will still fail auth, but the DB won't be
            // hammered on every inbound request.
            _cache.Set(CacheKey, result, TimeSpan.FromSeconds(5));
        }
        return result;
    }

    /// <summary>
    /// Returns the UTC date/time when the current API key rotation is due,
    /// based on ApiKeyCreatedDate + rotationDays. Returns DateTime.MinValue if
    /// the key age cannot be determined (triggers immediate check).
    /// </summary>
    public async Task<DateTime> GetNextRotationDueAsync(int rotationDays, CancellationToken cancellationToken = default)
    {
        var config = await GetServiceConfigFromDbAsync(cancellationToken);

        if (config?.ApiKeyCreatedDate == null)
            return DateTime.MaxValue;

        return config.ApiKeyCreatedDate.Value.AddDays(rotationDays);
    }

    /// <summary>
    /// Regenerates the API key in AIServiceConfig. The old key is preserved with a grace period.
    /// </summary>
    public async Task<string> RegenerateApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("Repository");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Repository connection string not configured.");
        }

        var gracePeriodHours = _configuration.GetValue<int?>("Security:KeyGracePeriodHours") ?? 24;
        var newKey = GenerateApiKey();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get current config
        var currentConfig = await GetServiceConfigAsync(connection, cancellationToken);
        if (currentConfig == null)
        {
            throw new InvalidOperationException("AIServiceConfig not initialized - service must start at least once");
        }

        // Update with new key (stored proc handles preserving old key with grace period)
        await using var cmd = new SqlCommand("AI.ServiceConfig_Upd", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = _sqlTimeoutSeconds
        };
        cmd.Parameters.AddWithValue("@ServiceUrl", currentConfig.ServiceUrl);
        cmd.Parameters.AddWithValue("@ApiKey", newKey);
        cmd.Parameters.AddWithValue("@IsEnabled", currentConfig.IsEnabled);
        cmd.Parameters.AddWithValue("@ServiceVersion", (object?)currentConfig.ServiceVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@KeyGracePeriodHours", gracePeriodHours);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Evict the cache so the new key takes effect on the next request
        _cache.Remove(CacheKey);

        _logger.LogWarning("API key rotated - old key valid for {GracePeriodHours} hours", gracePeriodHours);
        return newKey;
    }

    private async Task<ServiceConfig?> GetServiceConfigFromDbAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("Repository");
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return await GetServiceConfigAsync(connection, cancellationToken);
    }

    private async Task<ServiceConfig?> GetServiceConfigAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("AI.ServiceConfig_Get", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure,
            CommandTimeout = _sqlTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var config = new ServiceConfig
            {
                ServiceUrl = reader["ServiceUrl"]?.ToString() ?? "",
                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                ServiceVersion = reader["ServiceVersion"]?.ToString(),
                ApiKey = reader["ApiKey"]?.ToString(),
                ApiKeyCreatedDate = reader["ApiKeyCreatedDate"] as DateTime?
            };

            // Read PreviousApiKey columns safely (may not exist in older DB schemas)
            try
            {
                var ordinal = reader.GetOrdinal("PreviousApiKey");
                config.PreviousApiKey = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException) { }

            try
            {
                var ordinal = reader.GetOrdinal("PreviousApiKeyExpiryUtc");
                config.PreviousApiKeyExpiryUtc = reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch (IndexOutOfRangeException) { }

            return config;
        }

        return null;
    }

    private class ServiceConfig
    {
        public string ServiceUrl { get; set; } = "";
        public bool IsEnabled { get; set; }
        public string? ServiceVersion { get; set; }
        public string? ApiKey { get; set; }
        public DateTime? ApiKeyCreatedDate { get; set; }
        public string? PreviousApiKey { get; set; }
        public DateTime? PreviousApiKeyExpiryUtc { get; set; }
    }

    internal static string GenerateApiKey()
    {
        // Generate a cryptographically secure 48-character API key.
        // Rejection sampling eliminates modulo bias: bytes >= threshold are discarded
        // so every character in the alphabet is equally likely.
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const int length = 48;
        // Largest multiple of chars.Length that fits in a byte (256).
        // Bytes in [threshold, 255] are rejected to remove bias.
        int threshold = 256 - (256 % chars.Length);

        var result = new char[length];
        var filled = 0;
        while (filled < length)
        {
            // Oversample: on average ~1.03 bytes needed per character.
            var bytes = new byte[(length - filled) * 2];
            RandomNumberGenerator.Fill(bytes);
            foreach (var b in bytes)
            {
                if (b >= threshold) continue;
                result[filled++] = chars[b % chars.Length];
                if (filled == length) break;
            }
        }

        return new string(result);
    }
}
