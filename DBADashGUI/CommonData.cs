﻿using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    static class CommonData
    {
        public static DataTable GetInstances(string tagIDs="",bool? Active=true,bool? azureDB=null)
        {

            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand(@"dbo.Instances_Get", cn) { CommandType = CommandType.StoredProcedure })
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
                    
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();                      
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public static Int32 GetDatabaseID(string instance, string dbName)
        {
            if (instance == null || instance.Length == 0 || dbName == null || dbName.Length == 0)
            {
                return -1;
            }
            else
            {
                using (var cn = new SqlConnection(Common.ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("DatabaseID_Get", cn) { CommandType = CommandType.StoredProcedure })
                    {
                        cn.Open();
                        cmd.Parameters.AddWithValue("Instance", instance);
                        cmd.Parameters.AddWithValue("DBName", dbName);
                        return (Int32)cmd.ExecuteScalar();
                    }
                }
            }
        }

        public static DataTable GetFiles(Int32 DatabaseID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DBFiles_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                    cmd.Parameters.AddWithValue("IncludeWarning", true);
                    cmd.Parameters.AddWithValue("IncludeNA", true);
                    cmd.Parameters.AddWithValue("IncludeCritical", true);
                    cmd.Parameters.AddWithValue("IncludeOK", true);
                    cmd.Parameters.AddWithValue("FileGroupLevel", 0);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable GetFileGroups(Int32 DatabaseID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.FileGroup_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable ObjectExecutionStats(Int32 instanceID, Int32 databaseID, DateTime from, DateTime to, Int64 objectID, Int32 dateGrouping, string measure, string instance = "")
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.ObjectExecutionStats_Get", cn))
                {
                    cn.Open();
                    if (instanceID > 0)
                    {
                        cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    }
                    if (instance != null && instance.Length > 0)
                    {
                        cmd.Parameters.AddWithValue("Instance", instance);
                    }
                    cmd.Parameters.AddWithValue("FromDateUTC", from);
                    cmd.Parameters.AddWithValue("ToDateUTC", to);
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
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable GetJobSteps(Int32 InstanceID,Guid JobID)
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobSteps_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
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
            var jobs = new Dictionary<Guid, String>();
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
            List<string> instances = new List<string>();
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

        public static List<DatabaseItem> GetDatabasesWithDDLSnapshot(string instance)
        {
            List<DatabaseItem> databases = new List<DatabaseItem>();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesWithDDLSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("Instance", instance);
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
            Dictionary<string, string> objtypes = new Dictionary<string, string>();
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
            using (SqlCommand cmd = new SqlCommand("dbo.DBObjects_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
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
            {
                using (var cmd = new SqlCommand("dbo.DDLHistoryForObject_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("ObjectID", ObjectId);
                    cmd.Parameters.AddWithValue("PageSize", PageSize);
                    cmd.Parameters.AddWithValue("PageNumber", PageNum);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

    }
}
