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
        public HashSet<string> AllowedScripts { get; set; }
        public bool IsAllowAllScripts { get; set; }

        /// <summary>
        /// This is the ConnectionString of the S3 source connection used to import data from the remote agent.  This is stored and associated with the agent in the repository.  When sending messages to the agent, this will be used for the message payload as SQS messages are limited in size.
        /// </summary>
        public string S3Path { get; set; }

        private readonly CacheItemPolicy policy = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(60)
        };

        public string AgentIdentifier => Convert.ToBase64String(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(string.Concat(AgentServiceName, AgentHostName, AgentPath))));

        ///<summary>
        ///Get the DBADashAgentID from the repository DB.  This will collect/update on startup then be cached.
        ///</summary>
        public int GetDBADashAgentID(string connectionString)
        {
            int agentID;
            var cacheKey =
                // Caching takes all properties into account + connection string (as we could be writing to multiple repositories and the agent could have different IDs for each).  Base off MD5 hash which should be sufficient for this use case.
                Convert.ToBase64String(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(string.Concat(connectionString, AgentServiceName, AgentVersion, AgentHostName, AgentPath, ServiceSQSQueueUrl, MessagingEnabled, S3Path, AllowedScripts))));
            if (cache.Contains(cacheKey))
            {
                agentID = (int)cache[cacheKey];
            }
            else
            {
                Log.Information("Update DBADashAgent");
                agentID = Update(connectionString);
                Log.Information("DBADashAgentID: {0}", agentID);
                if (cache.Contains(agentID
                        .ToString()))
                {
                    // Remove old cache entry which will prevent updates if settings are toggled back and forth
                    cache.Remove((string)cache[agentID.ToString()]);
                    Log.Debug("Removed old cache entry for agentID: {0}", agentID);
                }
                cache.Add(cacheKey, agentID, policy);
                cache.Add(agentID.ToString(), cacheKey, policy); // Add reverse lookup so we can identify the cache key to remove if settings are toggled back and forth
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
                AllowedScripts = string.IsNullOrEmpty(cfg.AllowedScripts) ? new HashSet<string>() : new HashSet<string>(cfg.AllowedScripts.Split(',')
                    .Select(part => part.Trim())),
                IsAllowAllScripts = cfg.AllowedScripts == "*"
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
                return new DBADashAgent()
                {
                    AgentServiceName = rdr["AgentServiceName"].ToString(),
                    AgentHostName = rdr["AgentHostName"].ToString(),
                    AgentPath = rdr["AgentPath"].ToString(),
                    AgentVersion = rdr["AgentVersion"].ToString(),
                    ServiceSQSQueueUrl = rdr["ServiceSQSQueueURL"].ToString(),
                    S3Path = rdr["S3Path"] == DBNull.Value ? null : rdr["S3Path"].ToString(),
                    MessagingEnabled = rdr["MessagingEnabled"] != DBNull.Value && (bool)rdr["MessagingEnabled"],
                    AllowedScripts = new HashSet<string>(allowedScripts.Split(',')
                        .Select(part => part.Trim())),
                    IsAllowAllScripts = allowedScripts == "*"
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
            cmd.Parameters.AddWithValue("AllowedScripts", AllowedScripts == null || AllowedScripts.Count == 0 ? DBNull.Value : string.Join(',', AllowedScripts));

            pAgentID.Direction = System.Data.ParameterDirection.Output;
            cmd.ExecuteNonQuery();
            return (int)pAgentID.Value;
        }
    }
}