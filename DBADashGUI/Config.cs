using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADash;

namespace DBADashGUI
{
    internal static class Config
    {
        private static readonly Lazy<int> _clientSummaryCacheDuration = new Lazy<int>(() => RepositorySettings.GetIntSetting("GUISummaryCacheDuration", Common.ConnectionString) ?? 60);

        private static readonly Lazy<int> _defaultCommandTimeout = new Lazy<int>(() => RepositorySettings.GetIntSetting("GUIDefaultCommandTimeout", Common.ConnectionString) ?? 60);

        private static readonly Lazy<int> _summaryCommandTimeout = new Lazy<int>(() => RepositorySettings.GetIntSetting("GUISummaryCommandTimeout", Common.ConnectionString) ?? _defaultCommandTimeout.Value);

        private static readonly Lazy<int> _drivePerformanceMaxDrives = new Lazy<int>(() => RepositorySettings.GetIntSetting("GUIDrivePerformanceMaxDrives", Common.ConnectionString) ?? 8);

        public static int ClientSummaryCacheDuration => _clientSummaryCacheDuration.Value;

        public static int DefaultCommandTimeout => _defaultCommandTimeout.Value;

        public static int SummaryCommandTimeout => _summaryCommandTimeout.Value;

        public static int DrivePerformanceMaxDrives => _drivePerformanceMaxDrives.Value;
    }
}