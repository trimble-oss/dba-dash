using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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

        public bool Inherited { get; set; }

        public static LogShippingThreshold GetLogShippingThreshold(Int32 InstanceID,Int32 DatabaseID,string connectionString)
        {
            LogShippingThreshold threshold = new LogShippingThreshold();
            threshold.InstanceID = InstanceID;
            threshold.DatabaseID = DatabaseID;
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.LogRestoreThresholds_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
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

        public void Save(string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.LogRestoreThresholds_Upd", cn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                if (LatencyWarningThreshold != null) { cmd.Parameters.AddWithValue("LatencyWarning", LatencyWarningThreshold); }
                if (LatencyCriticalThreshold != null) { cmd.Parameters.AddWithValue("LatencyCritical", LatencyCriticalThreshold); }
                if (TimeSinceLastWarningThreshold != null) { cmd.Parameters.AddWithValue("TimeSinceLastWarning", TimeSinceLastWarningThreshold); }
                if (TimeSinceLastCriticalThreshold != null) { cmd.Parameters.AddWithValue("TimeSinceLastCritical", TimeSinceLastCriticalThreshold); }
                cmd.Parameters.AddWithValue("Inherit", Inherited);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
