using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.Caching;
using Serilog;
namespace DBADash
{
    public class DBADashAgent
    {
        MemoryCache cache = MemoryCache.Default;

        public string AgentServiceName { get; set; }
        public string AgentHostName { get; set; }
        public string AgentPath { get; set; }
        public string AgentVersion { get; set; }        
  
        CacheItemPolicy policy = new CacheItemPolicy
        {
            SlidingExpiration = TimeSpan.FromMinutes(60)
        };

        ///<summary>
        ///Get the DBADashAgentID from the repository DB.  This will collect/update on startup then be cached. 
        ///</summary>
        public int GetDBADashAgentID(string connectionString)
        {
            int agentID;
            var hash = MD5.Create(); 
            string cacheKey;
            // Caching takes all properties into account + connection string (as we could be writing to multiple repositories and the agent could have different IDs for each).  Base off MD5 hash which should be sufficient for this use case.
            cacheKey = Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Concat(connectionString, AgentServiceName, AgentVersion, AgentHostName, AgentPath))));
            if (cache.Contains(cacheKey))
            {
                agentID = (int)cache[cacheKey];
            }
            else
            {
                Log.Information("Update DBADashAgent");
                agentID = update(connectionString);
                Log.Information("DBADashAgentID: {0}", agentID);
                cache.Add(cacheKey, agentID, policy );

            }
            return agentID;
        }

        public override bool Equals(object obj)
        {
            if(obj.GetType() == typeof(DBADashAgent))
            {
                var compare = (DBADashAgent)obj;
                if (this.AgentServiceName == compare.AgentServiceName 
                     && this.AgentHostName == compare.AgentHostName
                    && this.AgentPath == compare.AgentPath
                    && this.AgentVersion == compare.AgentVersion)
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
            return string.Format("{0}|{1}|{2}|{3}", AgentServiceName, AgentHostName, AgentPath, AgentVersion).GetHashCode();
        }

        ///<summary>
        ///Return a DBADashAgent object by providing a service name.  AgentPath, Version and HostName are set automatically.
        ///</summary>
        public static DBADashAgent GetCurrent(string serviceName)
        {
            return new DBADashAgent()
            {
                AgentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                AgentHostName = Environment.MachineName,
                AgentServiceName = serviceName,
                AgentPath = AppDomain.CurrentDomain.BaseDirectory
            };
        }

        private int update(string connectionString)
        {
            using (var cn = new SqlConnection(connectionString))
            using(var cmd = new SqlCommand("dbo.DBADashAgent_Upd",cn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("AgentServiceName", AgentServiceName);
                cmd.Parameters.AddWithValue("AgentHostName", AgentHostName);
                cmd.Parameters.AddWithValue("AgentPath", AgentPath);
                cmd.Parameters.AddWithValue("AgentVersion", AgentVersion);
                var pAgentID = cmd.Parameters.Add("DBADashAgentID", System.Data.SqlDbType.Int);
                pAgentID.Direction = System.Data.ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                return (int)pAgentID.Value;
            }
        }
    }
}
