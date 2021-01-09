using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Backups
{
   public class BackupThresholds
    {
        public Int32 InstanceID { get; set; }
        public Int32 DatabaseID { get; set; }
        public Int32? FullWarning { get; set; } = null;
        public Int32? FullCritical { get; set; } = null;
        public Int32? DiffWarning { get; set; } = null;
        public Int32? DiffCritical { get; set; } = null;
        public Int32? LogWarning { get; set; } = null;
        public Int32? LogCritical { get; set; } = null;


        public bool Inherit { get; set; } = false;
        public bool UsePartial { get; set; } = false;
        public bool UseFG { get; set; } = false;

        

        public static BackupThresholds GetThresholds(Int32 InstanceID,Int32 DatabaseID,string connectionString)
        {
            BackupThresholds thresholds = new BackupThresholds();
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.BackupThresholds_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    if (rdr["FullBackupCriticalThreshold"] != DBNull.Value && rdr["FullBackupWarningThreshold"] != DBNull.Value)
                    {
                       
                        thresholds.FullCritical = (Int32)rdr["FullBackupCriticalThreshold"];
                        thresholds.FullWarning = (Int32)rdr["FullBackupWarningThreshold"];
                    }
                    if (rdr["DiffBackupCriticalThreshold"] != DBNull.Value && rdr["DiffBackupWarningThreshold"] != DBNull.Value)
                    {
                        thresholds.DiffCritical = (Int32)rdr["DiffBackupCriticalThreshold"];
                        thresholds.DiffWarning =(Int32)rdr["DiffBackupWarningThreshold"];                 
                    }

                    if (rdr["LogBackupCriticalThreshold"] != DBNull.Value && rdr["LogBackupWarningThreshold"] != DBNull.Value)
                    {
                        thresholds.LogCritical = (Int32)rdr["LogBackupCriticalThreshold"];
                        thresholds.LogWarning = (Int32)rdr["LogBackupWarningThreshold"];
                    }
                    thresholds.UsePartial = (bool)rdr["ConsiderPartialBackups"];
                    thresholds.UseFG = (bool)rdr["ConsiderFGBackups"];
                }
                else
                {
                    thresholds.Inherit = true;
                }

            }
            return thresholds;
        }

        public void Save(string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.BackupThresholds_Upd", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                if (FullWarning != null) { cmd.Parameters.AddWithValue("FullWarning", FullWarning); }
                if (FullCritical != null) { cmd.Parameters.AddWithValue("FullCritical", FullCritical); }
                if (DiffWarning != null) { cmd.Parameters.AddWithValue("DiffWarning", DiffWarning); }
                if (DiffCritical != null) { cmd.Parameters.AddWithValue("DiffCritical", DiffCritical); }
                if (LogWarning != null) { cmd.Parameters.AddWithValue("LogWarning", LogWarning); }
                if (LogCritical != null) { cmd.Parameters.AddWithValue("LogCritical", LogCritical); }
                cmd.Parameters.AddWithValue("UseFG", UseFG);
                cmd.Parameters.AddWithValue("UsePartial", UsePartial);
                cmd.Parameters.AddWithValue("Inherit", Inherit);
                cmd.ExecuteNonQuery();
            }
        }

    }
}
