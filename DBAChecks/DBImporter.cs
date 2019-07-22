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
         
            instanceID = updateInstance(connectionString,rInstance);
      
        
            updateDB(connectionString, instanceID, snapshotDate, Data);
            update(connectionString, instanceID, snapshotDate, Data,"Drives");
            update(connectionString, instanceID, snapshotDate, Data, "ServerProperties");
            update(connectionString, instanceID, snapshotDate, Data,"Backups");
            update(connectionString, instanceID, snapshotDate, Data,"AgentJobs");
            update(connectionString, instanceID, snapshotDate, Data,"LogRestores");
            update(connectionString, instanceID, snapshotDate, Data,"DBFiles");
            update(connectionString, instanceID, snapshotDate, Data,"DBConfig");
            update(connectionString, instanceID, snapshotDate, Data, "Corruption");
            update(connectionString, instanceID, snapshotDate, Data,"DatabasesHADR");
            update(connectionString, instanceID, snapshotDate, Data, "SysConfig");
            updateServerExtraProperties(connectionString, instanceID, snapshotDate, Data);
            InsertErrors(connectionString, instanceID, snapshotDate, Data);
        }

        private void updateServerExtraProperties(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("ServerExtraProperties") && ds.Tables["ServerExtraProperties"].Rows.Count==1)
            {
                var r = ds.Tables["ServerExtraProperties"].Rows[0];
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("ServerExtraProperties_Upd", cn);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.Parameters.AddWithValue("ActivePowerPlanGUID", r["ActivePowerPlanGUID"]);
                    cmd.Parameters.AddWithValue("ActivePowerPlan", r["ActivePowerPlan"]);
                    cmd.Parameters.AddWithValue("ProcessorNameString", r["ProcessorNameString"]);
                    cmd.Parameters.AddWithValue("SystemManufacturer", r["SystemManufacturer"]);
                    cmd.Parameters.AddWithValue("SystemProductName", r["SystemProductName"]);
                    cmd.Parameters.AddWithValue("IsAgentRunning", r["IsAgentRunning"]);
                    cmd.Parameters.AddWithValue("InstantFileInitializationEnabled", r["InstantFileInitializationEnabled"]);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }

            private void update(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds,string TableName)
        {
            if (ds.Tables.Contains(TableName))
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand(TableName + "_Upd", cn);
                    if (ds.Tables[TableName].Rows.Count > 0)
                    {
                        cmd.Parameters.AddWithValue(TableName, ds.Tables[TableName]);
                    }
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                }
            }
        }


            private void InsertErrors(string connectionString, Int32 instanceID, DateTime SnapshotDate, DataSet ds)
        {
            if (ds.Tables.Contains("Errors") && ds.Tables["Errors"].Rows.Count > 0)
            {
                var cn = new SqlConnection(connectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("CollectionErrorLog_Add", cn);
                    cmd.Parameters.AddWithValue("Errors", ds.Tables["Errors"]);
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                    cmd.Parameters.AddWithValue("ErrorDate", SnapshotDate);
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
                cmd.Parameters.AddWithValue("SnapshotDate", (DateTime)rInstance["SnapshotDateUTC"]);
                cmd.Parameters.AddWithValue("AgentHostName", (string)rInstance["AgentHostName"]);

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
                    var dtDB = ds.Tables["Databases"];
                    if (dtDB.Columns["owner_sid"].DataType == typeof(string))
                    {
                        Int32 pos = dtDB.Columns["owner_sid"].Ordinal;
                        dtDB.Columns["owner_sid"].ColumnName = "owner_sid_string";
                        
                        var newCol = dtDB.Columns.Add("owner_sid", typeof(byte[]));
                       
                        foreach(DataRow r in dtDB.Rows)
                        {
                            r["owner_sid"] = Convert.FromBase64String((string)r["owner_sid_string"]);
                        }
                        dtDB.Columns.Remove("owner_sid_string");
                        newCol.SetOrdinal(pos);

                    }


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
