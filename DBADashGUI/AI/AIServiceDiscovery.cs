#nullable enable
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Discovers the AI service URL and API key using a two-tier priority chain:
    ///   1. Local service on localhost (user-scoped, not registered in the DB)
    ///   2. Shared service registered in the repository database (admin-managed URL)
    /// </summary>
    public class AIServiceDiscovery
    {
        public enum ServiceSource
        {
            None,
            Local,
            Repository
        }

        public class ServiceInfo
        {
            public string? ServiceUrl { get; set; }
            public string? ApiKey { get; set; }
            public bool IsEnabled { get; set; }
            public bool IsActive { get; set; }
            public DateTime? LastHeartbeat { get; set; }
            public string? ServiceVersion { get; set; }
            public ServiceSource Source { get; set; }
            /// <summary>
            /// True when the local service's repository fingerprint doesn't match
            /// the GUI's current repository connection.
            /// </summary>
            public bool RepositoryMismatch { get; set; }
            /// <summary>
            /// True when the calling DB user is a member of AIUser, AIService,
            /// db_owner, or sysadmin — i.e. they received the real ApiKey value
            /// rather than the DDM-masked placeholder.  Always true for the local
            /// service source (no DB auth check needed for loopback mode).
            /// </summary>
            public bool HasApiKeyAccess { get; set; }
        }

        private static readonly HttpClient _probeClient = new() { Timeout = TimeSpan.FromSeconds(3) };

        /// <summary>
        /// Default loopback URL probed when no local service URL is configured in user
        /// settings. Matches the AI service's default listening port (Registration:Port).
        /// </summary>
        private const string DefaultLocalServiceUrl = "http://localhost:5055";

        /// <summary>
        /// Resolves the AI service using the priority chain: local → repository DB.
        /// Returns null if no service is available.
        /// </summary>
        public static async Task<ServiceInfo?> GetServiceInfoAsync()
        {
            // Priority 1: local service on localhost. Use the URL from user settings if
            // configured, otherwise fall back to the default loopback URL. In local mode
            // the service is not registered in the DB, so this probe is the only way to
            // discover it - skipping it when the (user-scoped, empty-by-default) setting
            // is blank is what hid the AI tab for local-mode installs (issue #2000).
            var localUrl = Properties.Settings.Default.LocalAIServiceUrl?.Trim();
            if (string.IsNullOrWhiteSpace(localUrl))
                localUrl = DefaultLocalServiceUrl;

            var localInfo = await TryGetLocalServiceAsync(localUrl);
            if (localInfo != null)
            {
                // If the local service has no API key yet, try to pull it from the
                // repository DB. This handles the case where the service runs on the
                // same machine but still requires authentication (ApiKey scheme).
                if (localInfo.ApiKey == null)
                    localInfo = await MergeRepositoryKeyAsync(localInfo);

                return localInfo;
            }

            // Priority 2: shared service registered in the repository database
            return await TryGetRepositoryServiceAsync();
        }

        /// <summary>
        /// Probes the local AI service at the given URL and reads the repository
        /// fingerprint from the health response to detect mismatches. Local mode is
        /// unauthenticated — the loopback binding is the security boundary.
        /// </summary>
        private static async Task<ServiceInfo?> TryGetLocalServiceAsync(string localUrl)
        {
            try
            {
                var healthUrl = $"{localUrl.TrimEnd('/')}/api/ai/health";
                using var response = await _probeClient.GetAsync(healthUrl);

                if (!response.IsSuccessStatusCode)
                    return null;

                // Parse repository fingerprint from health response
                string? serviceFingerprint = null;
                try
                {
                    var body = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("repositoryFingerprint", out var fp))
                        serviceFingerprint = fp.GetString();
                }
                catch { }

                // Fingerprint of the repository the GUI is connected to, for comparison
                // against the fingerprint the service reports from its health endpoint.
                var guiFingerprint = DBADash.RepositoryFingerprint.GetFingerprint(Common.ConnectionString);

                // Mismatch: both sides have a fingerprint but they differ
                var mismatch = serviceFingerprint != null
                               && guiFingerprint != null
                               && !string.Equals(serviceFingerprint, guiFingerprint, StringComparison.OrdinalIgnoreCase);

                return new ServiceInfo
                {
                    ServiceUrl = localUrl,
                    ApiKey = null,  // Local mode is unauthenticated; key merged below if auth is required
                    IsEnabled = true,
                    IsActive = true,
                    Source = ServiceSource.Local,
                    RepositoryMismatch = mismatch,
                    HasApiKeyAccess = true  // No DB auth check needed for loopback
                };
            }
            catch
            {
                // Not reachable - fall through to repository lookup
                return null;
            }
        }

        /// <summary>
        /// If a local service was found but has no API key, try to fetch the key from
        /// the repository DB.  This covers the case where the service runs on the same
        /// machine as the GUI but still requires authentication (e.g. ApiKey
        /// scheme).  The local URL is kept; only the key is merged.
        /// </summary>
        private static async Task<ServiceInfo> MergeRepositoryKeyAsync(ServiceInfo localInfo)
        {
            try
            {
                var repoInfo = await TryGetRepositoryServiceAsync();
                if (repoInfo?.ApiKey != null && !string.IsNullOrWhiteSpace(repoInfo.ApiKey))
                {
                    localInfo.ApiKey = repoInfo.ApiKey;
                    localInfo.HasApiKeyAccess = repoInfo.HasApiKeyAccess;

                    // Successfully retrieved the key from the current repository DB, which
                    // proves this local service IS registered against the same repository
                    // the GUI is connected to. That is stronger positive proof than the
                    // fingerprint comparison, so clear any reported mismatch.
                    localInfo.RepositoryMismatch = false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AIServiceDiscovery: local service found but no API key in repository DB - service may not have registered yet");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AIServiceDiscovery: failed to merge repository API key: {ex.Message}");
            }
            return localInfo;
        }

        /// <summary>
        /// Gets the AI service configuration from the repository database.
        /// The ServiceUrl in the DB is admin-managed and is not self-reported by the service on restart.
        /// Returns null if no AI service is configured for this repository.
        /// </summary>
        private static async Task<ServiceInfo?> TryGetRepositoryServiceAsync()
        {
            try
            {
                var connectionString = Common.ConnectionString;
                if (string.IsNullOrWhiteSpace(connectionString))
                    return null;

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("AI.ServiceConfig_Get", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandTimeout = 5;

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // HasApiKeyAccess was added in a later schema version - read it safely
                    // so old deployed databases that don't return the column don't throw.
                    bool hasApiKeyAccess = true;
                    try
                    {
                        var ordinal = reader.GetOrdinal("HasApiKeyAccess");
                        hasApiKeyAccess = !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // Column not present in this DB version - assume access granted
                        // (sysadmin / db_owner always had access before DDM was introduced)
                    }

                    var info = new ServiceInfo
                    {
                        ServiceUrl = reader["ServiceUrl"]?.ToString(),
                        ApiKey = reader["ApiKey"]?.ToString(),
                        IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        LastHeartbeat = reader["LastHeartbeat"] as DateTime?,
                        ServiceVersion = reader["ServiceVersion"]?.ToString(),
                        HasApiKeyAccess = hasApiKeyAccess,
                        Source = ServiceSource.Repository
                        // Repository mode: the service was registered against this DB so
                        // a fingerprint mismatch check is not needed.
                    };

                    return info;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AIServiceDiscovery: repository DB lookup failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if any AI service (local or repository) is available.
        /// </summary>
        public static async Task<bool> IsServiceAvailableAsync()
        {
            var info = await GetServiceInfoAsync();
            return info != null && info.IsEnabled && info.IsActive;
        }
    }
}
