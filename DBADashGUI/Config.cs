﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
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
        public static int CPUCriticalThreshold;
        public static int CPUWarningThreshold;
        public static int CPULowThreshold;
        public static int ReadLatencyCriticalThreshold;
        public static int ReadLatencyWarningThreshold;
        public static int ReadLatencyGoodThreshold;
        public static int WriteLatencyCriticalThreshold;
        public static int WriteLatencyWarningThreshold;
        public static int WriteLatencyGoodThreshold;
        public static int MinIOPsThreshold;
        public static int CriticalWaitCriticalThreshold;
        public static int CriticalWaitWarningThreshold;
        public static int? HardDeleteThresholdDays;
        public static int SlowQueriesDrillDownMaxRows;
        public static int CollectionTriggerWarningLimit;
        public static int CollectionTriggerLimit;
        public static int AlertAutoCloseThresholdMins;
        public static int AlertMaxNotificationCount;
        public static string InstanceMetadataTags;

        static Config()
        {
            try
            {
                RefreshConfig();
            }
            catch (SqlException ex) when (ex.Number == 229)
            {
                CommonShared.ShowExceptionDialog(ex, "Permissions error", default,
                    TaskDialogIcon.ShieldWarningYellowBar,
                    "The 'App' or 'AppReadOnly' roles can be used to grant access to the GUI");
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error getting application settings");
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
                var setting = ((string)rdr["SettingName"]).Trim();
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
            CPUCriticalThreshold = settings.GetValueAsInt("CPUCriticalThreshold", -1);
            CPUWarningThreshold = settings.GetValueAsInt("CPUWarningThreshold", -1);
            CPULowThreshold = settings.GetValueAsInt("CPULowThreshold", -1);
            ReadLatencyCriticalThreshold = settings.GetValueAsInt("ReadLatencyCriticalThreshold", -1);
            ReadLatencyWarningThreshold = settings.GetValueAsInt("ReadLatencyWarningThreshold", -1);
            ReadLatencyGoodThreshold = settings.GetValueAsInt("ReadLatencyGoodThreshold", -1);
            WriteLatencyCriticalThreshold = settings.GetValueAsInt("WriteLatencyCriticalThreshold", -1);
            WriteLatencyWarningThreshold = settings.GetValueAsInt("WriteLatencyWarningThreshold", -1);
            WriteLatencyGoodThreshold = settings.GetValueAsInt("WriteLatencyGoodThreshold", -1);
            MinIOPsThreshold = settings.GetValueAsInt("MinIOPsThreshold", -1);
            CriticalWaitCriticalThreshold = settings.GetValueAsInt("CriticalWaitCriticalThreshold", -1);
            CriticalWaitWarningThreshold = settings.GetValueAsInt("CriticalWaitWarningThreshold", -1);
            HardDeleteThresholdDays = settings.GetValueAsNullableInt("HardDeleteThresholdDays");
            SlowQueriesDrillDownMaxRows = settings.GetValueAsInt("GUISlowQueriesDrillDownMaxRows", 1000);
            CollectionTriggerLimit = settings.GetValueAsInt("CollectionTriggerLimit", 500);
            CollectionTriggerWarningLimit = settings.GetValueAsInt("CollectionTriggerWarningLimit", 100);
            AlertAutoCloseThresholdMins = settings.GetValueAsInt("AlertAutoCloseThresholdMins", 1440);
            AlertMaxNotificationCount = settings.GetValueAsInt("AlertMaxNotificationCount", 6);
            InstanceMetadataTags = settings.GetValueAsString("InstanceMetadataTags", string.Empty);
        }

        public static void ResetDefaults()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("Settings_Add", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("Reset", true);
            cn.Open();
            cmd.ExecuteNonQuery();
            RefreshConfig();
        }

        public static int GetValueAsInt(object value, int defaultValue)
        {
            return int.TryParse(value?.ToString(), out var result) ? result : defaultValue;
        }


    }
}