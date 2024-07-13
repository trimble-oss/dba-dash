using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DBADash
{
    public class RepositorySettings
    {
        public static object GetSetting(string SettingName, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Settings_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("SettingName", SettingName);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return rdr["SettingValue"] == DBNull.Value ? null : rdr["SettingValue"];
            }
            else
            {
                return null;
            }
        }

        public static void UpdateSetting(string SettingName, object SettingValue, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Settings_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("SettingName", SettingName);
            cmd.Parameters.AddWithValue("SettingValue", SettingValue ?? DBNull.Value);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public static int? GetIntSetting(string SettingName, string connectionString)
        {
            return (int?)GetSetting(SettingName, connectionString);
        }

        public static int? GetStringSetting(string SettingName, string connectionString)
        {
            return (int?)GetSetting(SettingName, connectionString);
        }

        public static int? GetSummaryRefreshInterval(string connectionString)
        {
            return GetIntSetting("SummaryRefreshInterval", connectionString);
        }
    }
}