using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    internal class SqlStrings
    {

        public static string GetSqlString(CollectionType type)
        {
            return GetSqlString(Enum.GetName(typeof(CollectionType), type));
        }

     
        public static string GetSqlString(string name)
        {
            string resourcePath = "DBADash.SQL.SQL" + name + ".sql";
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string AgentJobs { get { return GetSqlString("AgentJobs"); } }
        public static string Alerts { get { return GetSqlString("Alerts"); } }
        public static string AvailabilityGroups { get { return GetSqlString("AvailabilityGroups"); } }
        public static string AvailabilityReplicas { get { return GetSqlString("AvailabilityReplicas"); } }
        public static string AzureDBElasticPoolResourceStats { get { return GetSqlString("AzureDBElasticPoolResourceStats"); } }
        public static string AzureDBResourceGovernance { get { return GetSqlString("AzureDBResourceGovernance"); } }
        public static string AzureDBResourceStats { get { return GetSqlString("AzureDBResourceStats"); } }
        public static string AzureDBServiceObjectives { get { return GetSqlString("AzureDBServiceObjectives"); } }
        public static string Backups { get { return GetSqlString("Backups"); } }
        public static string BlockingSnapshot { get { return GetSqlString("BlockingSnapshot"); } }
        public static string Corruption { get { return GetSqlString("Corruption"); } }
        public static string CPU { get { return GetSqlString("CPU"); } }
        public static string CustomChecks { get { return GetSqlString("CustomChecks"); } }
        public static string DatabaseMirroring { get { return GetSqlString("DatabaseMirroring"); } }
        public static string DatabasePermissions { get { return GetSqlString("DatabasePermissions"); } }
        public static string DatabasePrincipals { get { return GetSqlString("DatabasePrincipals"); } }
        public static string DatabaseQueryStoreOptions { get { return GetSqlString("DatabaseQueryStoreOptions"); } }
        public static string DatabaseRoleMembers { get { return GetSqlString("DatabaseRoleMembers"); } }
        public static string Databases { get { return GetSqlString("Databases"); } }
        public static string DatabasesHADR { get { return GetSqlString("DatabasesHADR"); } }
        public static string DBConfig { get { return GetSqlString("DBConfig"); } }
        public static string DBFiles { get { return GetSqlString("DBFiles"); } }
        public static string DBTuningOptions { get { return GetSqlString("DBTuningOptions"); } }
        public static string Drives { get { return GetSqlString("Drives"); } }
        public static string IdentityColumns { get { return GetSqlString("IdentityColumns"); } }
        public static string Instance { get { return GetSqlString("Instance"); } }
        public static string IOStats { get { return GetSqlString("IOStats"); } }
        public static string JobHistory { get { return GetSqlString("JobHistory"); } }
        public static string LastGoodCheckDB { get { return GetSqlString("LastGoodCheckDB"); } }
        public static string LogRestores { get { return GetSqlString("LogRestores"); } }
        public static string MemoryUsage { get { return GetSqlString("MemoryUsage"); } }
        public static string ObjectExecutionStats { get { return GetSqlString("ObjectExecutionStats"); } }
        public static string OSInfo { get { return GetSqlString("OSInfo"); } }
        public static string OSLoadedModules { get { return GetSqlString("OSLoadedModules"); } }
        public static string PerformanceCounters { get { return GetSqlString("PerformanceCounters"); } }
        public static string RemoveEventSessions { get { return GetSqlString("RemoveEventSessions"); } }
        public static string RemoveEventSessionsAzure { get { return GetSqlString("RemoveEventSessionsAzure"); } }
        public static string RunningQueries { get { return GetSqlString("RunningQueries"); } }
        public static string ServerExtraProperties { get { return GetSqlString("ServerExtraProperties"); } }
        public static string ServerPermissions { get { return GetSqlString("ServerPermissions"); } }
        public static string ServerPrincipals { get { return GetSqlString("ServerPrincipals"); } }
        public static string ServerProperties { get { return GetSqlString("ServerProperties"); } }
        public static string ServerRoleMembers { get { return GetSqlString("ServerRoleMembers"); } }
        public static string SlowQueries { get { return GetSqlString("SlowQueries"); } }
        public static string SlowQueriesAzure { get { return GetSqlString("SlowQueriesAzure"); } }
        public static string StopEventSessions { get { return GetSqlString("StopEventSessions"); } }
        public static string StopEventSessionsAzure { get { return GetSqlString("StopEventSessionsAzure"); } }
        public static string SysConfig { get { return GetSqlString("SysConfig"); } }
        public static string TraceFlags { get { return GetSqlString("TraceFlags"); } }
        public static string VLF { get { return GetSqlString("VLF"); } }
        public static string Waits { get { return GetSqlString("Waits"); } }
        public static string Jobs { get { return GetSqlString("Jobs"); } }
        public static string JobSteps { get { return GetSqlString("JobSteps"); } }
    }
}
