using Microsoft.Data.SqlClient;
using System.Data;

namespace DBADash
{
    public class SharedData
    {
        private const int SoftDeleteCommandTimeout = 30;
        private const int HardDeleteCommandTimeout = 3600;

        private static void InstanceDelete(string connectionID, string connectionString, bool isActive = false, bool hardDelete = false)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Instance_Del", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = hardDelete ? HardDeleteCommandTimeout : SoftDeleteCommandTimeout };
            cn.Open();
            cmd.Parameters.AddWithValue("ConnectionID", connectionID);
            cmd.Parameters.AddWithValue("IsActive", isActive);
            cmd.Parameters.AddWithValue("HardDelete", hardDelete);
            cmd.ExecuteNonQuery();
        }

        private static void InstanceDelete(int InstanceID, string connectionString, bool IsActive = false, bool hardDelete = false)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Instance_Del", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = hardDelete ? HardDeleteCommandTimeout : SoftDeleteCommandTimeout };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("IsActive", IsActive);
            cmd.Parameters.AddWithValue("HardDelete", hardDelete);
            cmd.ExecuteNonQuery();
        }

        public static void MarkInstanceDeleted(string connectionID, string connectionString) => InstanceDelete(connectionID, connectionString, false, false);

        public static void MarkInstanceDeleted(int instanceID, string connectionString) => InstanceDelete(instanceID, connectionString, false, false);

        public static void RestoreInstance(int instanceID, string connectionString) => InstanceDelete(instanceID, connectionString, true, false);

        public static void RestoreInstance(string connectionID, string connectionString) => InstanceDelete(connectionID, connectionString, true, false);

        public static void HardDeleteInstance(int instanceID, string connectionString) => InstanceDelete(instanceID, connectionString, false, true);

        public static void HardDeleteInstance(string connectionID, string connectionString) => InstanceDelete(connectionID, connectionString, false, true);

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

        public static void UpdateAlias(int instanceID, ref string alias, string connectionString)
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