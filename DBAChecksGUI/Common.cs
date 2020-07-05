using System;
using System.Collections.Generic;
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
    }
}
