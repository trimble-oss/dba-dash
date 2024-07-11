using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADash;
using DocumentFormat.OpenXml;
using Microsoft.Data.SqlClient;

namespace DBADashGUI
{
    internal static class Config
    {
        public static int ClientSummaryCacheDuration;
        public static int DefaultCommandTimeout;
        public static int SummaryCommandTimeout;
        public static int DrivePerformanceMaxDrives;
        public static int SPBehindWarningThreshold;
        public static int SPBehindCriticalThreshold;
        public static int CUBehindWarningThreshold;
        public static int CUBehindCriticalThreshold;
        public static int DaysUntilSupportEndsWarningThreshold;
        public static int DaysUntilSupportEndsCriticalThreshold;
        public static int DaysUntilMainstreamSupportEndsWarningThreshold;
        public static int DaysUntilMainstreamSupportEndsCriticalThreshold;
        public static int BuildReferenceAgeWarningThreshold;
        public static int BuildReferenceAgeCriticalThreshold;
        public static int BuildReferenceUpdateExclusionPeriod;
        public static double IdleWarningThresholdForSleepingSessionWithOpenTran;
        public static double IdleCriticalThresholdForSleepingSessionWithOpenTran;
        public static int CellToolTipMaxLength;

        static Config()
        {
            try
            {
                RefreshConfig();
            }
            catch (SqlException ex) when (ex.Number == 229)
            {
                MessageBox.Show(ex.Message + "\n\nThe App or AppReadOnly role can be used to grant access to the GUI", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting application settings\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void RefreshConfig()
        {
            if (string.IsNullOrEmpty(Common.ConnectionString))
            {
                return;
            }
            Dictionary<string, object> settings = new();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("Settings_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var setting = (string)rdr["SettingName"];
                var value = rdr["SettingValue"].DBNullToNull();
                settings.Add(setting, value);
            }
            ClientSummaryCacheDuration = settings.GetValueAsInt("GUISummaryCacheDuration", 60);
            DefaultCommandTimeout = settings.GetValueAsInt("GUIDefaultCommandTimeout", 120);
            SummaryCommandTimeout = settings.GetValueAsInt("GUISummaryCommandTimeout", DefaultCommandTimeout);
            DrivePerformanceMaxDrives = settings.GetValueAsInt("GUIDrivePerformanceMaxDrives", 8);
            SPBehindWarningThreshold = settings.GetValueAsInt("GUISPBehindWarningThreshold", 1);
            SPBehindCriticalThreshold = settings.GetValueAsInt("GUISPBehindCriticalThreshold", 2);
            CUBehindWarningThreshold = settings.GetValueAsInt("GUICUBehindWarningThreshold", 1);
            CUBehindCriticalThreshold = settings.GetValueAsInt("GUICUBehindCriticalThreshold", 3);
            DaysUntilMainstreamSupportEndsCriticalThreshold = settings.GetValueAsInt("GUIDaysUntilMainstreamSupportEndsCriticalThreshold", 0);
            DaysUntilMainstreamSupportEndsWarningThreshold = settings.GetValueAsInt("GUIDaysUntilMainstreamSupportEndsWarningThreshold", 365);
            DaysUntilSupportEndsCriticalThreshold = settings.GetValueAsInt("GUIDaysUntilSupportEndsCriticalThreshold", 0);
            DaysUntilSupportEndsWarningThreshold = settings.GetValueAsInt("GUIDaysUntilSupportEndsWarningThreshold", 365);
            BuildReferenceAgeWarningThreshold = settings.GetValueAsInt("GUIBuildReferenceAgeWarningThreshold", 45);
            BuildReferenceAgeCriticalThreshold = settings.GetValueAsInt("GUIBuildReferenceAgeCriticalThreshold", 90);
            BuildReferenceUpdateExclusionPeriod = settings.GetValueAsInt("GUIBuildReferenceUpdateExclusionPeriod", 2);
            IdleWarningThresholdForSleepingSessionWithOpenTran = settings.GetValueAsDouble("IdleWarningThresholdForSleepingSessionWithOpenTran", 1);
            IdleCriticalThresholdForSleepingSessionWithOpenTran = settings.GetValueAsDouble("IdleCriticalThresholdForSleepingSessionWithOpenTran", 600);
            CellToolTipMaxLength = settings.GetValueAsInt("GUICellToolTipMaxLength", 1000);
        }

        public static int GetValueAsInt(object value, int defaultValue)
        {
            return int.TryParse(value?.ToString(), out var result) ? result : defaultValue;
        }
    }
}