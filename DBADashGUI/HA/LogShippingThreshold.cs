using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DBADashGUI.LogShipping
{
    public class LogShippingThreshold
    {
        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public int? LatencyWarningThreshold { get; set; } = null;
        public int? LatencyCriticalThreshold { get; set; } = null;
        public int? TimeSinceLastWarningThreshold { get; set; } = null;
        public int? TimeSinceLastCriticalThreshold { get; set; } = null;
        public int NewDatabaseExcludePeriod { get; set; } = 1440;

        public bool Inherited { get; set; }

        public static LogShippingThreshold GetLogShippingThreshold(int InstanceID, int DatabaseID)
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
                        threshold.LatencyCriticalThreshold = (int?)rdr["LatencyCriticalThreshold"];
                        threshold.LatencyWarningThreshold = (int?)rdr["LatencyWarningThreshold"];
                    }
                    if (rdr["TimeSinceLastCriticalThreshold"] != DBNull.Value && rdr["TimeSinceLastWarningThreshold"] != DBNull.Value)
                    {
                        threshold.TimeSinceLastCriticalThreshold = (int?)rdr["TimeSinceLastCriticalThreshold"];
                        threshold.TimeSinceLastWarningThreshold = (int?)rdr["TimeSinceLastWarningThreshold"];
                    }
                    threshold.NewDatabaseExcludePeriod = (int)rdr["NewDatabaseExcludePeriodMin"];
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