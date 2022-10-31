using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADashGUI.Performance;

namespace DBADashGUI
{
    static class CommonData
    {

        public static DataTable Instances;

        public static void UpdateInstancesList(string tagIDs = "", bool? Active = true, bool? azureDB = null, string searchString = "",string groupByTag="")
        {
            Instances = GetInstances(tagIDs, Active, azureDB, searchString,groupByTag);
        }

        public static DataTable GetInstances(string tagIDs = "", bool? Active = true, bool? azureDB = null, string searchString = "", string groupByTag = "")
        {

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand(@"dbo.Instances_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                if (tagIDs != null && tagIDs != String.Empty)
                {
                    cmd.Parameters.AddWithValue("TagIDs", tagIDs);
                }
                if (Active == null)
                {
                    cmd.Parameters.AddWithValue("IsActive", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("IsActive", Active);
                }
                if (azureDB != null)
                {
                    cmd.Parameters.AddWithValue("IsAzure", azureDB);
                }
                if (!string.IsNullOrEmpty(searchString))
                {
                    cmd.Parameters.AddWithValue("SearchString", searchString);
                }
                if (!string.IsNullOrEmpty(groupByTag))
                {
                    cmd.Parameters.AddWithValue("GroupByTag", groupByTag);
                }
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        public static Int32 GetDatabaseID(string instanceGroupName, string dbName)
        {
            if (instanceGroupName == null || instanceGroupName.Length == 0 || dbName == null || dbName.Length == 0)
            {
                return -1;
            }
            else
            {
                using (var cn = new SqlConnection(Common.ConnectionString))
                using (var cmd = new SqlCommand("DatabaseID_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("InstanceGroupName", instanceGroupName);
                    cmd.Parameters.AddWithValue("DBName", dbName);
                    return (Int32)cmd.ExecuteScalar();
                }
            }
        }

        public static DataTable GetFiles(Int32 DatabaseID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBFiles_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
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
        }

        public static DataTable GetFileGroups(Int32 DatabaseID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.FileGroup_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }

        }

        public static DataTable ObjectExecutionStats(Int32 instanceID, Int32 databaseID, Int64 objectID, Int32 dateGrouping, string measure, DateTime FromDate, DateTime ToDate, string instance = "")
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.ObjectExecutionStats_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                if (instanceID > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                }
                else if (instance != null && instance.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Instance", instance);
                }
                cmd.Parameters.AddWithValue("FromDateUTC", FromDate);
                cmd.Parameters.AddWithValue("ToDateUTC", ToDate);
                cmd.Parameters.AddWithValue("UTCOffset", Common.UtcOffset);
                if (objectID > 0)
                {
                    cmd.Parameters.AddWithValue("ObjectID", objectID);
                }
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                cmd.Parameters.AddWithValue("Measure", measure);
                if (databaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", databaseID);
                }
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

        }

        public static DataTable GetJobSteps(Int32 InstanceID, Guid JobID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobSteps_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("JobID", JobID);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static DataTable GetJobs(int InstanceId)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Jobs_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceId);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static List<string> GetInstancesWithDDLSnapshot(List<int> tags)
        {
            List<string> instances = new();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstancesWithDDLSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                if (tags.Count > 0)
                {
                    cmd.Parameters.AddWithValue("TagIDs", string.Join(",", tags));
                }
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        instances.Add((string)rdr[0]);
                    }
                }
            }
            return instances;
        }

        public static List<DatabaseItem> GetDatabasesWithDDLSnapshot(string instanceGroupName)
        {
            List<DatabaseItem> databases = new();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesWithDDLSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceGroupName", instanceGroupName);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        databases.Add(new DatabaseItem() { DatabaseID = (Int32)rdr[0], DatabaseName = (string)rdr[1] });
                    }
                }
            }
            return databases;
        }

        public static Dictionary<string, string> GetObjectTypes()
        {
            Dictionary<string, string> objtypes = new();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.ObjectType_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        objtypes.Add(((string)rdr[0]).TrimEnd(), (string)rdr[1]);
                    }
                }
            }
            return objtypes;
        }

        public static DataTable GetDBObjects(int DatabaseID, string types)
        {
            var dt = new DataTable();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBObjects_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("Types", types);
                da.Fill(dt);
            }
            return dt;
        }

        public static DataTable GetDDLHistoryForObject(Int64 ObjectId, int PageNum, int PageSize)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DDLHistoryForObject_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("ObjectID", ObjectId);
                cmd.Parameters.AddWithValue("PageSize", PageSize);
                cmd.Parameters.AddWithValue("PageNumber", PageNum);

                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
        public static DataTable GetCounters()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Counters_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }
    }
}
