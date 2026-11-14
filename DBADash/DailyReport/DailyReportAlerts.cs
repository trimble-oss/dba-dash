using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBADash.DailyReport
{
    /// <summary>An open alert from Alert.ActiveAlerts, once per instance and alert key (dbo.DailyReport_Alerts_Get)</summary>
    public class DailyReportAlert
    {
        public string Instance { get; init; }
        public string AlertType { get; init; }
        public string AlertKey { get; init; }
        /// <summary>0 Critical, 1-10 High, 11-20 Medium, 21-30 Low (informational alerts aren't read)</summary>
        public int Priority { get; init; }
        public string Message { get; init; }
        public DateTime TriggerUtc { get; init; }
        public bool IsAcknowledged { get; init; }
        public bool IsBlackout { get; init; }

        /// <summary>Listed in the report: not acknowledged and not in a blackout - those are already being dealt with</summary>
        public bool NeedsAttention => !IsAcknowledged && !IsBlackout;

        public string PriorityName => Priority switch
        {
            0 => "Critical",
            <= 10 => "High",
            <= 20 => "Medium",
            _ => "Low"
        };

        /// <summary>How the report shows the alert's severity (badge colour, critical counts): Critical and High are 1, Medium and Low 2</summary>
        public int Status => Priority <= 10 ? 1 : 2;

        /// <summary>What issue history records and compares: 10 Critical, 11 High, 12 Medium, 13 Low - so High to Critical counts as escalating, which Status can't show</summary>
        public int HistoryStatus => Priority switch { 0 => 10, <= 10 => 11, <= 20 => 12, _ => 13 };

        public string Key => IssueHistory.AlertKey(Instance, AlertKey);
    }

    public static class DailyReportAlertRepository
    {
        public static List<DailyReportAlert> Get(string connectionString, string instanceList)
        {
            var alerts = new List<DailyReportAlert>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DailyReport_Alerts_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceList);
            cn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                alerts.Add(new DailyReportAlert
                {
                    Instance = r.GetString(r.GetOrdinal("Instance")),
                    AlertType = r.GetString(r.GetOrdinal("AlertType")),
                    AlertKey = r.GetString(r.GetOrdinal("AlertKey")),
                    Priority = Convert.ToInt32(r["Priority"]),
                    Message = r.IsDBNull(r.GetOrdinal("Message")) ? string.Empty : r.GetString(r.GetOrdinal("Message")),
                    TriggerUtc = r.GetDateTime(r.GetOrdinal("TriggerDate")),
                    IsAcknowledged = r.GetBoolean(r.GetOrdinal("IsAcknowledged")),
                    IsBlackout = r.GetBoolean(r.GetOrdinal("IsBlackout"))
                });
            }
            return alerts;
        }
    }
}
