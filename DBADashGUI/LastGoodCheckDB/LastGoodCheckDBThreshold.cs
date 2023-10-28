using Microsoft.Data.SqlClient;
using System;
namespace DBADashGUI.LastGoodCheckDB
{
    public class LastGoodCheckDBThreshold
    {
        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public int? WarningThreshold { get; set; }
        public int? CriticalThreshold { get; set; }
        public int MinimumAge { get; set; }
        public bool Inherit { get; set; }

        public string ExcludedDatabases { get; set; }

        public static LastGoodCheckDBThreshold GetLastGoodCheckDBThreshold(int InstanceID, int DatabaseID)
        {
            var threshold = new LastGoodCheckDBThreshold
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID,
            };
            SqlConnection cn = new(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new("dbo.LastGoodCheckDBThresholds_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    threshold.WarningThreshold = rdr["WarningThresholdHrs"] == DBNull.Value ? null : (int?)rdr["WarningThresholdHrs"];
                    threshold.CriticalThreshold = rdr["CriticalThresholdHrs"] == DBNull.Value ? null : (int?)rdr["CriticalThresholdHrs"];
                    threshold.MinimumAge = rdr["MinimumAge"] == DBNull.Value ? 0 : (int)rdr["MinimumAge"];
                    threshold.ExcludedDatabases = rdr["ExcludedDatabases"] == DBNull.Value ? string.Empty : (string)rdr["ExcludedDatabases"];
                    threshold.Inherit = false;
                }
                else
                {
                    threshold.Inherit = true;
                }
            }
            return threshold;
        }

        public void Save()
        {
            SqlConnection cn = new(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new("dbo.LastGoodCheckDBThresholds_Upd", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("WarningThreshold", WarningThreshold);
                cmd.Parameters.AddWithValue("CriticalThreshold", CriticalThreshold);
                cmd.Parameters.AddWithValue("Inherit", Inherit);
                if (ExcludedDatabases.Trim() != string.Empty)
                {
                    cmd.Parameters.AddWithValue("ExcludedDatabases", ExcludedDatabases);
                }
                if (MinimumAge >= 0)
                {
                    cmd.Parameters.AddWithValue("MinimumAge", MinimumAge);
                }
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
