using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;

namespace DBADashGUI.DBADashAlerts
{
    public class NotificationChannelGroup
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public int ChannelCount { get; set; }
        public int RuleCount { get; set; }

        public override string ToString() => GroupName;

        public static List<NotificationChannelGroup> GetGroups(string connectionString)
        {
            var groups = new List<NotificationChannelGroup>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannelGroup_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                groups.Add(new NotificationChannelGroup
                {
                    GroupID = rdr.GetInt32("GroupID"),
                    GroupName = rdr.GetString("GroupName"),
                    ChannelCount = rdr.GetInt32("ChannelCount"),
                    RuleCount = rdr.GetInt32("RuleCount")
                });
            }
            return groups;
        }

        public static void Add(string connectionString, string groupName)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannelGroup_Add", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@GroupName", groupName);
            cmd.Parameters.Add(new SqlParameter("@GroupID", SqlDbType.Int) { Direction = ParameterDirection.Output });
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Update(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannelGroup_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@GroupID", GroupID);
            cmd.Parameters.AddWithValue("@GroupName", GroupName);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Delete(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.NotificationChannelGroup_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@GroupID", GroupID);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public static string GetGroupName(IEnumerable<NotificationChannelGroup> groups, int groupID)
            => groupID == 0 ? "(Default)" : groups.FirstOrDefault(g => g.GroupID == groupID)?.GroupName ?? "(Default)";

        public static bool GroupExists(string groupName, string connectionString) => GetGroups(connectionString).Any(g => string.Equals(g.GroupName?.Trim(), groupName, StringComparison.OrdinalIgnoreCase));
    }
}