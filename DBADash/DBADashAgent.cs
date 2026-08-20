using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Security.Cryptography;

namespace DBADash
{
    public class DBADashAgent
    {
        private readonly MemoryCache cache = MemoryCache.Default;

        public string AgentServiceName { get; set; }
        public string AgentHostName { get; set; }
        public string AgentPath { get; set; }
        public string AgentVersion { get; set; }
        public string ServiceSQSQueueUrl { get; set; }
        public bool MessagingEnabled { get; set; }
        public bool KillSessionEnabled { get; set; }
        public bool PlanForcingEnabled { get; set; }

        /// <summary>
        /// Hard cap (seconds) the service applies to ad-hoc XE trace / watch durations (<see
        /// cref="CollectionConfig.AdhocXEMaxDurationSeconds"/>).  Surfaced to the GUI (via the DBADashAgent row) so it
        /// can warn about / clamp a requested duration up-front rather than relying solely on the server-side clamp.
        /// </summary>
        public int AdhocXEMaxDurationSeconds { get; set; } = CollectionConfig.DefaultAdhocXEMaxDurationSeconds;

        // Normalise empty <-> null to a single canonical value (null).  The collect agent is reconstructed from
        // collected metadata (DBImporter.GetAgent) where an unset value arrives as an empty string, while the import
        // agent (GetCurrent) carries the raw config value (which may be null).  Without normalisation the same physical
        // agent registers twice with differing values (''/NULL), producing redundant DBADashAgent_Upd calls that fight
        // over the row each collection cycle.  Canonicalising here keeps the cache key and the update payload stable.
        private string _allowedScriptsCSV;
        public string AllowedScriptsCSV
        {
            get => _allowedScriptsCSV;
            set
            {
                _allowedScriptsCSV = string.IsNullOrEmpty(value) ? null : value;
                _allowedScriptsInfo = null; // reset the derived cache
            }
        }

        private string _allowedCustomProcsCSV;
        public string AllowedCustomProcsCSV
        {
            get => _allowedCustomProcsCSV;
            set
            {
                _allowedCustomProcsCSV = string.IsNullOrEmpty(value) ? null : value;
                _allowedCustomProcs = null; // reset the derived cache
            }
        }

        // Comma-separated allow/deny lists (XESessionFilter syntax) advertised so the GUI can gray out sessions it may
        // not start/stop or watch, instead of only finding out when the service rejects the request.  Null = the agent
        // hasn't reported a policy (older service) - see XEPolicyReported; empty is canonicalised to null (as for the
        // Allowed* CSVs) to keep the cache key / Upd payload stable across the two agent-construction paths.
        private string _manageXESessions;
        public string ManageXESessions
        {
            get => _manageXESessions;
            set => _manageXESessions = string.IsNullOrEmpty(value) ? null : value;
        }

        private string _watchXESessions;
        public string WatchXESessions
        {
            get => _watchXESessions;
            set => _watchXESessions = string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// True once the agent has advertised an XE session policy at all (either list set).  When false the service is
        /// an older build that doesn't report the policy, so the GUI should not gray out sessions - it lets the request
        /// through and relies on the service-side check (which always enforces regardless).
        /// </summary>
        public bool XEPolicyReported => _manageXESessions != null || _watchXESessions != null;

        /// <summary>True if <paramref name="sessionName"/> may be started/stopped (allows the attempt when no policy is reported).</summary>
        public bool CanManageXESession(string sessionName) =>
            !XEPolicyReported || XE.XESessionFilter.Parse(_manageXESessions).IsAllowed(sessionName);

        /// <summary>True if <paramref name="sessionName"/> may be watched (allows the attempt when no policy is reported).</summary>
        public bool CanWatchXESession(string sessionName) =>
            !XEPolicyReported || XE.XESessionFilter.Parse(_watchXESessions).IsAllowed(sessionName);

        public HashSet<string> AllowedScripts => AllowedScriptsInfo.scripts;
        public bool IsAllowAllScripts => AllowedScriptsInfo.isAllowAll;
        public HashSet<string> AllowedCustomProcs => _allowedCustomProcs ??= ProcessAllowedCustomProcs(AllowedCustomProcsCSV);

        // Single computation for both AllowedScripts and IsAllowAllScripts
        private (HashSet<string> scripts, bool isAllowAll) AllowedScriptsInfo =>
            _allowedScriptsInfo ??= ProcessAllowedScripts(AllowedScriptsCSV);

        private (HashSet<string> scripts, bool isAllowAll)? _allowedScriptsInfo;
        private HashSet<string> _allowedCustomProcs;

        /// <summary>
        /// This is the ConnectionString of the S3 source connection used to import data from the remote agent.  This is stored and associated with the agent in the repository.  When sending messages to the agent, this will be used for the message payload as SQS messages are limited in size.
        /// </summary>
        public string S3Path { get; set; }

        private static CacheItemPolicy NewPolicy() => new()
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        public string AgentIdentifier => Convert.ToBase64String(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(string.Concat(AgentServiceName, AgentHostName, AgentPath))));

        ///<summary>
        ///Get the DBADashAgentID from the repository DB.  This will collect/update on startup then be cached.
        ///</summary>
        public int GetDBADashAgentID(string connectionString)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);

            int agentID;
            var cacheKey =
                // Caching takes all properties into account + connection string (as we could be writing to multiple repositories and the agent could have different IDs for each).  Base off MD5 hash which should be sufficient for this use case.
                Convert.ToBase64String(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('|', connectionString, AgentServiceName, AgentVersion, AgentHostName, AgentPath, ServiceSQSQueueUrl, MessagingEnabled, KillSessionEnabled, PlanForcingEnabled, AdhocXEMaxDurationSeconds, S3Path, AllowedScriptsCSV, AllowedCustomProcsCSV, ManageXESessions, WatchXESessions))));
            if (cache.Contains(cacheKey))
            {
                agentID = (int)cache[cacheKey];
            }
            else
            {
                Log.Information("Update DBADashAgent");
                agentID = Update(connectionString);
                Log.Information("DBADashAgentID: {0}", agentID);
                var connectionHash = Convert.ToBase64String(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(connectionString)));
                var agentIdKey = $"{agentID}|{connectionHash}"; // Namespace by connection to avoid collisions across repositories
                var oldCacheKey = cache.Get(agentIdKey) as string;
                if (!string.IsNullOrEmpty(oldCacheKey))
                {
                    // Remove old cacheKey entry which will prevent updates if settings are toggled back and forth
                    cache.Remove(oldCacheKey);
                    cache.Remove(agentIdKey);
                    Log.Debug("Removed old cache entry for agentID: {0}", agentID);
                }
                var policy = NewPolicy();
                cache.Set(cacheKey, agentID, policy);
                cache.Set(agentIdKey, cacheKey, policy); // Add reverse lookup so we can identify the cache key to remove if settings are toggled back and forth
            }
            return agentID;
        }

        public override bool Equals(object obj)
        {
            if (obj?.GetType() == typeof(DBADashAgent))
            {
                var compare = (DBADashAgent)obj;
                if (AgentServiceName == compare.AgentServiceName
                     && AgentHostName == compare.AgentHostName
                    && AgentPath == compare.AgentPath
                    && AgentVersion == compare.AgentVersion)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return $"{AgentServiceName}|{AgentHostName}|{AgentPath}|{AgentVersion}".GetHashCode();
        }

        private static DBADashAgent currentAgent;

        ///<summary>
        ///Return a DBADashAgent object by providing a service name.  AgentPath, Version and HostName are set automatically.
        ///</summary>
        public static DBADashAgent GetCurrent()
        {
            currentAgent ??= GetCurrentAgent();
            return currentAgent;
        }

        private static DBADashAgent GetCurrentAgent()
        {
            var cfg = BasicConfig.Load<CollectionConfig>();
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            return new DBADashAgent()
            {
                AgentVersion = version?.ToString(),
                AgentHostName = Environment.MachineName,
                AgentServiceName = cfg.ServiceName,
                AgentPath = AppDomain.CurrentDomain.BaseDirectory,
                ServiceSQSQueueUrl = cfg.ServiceSQSQueueUrl,
                MessagingEnabled = cfg.EnableMessaging,
                KillSessionEnabled = cfg.AllowKillSession,
                PlanForcingEnabled = cfg.AllowPlanForcing,
                AdhocXEMaxDurationSeconds = cfg.AdhocXEMaxDurationSeconds,
                AllowedScriptsCSV = cfg.AllowedScripts,
                AllowedCustomProcsCSV = cfg.AllowedCustomProcs,
                ManageXESessions = cfg.ManageXESessions,
                WatchXESessions = cfg.WatchXESessions
            };
        }

        public static DBADashAgent GetDBADashAgent(string connectionString, int id)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DBADashAgent_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("DBADashAgentID", id);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                var allowedScripts = rdr["AllowedScripts"].ToString() ?? string.Empty;
                var allowedCustomProcs = rdr["AllowedCustomProcs"].ToString() ?? string.Empty;
                return new DBADashAgent()
                {
                    AgentServiceName = rdr["AgentServiceName"].ToString(),
                    AgentHostName = rdr["AgentHostName"].ToString(),
                    AgentPath = rdr["AgentPath"].ToString(),
                    AgentVersion = rdr["AgentVersion"].ToString(),
                    ServiceSQSQueueUrl = rdr["ServiceSQSQueueURL"].ToString(),
                    S3Path = rdr["S3Path"] == DBNull.Value ? null : rdr["S3Path"].ToString(),
                    MessagingEnabled = rdr["MessagingEnabled"] != DBNull.Value && (bool)rdr["MessagingEnabled"],
                    KillSessionEnabled = rdr["KillSessionEnabled"] != DBNull.Value && (bool)rdr["KillSessionEnabled"],
                    PlanForcingEnabled = rdr["PlanForcingEnabled"] != DBNull.Value && (bool)rdr["PlanForcingEnabled"],
                    AdhocXEMaxDurationSeconds = rdr["AdhocXEMaxDurationSeconds"] != DBNull.Value
                        ? (int)rdr["AdhocXEMaxDurationSeconds"]
                        : CollectionConfig.DefaultAdhocXEMaxDurationSeconds,
                    AllowedScriptsCSV = allowedScripts,
                    AllowedCustomProcsCSV = allowedCustomProcs,
                    ManageXESessions = rdr["ManageXESessions"] == DBNull.Value ? null : rdr["ManageXESessions"].ToString(),
                    WatchXESessions = rdr["WatchXESessions"] == DBNull.Value ? null : rdr["WatchXESessions"].ToString()
                };
            }
            else
            {
                throw new ArgumentException("Agent not found");
            }
        }

        private int Update(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DBADashAgent_Upd", cn) { CommandType = System.Data.CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("AgentServiceName", AgentServiceName);
            cmd.Parameters.AddWithValue("AgentHostName", AgentHostName);
            cmd.Parameters.AddWithValue("AgentPath", AgentPath);
            cmd.Parameters.AddWithValue("AgentVersion", AgentVersion);
            var pAgentID = cmd.Parameters.Add("DBADashAgentID", System.Data.SqlDbType.Int);
            cmd.Parameters.AddWithValue("ServiceSQSQueueURL", ServiceSQSQueueUrl);
            cmd.Parameters.AddWithValue("AgentIdentifier", AgentIdentifier);
            if (!string.IsNullOrEmpty(S3Path))
            {
                cmd.Parameters.AddWithValue("S3Path", S3Path);
            }
            cmd.Parameters.AddWithValue("MessagingEnabled", MessagingEnabled);
            cmd.Parameters.AddWithValue("KillSessionEnabled", KillSessionEnabled);
            cmd.Parameters.AddWithValue("PlanForcingEnabled", PlanForcingEnabled);
            cmd.Parameters.AddWithValue("AdhocXEMaxDurationSeconds", AdhocXEMaxDurationSeconds);
            cmd.Parameters.AddWithValue("AllowedScripts", AllowedScriptsCSV);
            cmd.Parameters.AddWithValue("AllowedCustomProcs", AllowedCustomProcsCSV);
            cmd.Parameters.AddWithValue("ManageXESessions", (object)ManageXESessions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("WatchXESessions", (object)WatchXESessions ?? DBNull.Value);
            pAgentID.Direction = System.Data.ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return (int)pAgentID.Value;
        }

        private static (HashSet<string> scripts, bool isAllowAll) ProcessAllowedScripts(string allowedScripts)
        {
            if (string.IsNullOrEmpty(allowedScripts))
            {
                return (new HashSet<string>(StringComparer.OrdinalIgnoreCase), false);
            }

            bool isAllowAll = allowedScripts.Trim() == "*";
            var scripts = new HashSet<string>(
                allowedScripts.Split(',').Select(part => part.Trim()),
                StringComparer.OrdinalIgnoreCase);

            return (scripts, isAllowAll);
        }

        // Helper method to process allowed custom procs
        private static HashSet<string> ProcessAllowedCustomProcs(string allowedCustomProcs)
        {
            return string.IsNullOrEmpty(allowedCustomProcs)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(
                    allowedCustomProcs.Split(',').Select(part => part.Trim()),
                    StringComparer.OrdinalIgnoreCase);
        }
    }
}