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

        public static void UpdateShowInSummary(int InstanceID, bool ShowInSummary, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Instance_ShowInSummary_Upd", cn)
            { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("ShowInSummary", ShowInSummary);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateAlias(int instanceID, ref string alias,string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.InstanceAlias_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddWithValue("Alias", alias);
            var pInstanceDisplayName = new SqlParameter("InstanceDisplayName", SqlDbType.NVarChar, 128) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(pInstanceDisplayName);
            cmd.ExecuteNonQuery();
            alias = (string)pInstanceDisplayName.Value; // Returns the display name (set to ConnectionID if alias is NULL)
            
        }

    }
}