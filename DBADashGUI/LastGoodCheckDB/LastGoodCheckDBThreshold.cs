using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
namespace DBADashGUI.LastGoodCheckDB
{
   public class LastGoodCheckDBThreshold
    {
        public Int32 InstanceID { get; set; }
        public Int32 DatabaseID { get; set; }
        public Int32? WarningThreshold { get; set; }
        public Int32? CriticalThreshold { get; set; }
        public bool Inherit { get; set; }

        public string ConnectionString { get; set; }

        public static LastGoodCheckDBThreshold GetLastGoodCheckDBThreshold(string connectionString,Int32 InstanceID,Int32 DatabaseID)
        {
            var threshold = new LastGoodCheckDBThreshold();
            threshold.InstanceID = InstanceID;
            threshold.DatabaseID = DatabaseID;
            threshold.ConnectionString = connectionString;
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.LastGoodCheckDBThresholds_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    threshold.WarningThreshold = rdr["WarningThresholdHrs"] == DBNull.Value ? null : (Int32?)rdr["WarningThresholdHrs"];
                    threshold.CriticalThreshold = rdr["CriticalThresholdHrs"] == DBNull.Value ? null : (Int32?)rdr["CriticalThresholdHrs"];
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
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.LastGoodCheckDBThresholds_Upd", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("WarningThreshold", WarningThreshold);
                cmd.Parameters.AddWithValue("CriticalThreshold", CriticalThreshold);
                cmd.Parameters.AddWithValue("Inherit", Inherit);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
