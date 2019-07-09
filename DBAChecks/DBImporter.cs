using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAChecks
{
    class DBImporter
    {
 
        public DBImporter()
        {

        }

        public void Update(string connectionString, DataSet Data)
        {
            var rInstance = Data.Tables["DBAChecks"].Rows[0];
            DateTime snapshotDate = (DateTime)rInstance["SnapshotDateUTC"];
            Int32 instanceID;
            string connectionID = (string)rInstance["ConnectionID"];

            instanceID= updateInstance(connectionString,rInstance);
            updateServerProperties(connectionString, instanceID, snapshotDate, Data);
            updateSysConfig(connectionString, instanceID, snapshotDate, Data);
            updateDB(connectionString, instanceID, snapshotDate, Data);
            updateDrives(connectionString, instanceID, snapshotDate, Data);
            updateBackups(connectionString, instanceID, snapshotDate, Data);
            updateJobs(connectionString, instanceID, snapshotDate, Data);
            updateLogRestores(connectionString, instanceID, snapshotDate, Data);
            updateFiles(connectionString, instanceID, snapshotDate, Data);
        }

        private void updateSysConfig(string connectionString,Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Configuration") && ds.Tables["Configuration"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("SysConfig_Upd", cn);
                    cmd.Parameters.AddWithValue("Config", ds.Tables["Configuration"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void updateFiles(string connectionString,Int32 instanceID,DateTime SnapshotDate,DataSet ds)
        {
            if (ds.Tables.Contains("Properties") && ds.Tables["Properties"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("DBFiles_Upd", cn);
                    cmd.Parameters.AddWithValue("Files", ds.Tables["Files"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void updateServerProperties(string connectionString,Int32 instanceID,DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Properties") && ds.Tables["Properties"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("ServerProperties_Upd", cn);
                    cmd.Parameters.AddWithValue("Properties", ds.Tables["Properties"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void updateDrives(string connectionString,Int32 instanceID,DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Drives") && ds.Tables["Drives"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("Drives_Upd", cn);
                    cmd.Parameters.AddWithValue("Drives", ds.Tables["Drives"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void updateBackups(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Backups") && ds.Tables["Backups"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("Backups_Upd", cn);
                    cmd.Parameters.AddWithValue("Backups", ds.Tables["Backups"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void updateJobs(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("AgentJobs") && ds.Tables["AgentJobs"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("AgentJobs_Upd", cn);
                    cmd.Parameters.AddWithValue("Jobs", ds.Tables["AgentJobs"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void updateLogRestores(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("LogRestores") && ds.Tables["LogRestores"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("LogRestores_Upd", cn);
                    cmd.Parameters.AddWithValue("LogRestores", ds.Tables["LogRestores"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private Int32 updateInstance(string connectionString, DataRow rInstance)
        {
            
            var cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("Instance_Upd", cn);
                cmd.Parameters.AddWithValue("ConnectionID", (string)rInstance["ConnectionID"]);
                cmd.Parameters.AddWithValue("Instance", (string)rInstance["Instance"]);
                cmd.Parameters.AddWithValue("SnapshotDateUTC", (DateTime)rInstance["SnapshotDateUTC"]);
                var pInstanceID = cmd.Parameters.Add("InstanceID", SqlDbType.Int);
                pInstanceID.Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
                return (Int32)pInstanceID.Value;
            }
        }

        private void updateDB(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Databases") && ds.Tables["Databases"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("Database_Upd", cn);
                    cmd.Parameters.AddWithValue("DB", ds.Tables["Databases"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }


    }
}
