using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.DBFiles
{
    public class FileThreshold
    {
        public bool Inherited { get; set; }
        public Int32 InstanceID { get; set; }
        public Int32 DatabaseID { get; set; }
        public Int32 DataSpaceID { get; set; }

        public decimal WarningThreshold { get; set; }
        public decimal CriticalThreshold { get; set; }

        public string ConnectionString { get; set; }

        public FileCheckTypeEnum FileCheckType { get; set; }
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

        public static FileThreshold GetFileThreshold(Int32 InstanceID, Int32 DatabaseID,Int32 DataSpaceID,string connectionString)
        {
            FileThreshold threshold = new FileThreshold();
            threshold.InstanceID = InstanceID;
            threshold.DatabaseID = DatabaseID;
            threshold.DataSpaceID = DataSpaceID;
            threshold.ConnectionString = connectionString;
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DBFileThresholds_Get", cn);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                cmd.Parameters.AddWithValue("DataSpaceID", DataSpaceID);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    if (rdr["FreeSpaceCriticalThreshold"] != DBNull.Value && rdr["FreeSpaceWarningThreshold"] != DBNull.Value)
                    {
                        threshold.CriticalThreshold = (decimal)rdr["FreeSpaceCriticalThreshold"];
                        threshold.WarningThreshold = (decimal)rdr["FreeSpaceWarningThreshold"];
                    }
                    threshold.FileCheckTypeChar = char.Parse((string)rdr["FreeSpaceCheckType"]);
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

        public void UpdateThresholds()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                var cmd = new SqlCommand("dbo.DBFileThresholds_Upd", cn);
                cmd.CommandType = CommandType.StoredProcedure;
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
                cmd.ExecuteNonQuery();
                var rdr = cmd.ExecuteNonQuery();     
            }
        }

    }
}
