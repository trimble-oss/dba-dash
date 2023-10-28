using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DBADashGUI.DBFiles
{
    public class FileThreshold
    {
        public bool Inherited { get; set; }
        public int InstanceID { get; set; }
        public int DatabaseID { get; set; }
        public int DataSpaceID { get; set; }

        public decimal WarningThreshold { get; set; }
        public decimal CriticalThreshold { get; set; }

        public bool ZeroAuthgrowthOnly { get; set; }

        public FileCheckTypeEnum FileCheckType { get; set; }

        public decimal PctMaxSizeWarningThreshold { get; set; }

        public decimal PctMaxSizeCriticalThreshold { get; set; }

        public bool PctMaxCheckEnabled { get; set; }

        public enum FileCheckTypeEnum
        {
            Percent,
            MB,
            None
        }

        public char FileCheckTypeChar
        {
            get
            {
                if (WarningThreshold <= 0 && CriticalThreshold <= 0)
                {
                    return '-';
                }
                if (FileCheckType == FileCheckTypeEnum.MB)
                {
                    return 'M';
                }
                if (FileCheckType == FileCheckTypeEnum.Percent)
                {
                    return '%';
                }
                else
                {
                    return '-';
                }
            }
            set
            {
                if (value == 'M')
                {
                    FileCheckType = FileCheckTypeEnum.MB;
                }
                else if (value == '%')
                {
                    FileCheckType = FileCheckTypeEnum.Percent;
                }
                else
                {
                    FileCheckType = FileCheckTypeEnum.None;
                }
            }
        }

        public FileThreshold GetInheritedThreshold()
        {
            if (!this.Inherited)
            {
                return this;
            }
            int _DataSpaceID = DataSpaceID == 0 ? 0 : -1;
            int _DatabaseID = this.DataSpaceID is (-1) or 0 ? -1 : this.DatabaseID;
            int _InstanceID = this.DatabaseID == -1 ? -1 : this.InstanceID;
            var threshold = GetFileThreshold(_InstanceID, _DatabaseID, _DataSpaceID);
            if (threshold.Inherited && InstanceID != -1)
            {
                return threshold.GetInheritedThreshold();
            }
            else
            {
                return threshold;
            }
        }

        public static FileThreshold GetFileThreshold(int InstanceID, int DatabaseID, int DataSpaceID)
        {
            FileThreshold threshold = new()
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID,
                DataSpaceID = DataSpaceID,
            };
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new("dbo.DBFileThresholds_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("DataSpaceID", DataSpaceID);
                using SqlDataReader rdr = cmd.ExecuteReader();

                if (rdr.Read())
                {
                    if (rdr["FreeSpaceCriticalThreshold"] != DBNull.Value && rdr["FreeSpaceWarningThreshold"] != DBNull.Value)
                    {
                        threshold.CriticalThreshold = (decimal)rdr["FreeSpaceCriticalThreshold"];
                        threshold.WarningThreshold = (decimal)rdr["FreeSpaceWarningThreshold"];
                    }
                    if (rdr["PctMaxSizeWarningThreshold"] != DBNull.Value && rdr["PctMaxSizeCriticalThreshold"] != DBNull.Value)
                    {
                        threshold.PctMaxSizeCriticalThreshold = (decimal)rdr["PctMaxSizeCriticalThreshold"];
                        threshold.PctMaxSizeWarningThreshold = (decimal)rdr["PctMaxSizeWarningThreshold"];
                        threshold.PctMaxCheckEnabled = true;
                    }
                    else
                    {
                        threshold.PctMaxCheckEnabled = false;
                    }
                    threshold.FileCheckTypeChar = char.Parse((string)rdr["FreeSpaceCheckType"]);
                    threshold.Inherited = false;
                    threshold.ZeroAuthgrowthOnly = (bool)rdr["FreeSpaceCheckZeroAutogrowthOnly"];
                    if (rdr.Read())
                    {
                        throw new Exception("More than 1 row returned");
                    }
                }
                else
                {
                    threshold.Inherited = InstanceID != -1;
                }
            }

            return threshold;
        }

        public void UpdateThresholds()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBFileThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("DataSpaceID", DataSpaceID);
                cmd.Parameters.AddWithValue("CheckType", Inherited ? 'I' : FileCheckTypeChar);

                if (WarningThreshold <= 0 || FileCheckType == FileCheckTypeEnum.None || Inherited)
                {
                    cmd.Parameters.AddWithValue("Warning", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("Warning", WarningThreshold);
                }
                if (CriticalThreshold <= 0 || FileCheckType == FileCheckTypeEnum.None || Inherited)
                {
                    cmd.Parameters.AddWithValue("Critical", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("Critical", CriticalThreshold);
                }
                if (PctMaxCheckEnabled)
                {
                    cmd.Parameters.AddWithValue("PctMaxSizeWarningThreshold", PctMaxSizeWarningThreshold);
                    cmd.Parameters.AddWithValue("PctMaxSizeCriticalThreshold", PctMaxSizeCriticalThreshold);
                }
                else
                {
                    cmd.Parameters.AddWithValue("PctMaxSizeWarningThreshold", DBNull.Value);
                    cmd.Parameters.AddWithValue("PctMaxSizeCriticalThreshold", DBNull.Value);
                }
                cmd.Parameters.AddWithValue("FreeSpaceCheckZeroAutogrowthOnly", ZeroAuthgrowthOnly);
                cmd.ExecuteNonQuery();
            }
        }
    }
}