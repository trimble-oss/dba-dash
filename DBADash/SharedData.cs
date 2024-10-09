using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace DBADash
{
    public class SharedData
    {

        public static void MarkInstanceDeleted(string connectionID, string connectionString, bool isActive = false)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Instance_Del", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("ConnectionID", connectionID);
            cmd.Parameters.AddWithValue("IsActive", isActive);
            cmd.ExecuteNonQuery();
        }

        public static void MarkInstanceDeleted(int InstanceID, string connectionString, bool IsActive)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Instance_Del", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("IsActive", IsActive);
            cmd.ExecuteNonQuery();
        }
    }
}