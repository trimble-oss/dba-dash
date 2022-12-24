using Microsoft.Data.SqlClient;
using System;
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

        public static LogShippingThreshold GetLogShippingThreshold(Int32 InstanceID, Int32 DatabaseID)
        {
            LogShippingThreshold threshold = new()
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID
            };

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new("dbo.LogRestoreThresholds_Get", cn) { CommandType = CommandType.StoredProcedure })
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
            using (SqlCommand cmd = new("dbo.LogRestoreThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithNullableValue("LatencyWarning", LatencyWarningThreshold);
                cmd.Parameters.AddWithNullableValue("LatencyCritical", LatencyCriticalThreshold);
                cmd.Parameters.AddWithNullableValue("TimeSinceLastWarning", TimeSinceLastWarningThreshold);
                cmd.Parameters.AddWithNullableValue("TimeSinceLastCritical", TimeSinceLastCriticalThreshold);
                cmd.Parameters.AddWithValue("Inherit", Inherited);
                cmd.Parameters.AddWithValue("NewDatabaseExcludePeriodMin", NewDatabaseExcludePeriod);
                cmd.ExecuteNonQuery();
            }
        }
    }
}