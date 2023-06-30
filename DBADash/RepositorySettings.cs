using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Microsoft.Data.SqlClient;
using Octokit;

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