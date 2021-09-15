using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DBADashGUI.LogShipping
{
    public class LogShippingThreshold
    {
        public Int32 InstanceID { get; set; }
        public Int32 DatabaseID { get; set; }
        public Int32? LatencyWarningThreshold { get; set; } = null;
        public Int32? LatencyCriticalThreshold { get; set; } = null;
        public Int32? TimeSinceLastWarningThreshold { get; set; } = null;
        public Int32? TimeSinceLastCriticalThreshold { get; set; } = null;
        public Int32 NewDatabaseExcludePeriod { get; set; } = 1440;

        public bool Inherited { get; set; }

        public static LogShippingThreshold GetLogShippingThreshold(Int32 InstanceID,Int32 DatabaseID)
        {
            LogShippingThreshold threshold = new LogShippingThreshold
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID
            };

            using (var cn = new SqlConnection(Common.ConnectionString))           
            using (SqlCommand cmd = new SqlCommand("dbo.LogRestoreThresholds_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    if (rdr["LatencyCriticalThreshold"] != DBNull.Value && rdr["LatencyWarningThreshold"] != DBNull.Value)
                    {
                        threshold.LatencyCriticalThreshold = (Int32?)rdr["LatencyCriticalThreshold"];
                        threshold.LatencyWarningThreshold = (Int32?)rdr["LatencyWarningThreshold"];
                    }
                    if (rdr["TimeSinceLastCriticalThreshold"] != DBNull.Value && rdr["TimeSinceLastWarningThreshold"] != DBNull.Value)
                    {
                        threshold.TimeSinceLastCriticalThreshold = (Int32?)rdr["TimeSinceLastCriticalThreshold"];
                        threshold.TimeSinceLastWarningThreshold = (Int32?)rdr["TimeSinceLastWarningThreshold"];
                    }
                    threshold.NewDatabaseExcludePeriod = (Int32)rdr["NewDatabaseExcludePeriodMin"];
                    threshold.Inherited = false;
                    if (rdr.Read())
                    {
                        throw new Exception("More than 1 row returned");
                    }
                }
                else
                {
                    threshold.Inherited = true;
                }
            }
            
            return threshold;
        }

        public void Save()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))         
            using (SqlCommand cmd = new SqlCommand("dbo.LogRestoreThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                if (LatencyWarningThreshold != null) { cmd.Parameters.AddWithValue("LatencyWarning", LatencyWarningThreshold); }
                if (LatencyCriticalThreshold != null) { cmd.Parameters.AddWithValue("LatencyCritical", LatencyCriticalThreshold); }
                if (TimeSinceLastWarningThreshold != null) { cmd.Parameters.AddWithValue("TimeSinceLastWarning", TimeSinceLastWarningThreshold); }
                if (TimeSinceLastCriticalThreshold != null) { cmd.Parameters.AddWithValue("TimeSinceLastCritical", TimeSinceLastCriticalThreshold); }
                cmd.Parameters.AddWithValue("Inherit", Inherited);
                cmd.Parameters.AddWithValue("NewDatabaseExcludePeriodMin", NewDatabaseExcludePeriod);
                cmd.ExecuteNonQuery();
            }           
        }
    }
}
