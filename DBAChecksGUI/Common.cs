using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAChecksGUI
{
    class Common
    {
        public static string DDL(Int64 DDLID,string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                string sql = @"SELECT DDL
FROM dbo.DDL
WHERE DDLID = @DDLID";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("DDLID", DDLID);
                var bDDL = (byte[])cmd.ExecuteScalar();

                return DBAChecks.SchemaSnapshotDB.Unzip(bDDL);

            }
        }

        public static DataTable ConvertUTCToLocal(ref DataTable dt,List<string>convertCols=null)
        {
            List<Int32> convertColsIdx = new List<int>();
            if (convertCols == null || convertCols.Count == 0) {
                convertCols = new List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    if ( col.DataType == typeof(DateTime))
                    {
                        convertColsIdx.Add(col.Ordinal);
                    }
                }
            }
            else
            {
                foreach(string col in convertCols)
                {
                    convertColsIdx.Add(dt.Columns[col].Ordinal);
                }
            }
            if (convertColsIdx.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    foreach(var col in convertColsIdx)
                    {
                        if (row[col] != DBNull.Value)
                        {
                            row[col] = ((DateTime)row[col]).ToLocalTime();
                        }
                    }
                }
            }
            return dt;

        }
    }
}
