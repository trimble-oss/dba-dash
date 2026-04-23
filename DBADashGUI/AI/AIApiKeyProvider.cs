#nullable enable
using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Manages AI API authentication. The key is supplied by <see cref="AIServiceDiscovery"/>
    /// (either from local user settings or the repository database) and cached here.
    /// </summary>
    public class AIApiKeyProvider
    {
        private static string? _cachedApiKey;
        private static DateTime _keyRetrievedAt = DateTime.MinValue;
        private static readonly TimeSpan KeyCacheDuration = TimeSpan.FromMinutes(30);
        private static readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// Gets the current API key. Returns the cached key if still valid.
        /// Falls back to a DB lookup for backwards compatibility when no key has been seeded.
        /// </summary>
        public static async Task<string?> GetApiKeyAsync()
        {
            // Return cached key if still valid
            if (_cachedApiKey != null && DateTime.UtcNow - _keyRetrievedAt < KeyCacheDuration)
                return _cachedApiKey;

            await _lock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cachedApiKey != null && DateTime.UtcNow - _keyRetrievedAt < KeyCacheDuration)
                    return _cachedApiKey;

                // Re-run service discovery to pick up the correct key for whichever source is active
                var serviceInfo = await AIServiceDiscovery.GetServiceInfoAsync();
                if (serviceInfo?.ApiKey != null)
                {
                    _cachedApiKey = serviceInfo.ApiKey;
                    _keyRetrievedAt = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine($"AIApiKeyProvider: key resolved via discovery (source: {serviceInfo.Source})");
                    return _cachedApiKey;
                }

                System.Diagnostics.Debug.WriteLine($"AIApiKeyProvider: discovery returned no key (source: {serviceInfo?.Source}) - falling back to direct DB read");

                var dbKey = await ReadKeyDirectlyFromDbAsync();
                if (dbKey != null)
                {
                    _cachedApiKey = dbKey;
                    _keyRetrievedAt = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine("AIApiKeyProvider: key resolved via direct DB fallback");
                    return _cachedApiKey;
                }

                System.Diagnostics.Debug.WriteLine("AIApiKeyProvider: no API key available from any source");
                return _cachedApiKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AIApiKeyProvider: error refreshing key: {ex.Message}");
                return _cachedApiKey;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Direct fallback: reads the API key straight from the repository DB without
        /// going through service discovery.  Used when local-mode discovery succeeds
        /// (health endpoint reachable) but the key merge step fails for any reason.
        /// </summary>
        private static async Task<string?> ReadKeyDirectlyFromDbAsync()
        {
            try
            {
                var connectionString = Common.ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                    return null;

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("AI.ServiceConfig_Get", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandTimeout = 5
                };

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var key = reader["ApiKey"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(key))
                        return key;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AIApiKeyProvider: direct DB key read failed: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Seeds the cached key from a freshly-resolved <see cref="AIServiceDiscovery.ServiceInfo"/>.
        /// Call this after discovery so subsequent <see cref="GetApiKeyAsync"/> calls avoid extra lookups.
        /// </summary>
        public static void SeedFromServiceInfo(AIServiceDiscovery.ServiceInfo info)
        {
            if (info.ApiKey == null) return; // Don't cache null - let GetApiKeyAsync fall back to DB
            _lock.Wait();
            try
            {
                _cachedApiKey = info.ApiKey;
                _keyRetrievedAt = DateTime.UtcNow;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Invalidates the cached key, forcing retrieval from the active source on next request.
        /// Call this when receiving 401 Unauthorized from the API.
        /// </summary>
        public static void InvalidateCache()
        {
            _lock.Wait();
            try
            {
                _cachedApiKey = null;
                _keyRetrievedAt = DateTime.MinValue;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Manually sets the API key (for testing or fallback scenarios).
        /// </summary>
        public static void SetApiKey(string apiKey)
        {
            _lock.Wait();
            try
            {
                _cachedApiKey = apiKey;
                _keyRetrievedAt = DateTime.UtcNow;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
