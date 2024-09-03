using System;
using System.IO;
using System.Reflection;

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
            var resourcePath = "DBADash.SQL.SQL" + name + ".sql";
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new Exception($"Resource {resourcePath} not found.");
            using StreamReader reader = new(stream);

            return reader.ReadToEnd();
        }

        public static string AgentJobs => GetSqlString("AgentJobs");
        public static string Alerts => GetSqlString("Alerts");
        public static string AvailabilityGroups => GetSqlString("AvailabilityGroups");
        public static string AvailabilityReplicas => GetSqlString("AvailabilityReplicas");
        public static string AzureDBElasticPoolResourceStats => GetSqlString("AzureDBElasticPoolResourceStats");
        public static string AzureDBResourceGovernance => GetSqlString("AzureDBResourceGovernance");
        public static string AzureDBResourceStats => GetSqlString("AzureDBResourceStats");
        public static string AzureDBServiceObjectives => GetSqlString("AzureDBServiceObjectives");
        public static string Backups => GetSqlString("Backups");
        public static string BlockingSnapshot => GetSqlString("BlockingSnapshot");
        public static string Corruption => GetSqlString("Corruption");
        public static string CPU => GetSqlString("CPU");
        public static string CustomChecks => GetSqlString("CustomChecks");
        public static string DatabaseMirroring => GetSqlString("DatabaseMirroring");
        public static string DatabasePermissions => GetSqlString("DatabasePermissions");
        public static string DatabasePrincipals => GetSqlString("DatabasePrincipals");
        public static string DatabaseQueryStoreOptions => GetSqlString("DatabaseQueryStoreOptions");
        public static string DatabaseRoleMembers => GetSqlString("DatabaseRoleMembers");
        public static string Databases => GetSqlString("Databases");
        public static string DatabasesHADR => GetSqlString("DatabasesHADR");
        public static string DBConfig => GetSqlString("DBConfig");
        public static string DBFiles => GetSqlString("DBFiles");
        public static string DBTuningOptions => GetSqlString("DBTuningOptions");
        public static string Drives => GetSqlString("Drives");
        public static string IdentityColumns => GetSqlString("IdentityColumns");
        public static string Instance => GetSqlString("Instance");
        public static string IOStats => GetSqlString("IOStats");
        public static string JobHistory => GetSqlString("JobHistory");
        public static string LastGoodCheckDB => GetSqlString("LastGoodCheckDB");
        public static string LogRestores => GetSqlString("LogRestores");
        public static string MemoryUsage => GetSqlString("MemoryUsage");
        public static string ObjectExecutionStats => GetSqlString("ObjectExecutionStats");
        public static string OSInfo => GetSqlString("OSInfo");
        public static string OSLoadedModules => GetSqlString("OSLoadedModules");
        public static string PerformanceCounters => GetSqlString("PerformanceCounters");
        public static string RemoveEventSessions => GetSqlString("RemoveEventSessions");
        public static string RemoveEventSessionsAzure => GetSqlString("RemoveEventSessionsAzure");
        public static string RunningQueries => GetSqlString("RunningQueries");
        public static string ServerExtraProperties => GetSqlString("ServerExtraProperties");
        public static string ServerPermissions => GetSqlString("ServerPermissions");
        public static string ServerPrincipals => GetSqlString("ServerPrincipals");
        public static string ServerProperties => GetSqlString("ServerProperties");
        public static string ServerRoleMembers => GetSqlString("ServerRoleMembers");
        public static string SlowQueries => GetSqlString("SlowQueries");
        public static string SlowQueriesAzure => GetSqlString("SlowQueriesAzure");
        public static string StopEventSessions => GetSqlString("StopEventSessions");
        public static string StopEventSessionsAzure => GetSqlString("StopEventSessionsAzure");
        public static string SysConfig => GetSqlString("SysConfig");
        public static string TraceFlags => GetSqlString("TraceFlags");
        public static string VLF => GetSqlString("VLF");
        public static string Waits => GetSqlString("Waits");
        public static string Jobs => GetSqlString("Jobs");
        public static string JobSteps => GetSqlString("JobSteps");
        public static string RunningJobs => GetSqlString("RunningJobs");
        public static string QueryStoreTopQueries => GetSqlString("QueryStoreTopQueries");
        public static string QueryStoreForcedPlans => GetSqlString("QueryStoreForcedPlans");
        public static string ServerServices => GetSqlString("ServerServices");
    }
}