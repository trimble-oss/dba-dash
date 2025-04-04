﻿using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DBADashGUI.Backups
{
    public class BackupThresholds
    {
        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public int? FullWarning { get; set; }
        public int? FullCritical { get; set; }
        public int? DiffWarning { get; set; }
        public int? DiffCritical { get; set; }
        public int? LogWarning { get; set; }
        public int? LogCritical { get; set; }

        public int MinimumAge { get; set; }

        public bool Inherit { get; set; }
        public bool UsePartial { get; set; }
        public bool UseFG { get; set; }

        public string ExcludedDBs { get; set; } = string.Empty;

        public bool ConsiderSnapshotBackups { get; set; } = true;

        public bool ConsiderCopyOnlyBackups { get; set; } = true;

        public bool ConsiderFullBackupWithDiffThreshold { get; set; } = true;

        public static BackupThresholds GetThresholds(int InstanceID, int DatabaseID)
        {
            BackupThresholds thresholds = new();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.BackupThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                if (rdr["FullBackupCriticalThreshold"] != DBNull.Value && rdr["FullBackupWarningThreshold"] != DBNull.Value)
                {
                    thresholds.FullCritical = (int)rdr["FullBackupCriticalThreshold"];
                    thresholds.FullWarning = (int)rdr["FullBackupWarningThreshold"];
                }
                if (rdr["DiffBackupCriticalThreshold"] != DBNull.Value && rdr["DiffBackupWarningThreshold"] != DBNull.Value)
                {
                    thresholds.DiffCritical = (int)rdr["DiffBackupCriticalThreshold"];
                    thresholds.DiffWarning = (int)rdr["DiffBackupWarningThreshold"];
                }

                if (rdr["LogBackupCriticalThreshold"] != DBNull.Value && rdr["LogBackupWarningThreshold"] != DBNull.Value)
                {
                    thresholds.LogCritical = (int)rdr["LogBackupCriticalThreshold"];
                    thresholds.LogWarning = (int)rdr["LogBackupWarningThreshold"];
                }
                thresholds.MinimumAge = rdr["MinimumAge"] == DBNull.Value ? 0 : (int)rdr["MinimumAge"];
                thresholds.UsePartial = (bool)rdr["ConsiderPartialBackups"];
                thresholds.UseFG = (bool)rdr["ConsiderFGBackups"];
                thresholds.ExcludedDBs = Convert.ToString(rdr["ExcludedDatabases"]);
                thresholds.ConsiderCopyOnlyBackups = (bool)rdr["ConsiderCopyOnlyBackups"];
                thresholds.ConsiderSnapshotBackups = (bool)rdr["ConsiderSnapshotBackups"];
                thresholds.ConsiderFullBackupWithDiffThreshold = (bool)rdr["ConsiderFullBackupWithDiffThreshold"];
            }
            else
            {
                thresholds.Inherit = true;
            }

            return thresholds;
        }

        public void Save()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.BackupThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
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
            cmd.Parameters.AddWithValue("ExcludedDatabases", ExcludedDBs);
            if (MinimumAge > 0) { cmd.Parameters.AddWithValue("MinimumAge", MinimumAge); }

            cmd.Parameters.AddWithValue("ConsiderCopyOnlyBackups", ConsiderCopyOnlyBackups);
            cmd.Parameters.AddWithValue("ConsiderSnapshotBackups", ConsiderSnapshotBackups);
            cmd.Parameters.AddWithValue("ConsiderFullBackupWithDiffThreshold", ConsiderFullBackupWithDiffThreshold);
            cmd.ExecuteNonQuery();
        }
    }
}