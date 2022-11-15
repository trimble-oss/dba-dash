using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DBADashGUI.Checks
{
    public class MemoryDumpThresholds
    {
        public int? MemoryDumpWarningThresholdHrs { get; set; }
        public int? MemoryDumpCriticalThresholdHrs { get; set; }
        public DateTime? MemoryDumpAckDate { get; init; }

        public static MemoryDumpThresholds GetMemoryDumpThresholds()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryDumpThresholds_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                var pWarning = new SqlParameter() { ParameterName = "MemoryDumpWarningThresholdHrs", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
                var pCritical = new SqlParameter() { ParameterName = "MemoryDumpCriticalThresholdHrs", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.Int };
                var pAckDate = new SqlParameter() { ParameterName = "MemoryDumpAckDate", Direction = ParameterDirection.Output, SqlDbType = SqlDbType.DateTime };

                cmd.Parameters.Add(pWarning);
                cmd.Parameters.Add(pCritical);
                cmd.Parameters.Add(pAckDate);

                cmd.ExecuteNonQuery();
                var thres = new MemoryDumpThresholds()
                {
                    MemoryDumpWarningThresholdHrs = pWarning.Value == DBNull.Value ? null : (int)pWarning.Value,
                    MemoryDumpCriticalThresholdHrs = pCritical.Value == DBNull.Value ? null : (int)pCritical.Value,
                    MemoryDumpAckDate = pAckDate.Value == DBNull.Value ? null : (DateTime)pAckDate.Value
                };
                return thres;
            }
        }

        public static void Acknowledge(bool clear = false)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.AcknowledgeMemoryDumps", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("Clear", clear);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void Save()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryDumpThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddRange(new SqlParameter[] {
                    new SqlParameter() { ParameterName = "MemoryDumpWarningThresholdHrs", SqlDbType = SqlDbType.Int,Value=MemoryDumpWarningThresholdHrs==null? DBNull.Value : MemoryDumpWarningThresholdHrs },
                    new SqlParameter() { ParameterName = "MemoryDumpCriticalThresholdHrs", SqlDbType = SqlDbType.Int, Value = MemoryDumpCriticalThresholdHrs == null ? DBNull.Value : MemoryDumpCriticalThresholdHrs }
                }
                );
                cmd.ExecuteNonQuery();
            }
        }
    }
}
