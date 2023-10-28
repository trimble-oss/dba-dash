using DBADashGUI.Performance;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Caching;

namespace DBADashGUI
{
    internal static class CommonData
    {
        public static DataTable Instances;

        private static readonly MemoryCache cache = MemoryCache.Default;

        public static void UpdateInstancesList(string tagIDs = "", bool? Active = true, bool? azureDB = null, string searchString = "", string groupByTag = "")
        {
            Instances = GetInstances(tagIDs, Active, azureDB, searchString, groupByTag);
        }

        public static DataTable GetInstances(string tagIDs = "", bool? Active = true, bool? azureDB = null, string searchString = "", string groupByTag = "")
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand(@"dbo.Instances_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddStringIfNotNullOrEmpty("TagIDs", tagIDs);
            cmd.Parameters.AddWithNullableValue("IsActive", Active);
            cmd.Parameters.AddWithNullableValue("IsAzure", azureDB);
            cmd.Parameters.AddStringIfNotNullOrEmpty("SearchString", searchString);
            cmd.Parameters.AddStringIfNotNullOrEmpty("GroupByTag", groupByTag);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public static int GetDatabaseID(string instanceGroupName, string dbName)
        {
            if (instanceGroupName == null || instanceGroupName.Length == 0 || dbName == null || dbName.Length == 0)
            {
                return -1;
            }
            else
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("DatabaseID_Get", cn) { CommandType = CommandType.StoredProcedure };
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceGroupName", instanceGroupName);
                cmd.Parameters.AddWithValue("DBName", dbName);
                return (int)cmd.ExecuteScalar();
            }
        }

        public static DataTable GetFiles(int DatabaseID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBFiles_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("IncludeWarning", true);
            cmd.Parameters.AddWithValue("IncludeNA", true);
            cmd.Parameters.AddWithValue("IncludeCritical", true);
            cmd.Parameters.AddWithValue("IncludeOK", true);
            cmd.Parameters.AddWithValue("FileGroupLevel", 0);

            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public static DataTable GetFileGroups(int DatabaseID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.FileGroup_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public static DataTable ObjectExecutionStats(int instanceID, int databaseID, long objectID, int dateGrouping, string measure, DateTime FromDate, DateTime ToDate, string instance = "")
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ObjectExecutionStats_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();

            cmd.Parameters.AddIfGreaterThanZero("InstanceID", instanceID);
            cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", instance);
            cmd.Parameters.AddWithValue("FromDateUTC", FromDate);
            cmd.Parameters.AddWithValue("ToDateUTC", ToDate);
            cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);
            cmd.Parameters.AddIfGreaterThanZero("ObjectID", objectID);
            cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
            cmd.Parameters.AddWithValue("Measure", measure);
            cmd.Parameters.AddIfGreaterThanZero("DatabaseID", databaseID);
            if (DateRange.HasTimeOfDayFilter)
            {
                cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
            }
            if (DateRange.HasDayOfWeekFilter)
            {
                cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
            }
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public static DataTable GetJobSteps(int InstanceID, Guid JobID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobSteps_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("JobID", JobID);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static DataTable GetJobs(int InstanceId)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Jobs_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceId);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static List<string> GetInstancesWithDDLSnapshot(List<int> tags)
        {
            List<string> instances = new();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.InstancesWithDDLSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            if (tags.Count > 0)
            {
                cmd.Parameters.AddWithValue("TagIDs", string.Join(",", tags));
            }

            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                instances.Add((string)rdr[0]);
            }

            return instances;
        }

        public static List<DatabaseItem> GetDatabasesWithDDLSnapshot(string instanceGroupName)
        {
            List<DatabaseItem> databases = new();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DatabasesWithDDLSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceGroupName", instanceGroupName);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                databases.Add(new DatabaseItem() { DatabaseID = (int)rdr[0], DatabaseName = (string)rdr[1] });
            }

            return databases;
        }

        public static Dictionary<string, string> GetObjectTypes()
        {
            Dictionary<string, string> objtypes = new();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ObjectType_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                objtypes.Add(((string)rdr[0]).TrimEnd(), (string)rdr[1]);
            }

            return objtypes;
        }

        public static DataTable GetDBObjects(int DatabaseID, string types)
        {
            var dt = new DataTable();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBObjects_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("Types", types);
            da.Fill(dt);
            return dt;
        }

        public static DataTable GetDDLHistoryForObject(long ObjectId, int PageNum, int PageSize)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DDLHistoryForObject_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("ObjectID", ObjectId);
            cmd.Parameters.AddWithValue("PageSize", PageSize);
            cmd.Parameters.AddWithValue("PageNumber", PageNum);

            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static DataTable GetCounters()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Counters_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public static void AcknowledgeInstanceUptime(int instanceId)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.InstanceUptimeAck", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.ExecuteNonQuery();
        }

        public static DataTable GetDrives(HashSet<int> instanceIDs, bool includeMetrics, bool includeCritical, bool includeWarning, bool includeNA, bool includeOK, bool showHidden, string driveName, bool hasMetrics = false)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Drives_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", instanceIDs));
            cmd.Parameters.Add(new SqlParameter() { ParameterName = "IncludeCritical", DbType = System.Data.DbType.Boolean, Value = includeCritical });
            cmd.Parameters.Add(new SqlParameter() { ParameterName = "IncludeWarning", DbType = System.Data.DbType.Boolean, Value = includeWarning });
            cmd.Parameters.Add(new SqlParameter() { ParameterName = "IncludeNA", DbType = System.Data.DbType.Boolean, Value = includeNA });
            cmd.Parameters.Add(new SqlParameter() { ParameterName = "IncludeOK", DbType = System.Data.DbType.Boolean, Value = includeOK });
            cmd.Parameters.AddWithNullableValue("DriveName", driveName);
            cmd.Parameters.AddWithValue("IncludeMetrics", includeMetrics);
            cmd.Parameters.AddWithValue("ShowHidden", showHidden);
            cmd.Parameters.AddWithValue("HasMetrics", hasMetrics);

            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public static DataTable GetMetricDrives(int instanceID, string driveName = null)
        {
            var key = "GetMetricDrives_" + instanceID + "_" + driveName;
            if (cache.Get(key) is DataTable metricDrives) return metricDrives;

            metricDrives = GetDrives(new HashSet<int>() { instanceID }, false, true, true, true, true, true, driveName,
                true);
            metricDrives.DefaultView.Sort = "Name";
            metricDrives = metricDrives.DefaultView.ToTable();
            var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) };
            cache.Add(key, metricDrives, policy);

            return metricDrives;
        }

        public static void ClearCache()
        {
            foreach (var element in cache)
            {
                cache.Remove(element.Key);
            }
        }
    }
}