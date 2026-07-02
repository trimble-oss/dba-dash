using DBADash;
using DBADashGUI.CustomReports;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.Backups
{
    /// <summary>
    /// Backups tab implemented as a system report.  Inherits from <see cref="CustomReportView"/> to reuse the
    /// standard report toolbar (copy/excel/refresh/column chooser), trigger collections, status/snapshot
    /// highlighting, drill-down navigation and back button, while adding the Backups specific status filter and
    /// threshold configuration.  Modelled on <see cref="LogShipping.LogShippingView"/>.
    ///
    /// The summary + database detail are served by a single proc (dbo.BackupReport_Get) returning two result
    /// sets.  Drilling into a single database swaps to a separate report wrapping dbo.LastBackup_Get to show the
    /// individual backup history for that database.
    /// </summary>
    internal class BackupsView : CustomReportView
    {
        private const string BackupReportProcedureName = "BackupReport_Get";

        private ToolStripDropDownButton tsConfigureBackups;
        private ToolStripMenuItem mnuThresholdInstance;

        public BackupsView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigureBackups = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigureBackups",
                ToolTipText = "Configure backup thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            var mnuThresholdRoot = new ToolStripMenuItem("Thresholds (Root)");
            mnuThresholdRoot.Click += (_, _) => ConfigureThresholds(-1, -1);
            mnuThresholdInstance = new ToolStripMenuItem("Thresholds (Instance)");
            mnuThresholdInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigureThresholds(id, -1);
            };
            tsConfigureBackups.DropDownItems.AddRange(new ToolStripItem[] { mnuThresholdRoot, mnuThresholdInstance });

            // Insert just after the Parameters button.  The status filter is added by the base class (the report
            // sets ShowStatusFilter) and sits immediately before this Configure button.
            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigureBackups);
        }

        private void UpdateToolbarState()
        {
            // The configure control only applies to the backup summary + detail report.  When we drill into the
            // per-database backup history (a different proc) it is hidden.  The status filter is shown/hidden by the
            // base class based on the report's ShowStatusFilter flag.
            tsConfigureBackups.Visible = Report?.ProcedureName == BackupReportProcedureName;
            mnuThresholdInstance.Enabled = TryGetSingleInstanceId(out _);
        }

        #endregion Toolbar

        #region Parameters

        /// <summary>
        /// Sets the @InstanceID drill-down parameter.  Replaces the parameter object (rather than mutating it) so
        /// the shallow-copied navigation state retains the original value for back-navigation.
        /// </summary>
        private void SetInstanceIDParam(int instanceId)
        {
            var idx = customParams.FindIndex(p =>
                p.Param.ParameterName.Equals("@InstanceID", StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return;
            var existing = customParams[idx];
            customParams[idx] = new CustomSqlParameter
            {
                Param = new SqlParameter
                {
                    ParameterName = existing.Param.ParameterName,
                    SqlDbType = existing.Param.SqlDbType,
                    Value = instanceId
                },
                UseDefaultValue = false
            };
        }

        /// <summary>
        /// Determines the single instance currently in scope, taking drill-down state into account.
        /// </summary>
        private bool TryGetSingleInstanceId(out int instanceId)
        {
            instanceId = -1;
            var drill = customParams?.FirstOrDefault(p =>
                p.Param.ParameterName.Equals("@InstanceID", StringComparison.OrdinalIgnoreCase) && !p.UseDefaultValue);
            if (drill?.Param.Value is int drilledId && drilledId > 0)
            {
                instanceId = drilledId;
                return true;
            }
            if (context == null) return false;
            if (context.InstanceID > 0)
            {
                instanceId = context.InstanceID;
                return true;
            }
            if (context.InstanceIDs.Count == 1)
            {
                instanceId = context.InstanceIDs.First();
                return true;
            }
            return false;
        }

        #endregion Parameters

        #region Configuration dialogs

        public void ConfigureThresholds(int instanceId, int databaseId)
        {
            using var frm = new BackupThresholdsConfig { InstanceID = instanceId, DatabaseID = databaseId };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        #endregion Configuration dialogs

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            UpdateToolbarState();
        }

        #endregion Grid post-processing

        #region Custom links

        /// <summary>
        /// Drill down from the instance summary to a single instance, narrowing both result sets in place.
        /// </summary>
        private sealed class BackupInstanceDrillDownLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not BackupsView view) return;
                if (row.Cells["InstanceID"].Value.DBNullToNull() is not int instanceId || instanceId <= 0) return;

                view.PushNavigationState();
                view.DrillDownGridFilters = null;
                view.SetInstanceIDParam(instanceId);
                // Focusing a single instance shows all of its databases.
                if (view.StatusFilter is { } filter)
                {
                    filter.Critical = true;
                    filter.Warning = true;
                    filter.NA = true;
                    filter.OK = true;
                }
                view.RefreshData();
            }
        }

        /// <summary>
        /// Opens the threshold configuration dialog for the clicked row.  When <see cref="DatabaseLevel"/> is set
        /// the database threshold is configured, otherwise the instance-level threshold.
        /// </summary>
        private sealed class BackupConfigureThresholdLink : LinkColumnInfo
        {
            public bool DatabaseLevel { get; init; }

            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not BackupsView view) return;
                var instanceId = row.Cells["InstanceID"].Value.DBNullToNull() as int? ?? -1;
                var databaseId = DatabaseLevel
                    ? row.Cells["DatabaseID"].Value.DBNullToNull() as int? ?? -1
                    : -1;
                view.ConfigureThresholds(instanceId, databaseId);
            }
        }

        #endregion Custom links

        #region Report definition

        // Status coloring: the displayed cell is colored from the (hidden) status column value.
        private static CellHighlightingRuleSet StatusHighlight(string statusColumn) =>
            new(statusColumn) { IsStatusColumn = true };

        // Count coloring: the count cell shows the given status color when greater than zero, otherwise N/A.
        private static CellHighlightingRuleSet CountHighlight(string column, DBADashStatusEnum statusWhenPositive) =>
            new(column)
            {
                Rules = new List<CellHighlightingRule>
                {
                    new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status = statusWhenPositive },
                    new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatusEnum.NA },
                }
            };

        // Configure link bolding: bold when a threshold is explicitly configured at that level (matching the
        // styling in the legacy BackupsControl).
        private static CellHighlightingRuleSet ConfiguredHighlight(string column, string valueWhenConfigured) =>
            new(column)
            {
                Rules = new List<CellHighlightingRule>
                {
                    new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = valueWhenConfigured, Font = new Font("Segoe UI", 9F, FontStyle.Bold) },
                }
            };

        private static ColumnMetadata Hidden(int displayIndex) =>
            new() { Visible = false, DisplayIndex = displayIndex };

        private static ColumnMetadata Num(int displayIndex, string alias, string format = "N1") =>
            new() { DisplayIndex = displayIndex, Alias = alias, FormatString = format };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(BackupsView),
            ReportName = "Backups",
            SchemaName = "dbo",
            ProcedureName = BackupReportProcedureName,
            QualifiedProcedureName = "dbo.BackupReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.Backups.ToString(),
                CollectionType.Databases.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Summary",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = Hidden(0),
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Instance", Link = new BackupInstanceDrillDownLink(), Description = "Click to view databases for this instance" },
                        ["DatabaseCount"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Database Count" },
                        ["FullOK"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Full Backup OK", Highlighting = CountHighlight("FullOK", DBADashStatusEnum.OK) },
                        ["FullNA"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Full Backup N/A" },
                        ["FullWarning"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Full Backup Warning", Highlighting = CountHighlight("FullWarning", DBADashStatusEnum.Warning) },
                        ["FullCritical"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Full Backup Critical", Highlighting = CountHighlight("FullCritical", DBADashStatusEnum.Critical) },
                        ["DiffOK"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Diff Backup OK", Highlighting = CountHighlight("DiffOK", DBADashStatusEnum.OK) },
                        ["DiffNA"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Diff Backup N/A" },
                        ["DiffWarning"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Diff Backup Warning", Highlighting = CountHighlight("DiffWarning", DBADashStatusEnum.Warning) },
                        ["DiffCritical"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Diff Backup Critical", Highlighting = CountHighlight("DiffCritical", DBADashStatusEnum.Critical) },
                        ["LogOK"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Log Backup OK", Highlighting = CountHighlight("LogOK", DBADashStatusEnum.OK) },
                        ["LogNA"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Log Backup N/A" },
                        ["LogWarning"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Log Backup Warning", Highlighting = CountHighlight("LogWarning", DBADashStatusEnum.Warning) },
                        ["LogCritical"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Log Backup Critical", Highlighting = CountHighlight("LogCritical", DBADashStatusEnum.Critical) },
                        ["FullRecoveryCount"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Full Recovery Count" },
                        ["BulkLoggedCount"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Bulk Logged Count" },
                        ["SimpleCount"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Simple Count" },
                        ["IsPartnerBackup"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Is Partner Backup" },
                        ["SnapshotDate"] = new ColumnMetadata { DisplayIndex = 19, Alias = "Snapshot Date" },
                        ["SnapshotAge"] = new ColumnMetadata { DisplayIndex = 20, Alias = "Snapshot Age", Highlighting = StatusHighlight("SnapshotAgeStatus") },
                        ["OldestFull"] = new ColumnMetadata { DisplayIndex = 21, Alias = "Oldest Full", Description = "Date of oldest full backup, excluding databases without a threshold configured" },
                        ["OldestDiff"] = new ColumnMetadata { DisplayIndex = 22, Alias = "Oldest Diff", Description = "Date of oldest diff backup, excluding databases without a threshold configured" },
                        ["OldestLog"] = new ColumnMetadata { DisplayIndex = 23, Alias = "Oldest Log", Description = "Date of oldest log backup, excluding databases without a threshold configured" },
                        ["FullBackupSizeGB"] = Num(24, "Full Backup Size (GB)"),
                        ["FullBackupMBsec"] = Num(25, "Full Backup MB/sec"),
                        ["FullBackupSizeCompressedGB"] = Num(26, "Full Backup Size (Compressed) (GB)"),
                        ["FullCompressionSavingPct"] = Num(27, "Full Compression Saving %", "P1"),
                        ["FullBackupWriteMBsec"] = Num(28, "Full Backup Write MB/sec"),
                        ["DiffBackupSizeGB"] = Num(29, "Diff Backup Size (GB)"),
                        ["DiffBackupMBsec"] = Num(30, "Diff Backup MB/sec"),
                        ["DiffBackupSizeCompressedGB"] = Num(31, "Diff Backup Size (Compressed) (GB)"),
                        ["DiffCompressionSavingPct"] = Num(32, "Diff Compression Saving %", "P1"),
                        ["DiffBackupWriteMBsec"] = Num(33, "Diff Backup Write MB/sec"),
                        ["FullChecksum"] = new ColumnMetadata { DisplayIndex = 34, Alias = "Full Checksums" },
                        ["DiffChecksum"] = new ColumnMetadata { DisplayIndex = 35, Alias = "Diff Checksums" },
                        ["LogChecksum"] = new ColumnMetadata { DisplayIndex = 36, Alias = "Log Checksums" },
                        ["FullCompressed"] = new ColumnMetadata { DisplayIndex = 37, Alias = "Full Compressed" },
                        ["DiffCompressed"] = new ColumnMetadata { DisplayIndex = 38, Alias = "Diff Compressed" },
                        ["LogCompressed"] = new ColumnMetadata { DisplayIndex = 39, Alias = "Log Compressed" },
                        ["FullPasswordProtected"] = new ColumnMetadata { DisplayIndex = 40, Alias = "Full Password Protected" },
                        ["DiffPasswordProtected"] = new ColumnMetadata { DisplayIndex = 41, Alias = "Diff Password Protected" },
                        ["LogPasswordProtected"] = new ColumnMetadata { DisplayIndex = 42, Alias = "Log Password Protected" },
                        ["FullEncrypted"] = new ColumnMetadata { DisplayIndex = 43, Alias = "Full Encrypted" },
                        ["DiffEncrypted"] = new ColumnMetadata { DisplayIndex = 44, Alias = "Diff Encrypted" },
                        ["LogEncrypted"] = new ColumnMetadata { DisplayIndex = 45, Alias = "Log Encrypted" },
                        ["SnapshotBackups"] = new ColumnMetadata { DisplayIndex = 46, Alias = "Snapshot Backups" },
                        ["DBThresholdConfiguration"] = new ColumnMetadata { DisplayIndex = 47, Alias = "DB Level Threshold Config" },
                        ["FullCompressionAlgorithms"] = new ColumnMetadata { DisplayIndex = 48, Alias = "Full Compression Algorithms" },
                        ["DiffCompressionAlgorithms"] = new ColumnMetadata { DisplayIndex = 49, Alias = "Diff Compression Algorithms" },
                        ["LogCompressionAlgorithms"] = new ColumnMetadata { DisplayIndex = 50, Alias = "Log Compression Algorithms" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 51, Alias = "Configure", Link = new BackupConfigureThresholdLink { DatabaseLevel = false }, Description = "Configure instance-level thresholds", Highlighting = ConfiguredHighlight("InstanceThresholdConfiguration", "True") },
                        // Hidden technical columns.
                        ["Instance"] = Hidden(52),
                        ["SnapshotAgeStatus"] = Hidden(53),
                        ["InstanceThresholdConfiguration"] = Hidden(54),
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Detail",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = Hidden(0),
                        ["DatabaseID"] = Hidden(1),
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Instance" },
                        ["name"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Database", Link = new SystemDrillDownLinkColumnInfo { ReportFactory = () => LastBackupReport, ColumnToParameterMap = new Dictionary<string, string> { ["@DatabaseID"] = "DatabaseID" }, DrillDownMode = DrillDownMode.ExistingWindow }, Description = "Click to view backup history for this database" },
                        ["create_date_utc"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Created" },
                        ["recovery_model_desc"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Recovery Model" },
                        ["LastFull"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Last Full", Highlighting = StatusHighlight("FullBackupStatus") },
                        ["LastDiff"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Last Diff", Highlighting = StatusHighlight("DiffBackupStatus") },
                        ["LastLog"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Last Log", Highlighting = StatusHighlight("LogBackupStatus") },
                        ["LastFG"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Last Filegroup", Highlighting = StatusHighlight("FGBackupStatus") },
                        ["LastFGDiff"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Last Filegroup Diff", Highlighting = StatusHighlight("FGDiffBackupStatus") },
                        ["LastPartial"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Last Partial", Highlighting = StatusHighlight("PartialBackupStatus") },
                        ["LastPartialDiff"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Last Partial Diff", Highlighting = StatusHighlight("PartialDiffBackupStatus") },
                        ["SnapshotBackups"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Snapshot Backup" },
                        ["IsPartnerBackup"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Is Partner Backup" },
                        ["IsFullDamaged"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Is Full Damaged" },
                        ["IsDiffDamaged"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Is Diff Damaged" },
                        ["IsLogDamaged"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Is Log Damaged" },
                        ["SnapshotDate"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Snapshot Date" },
                        ["SnapshotAge"] = new ColumnMetadata { DisplayIndex = 19, Alias = "Snapshot Age", Highlighting = StatusHighlight("SnapshotAgeStatus") },
                        ["ThresholdsConfiguredLevel"] = new ColumnMetadata { DisplayIndex = 20, Alias = "Threshold Configured Level" },
                        ["FullBackupExcludedReason"] = new ColumnMetadata { DisplayIndex = 21, Alias = "Full Backup Excluded Reason" },
                        ["DiffBackupExcludedReason"] = new ColumnMetadata { DisplayIndex = 22, Alias = "Diff Backup Excluded Reason" },
                        ["LogBackupExcludedReason"] = new ColumnMetadata { DisplayIndex = 23, Alias = "Log Backup Excluded Reason" },
                        ["LogBackupWarningThreshold"] = new ColumnMetadata { DisplayIndex = 24, Alias = "Log Backup Warning Threshold" },
                        ["LogBackupCriticalThreshold"] = new ColumnMetadata { DisplayIndex = 25, Alias = "Log Backup Critical Threshold" },
                        ["FullBackupWarningThreshold"] = new ColumnMetadata { DisplayIndex = 26, Alias = "Full Backup Warning Threshold" },
                        ["FullBackupCriticalThreshold"] = new ColumnMetadata { DisplayIndex = 27, Alias = "Full Backup Critical Threshold" },
                        ["DiffBackupWarningThreshold"] = new ColumnMetadata { DisplayIndex = 28, Alias = "Diff Backup Warning Threshold" },
                        ["DiffBackupCriticalThreshold"] = new ColumnMetadata { DisplayIndex = 29, Alias = "Diff Backup Critical Threshold" },
                        ["ConsiderPartialBackups"] = new ColumnMetadata { DisplayIndex = 30, Alias = "Consider Partial Backups" },
                        ["ConsiderFGBackups"] = new ColumnMetadata { DisplayIndex = 31, Alias = "Consider FG Backups" },
                        ["ConsiderCopyOnlyBackups"] = new ColumnMetadata { DisplayIndex = 32, Alias = "Consider Copy Only Backups" },
                        ["ConsiderSnapshotBackups"] = new ColumnMetadata { DisplayIndex = 33, Alias = "Consider Snapshot Backups" },
                        ["ConsiderFullBackupWithDiffThreshold"] = new ColumnMetadata { DisplayIndex = 34, Alias = "Ignore DIFF Threshold if FULL meets criteria" },
                        ["LastFullDuration"] = new ColumnMetadata { DisplayIndex = 35, Alias = "Last Full Duration" },
                        ["FullBackupSizeGB"] = Num(36, "Full Backup Size (GB)"),
                        ["FullBackupMBsec"] = Num(37, "Full Backup MB/sec"),
                        ["FullBackupSizeCompressedGB"] = Num(38, "Full Backup Size (Compressed) (GB)"),
                        ["FullCompressionSavingPct"] = Num(39, "Full Compression Saving %", "P1"),
                        ["FullBackupWriteMBsec"] = Num(40, "Full Backup Write MB/sec"),
                        ["DiffBackupSizeGB"] = Num(41, "Diff Backup Size (GB)"),
                        ["DiffBackupMBsec"] = Num(42, "Diff Backup MB/sec"),
                        ["DiffBackupSizeCompressedGB"] = Num(43, "Diff Backup Size (Compressed) (GB)"),
                        ["DiffCompressionSavingPct"] = Num(44, "Diff Compression Saving %", "P1"),
                        ["DiffBackupWriteMBsec"] = Num(45, "Diff Backup Write MB/sec"),
                        ["IsFullChecksum"] = new ColumnMetadata { DisplayIndex = 46, Alias = "Full Checksum" },
                        ["IsDiffChecksum"] = new ColumnMetadata { DisplayIndex = 47, Alias = "Diff Checksum" },
                        ["IsLogChecksum"] = new ColumnMetadata { DisplayIndex = 48, Alias = "Log Checksum" },
                        ["IsFullCompressed"] = new ColumnMetadata { DisplayIndex = 49, Alias = "Full Compressed" },
                        ["IsDiffCompressed"] = new ColumnMetadata { DisplayIndex = 50, Alias = "Diff Compressed" },
                        ["IsLogCompressed"] = new ColumnMetadata { DisplayIndex = 51, Alias = "Log Compressed" },
                        ["IsFullPasswordProtected"] = new ColumnMetadata { DisplayIndex = 52, Alias = "Full Password Protected" },
                        ["IsDiffPasswordProtected"] = new ColumnMetadata { DisplayIndex = 53, Alias = "Diff Password Protected" },
                        ["IsLogPasswordProtected"] = new ColumnMetadata { DisplayIndex = 54, Alias = "Log Password Protected" },
                        ["IsFullEncrypted"] = new ColumnMetadata { DisplayIndex = 55, Alias = "Full Encrypted" },
                        ["IsDiffEncrypted"] = new ColumnMetadata { DisplayIndex = 56, Alias = "Diff Encrypted" },
                        ["IsLogEncrypted"] = new ColumnMetadata { DisplayIndex = 57, Alias = "Log Encrypted" },
                        ["FullCompressionAlgorithm"] = new ColumnMetadata { DisplayIndex = 58, Alias = "Full Compression Algorithm" },
                        ["DiffCompressionAlgorithm"] = new ColumnMetadata { DisplayIndex = 59, Alias = "Diff Compression Algorithm" },
                        ["LogCompressionAlgorithm"] = new ColumnMetadata { DisplayIndex = 60, Alias = "Log Compression Algorithm" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 61, Alias = "Configure", Link = new BackupConfigureThresholdLink { DatabaseLevel = true }, Description = "Configure database-level thresholds", Highlighting = ConfiguredHighlight("ThresholdsConfiguredLevel", "Database") },
                        // Hidden technical columns - status enums driving cell highlighting and values not displayed.
                        ["Instance"] = Hidden(62),
                        ["recovery_model"] = Hidden(63),
                        ["FullBackupStatus"] = Hidden(64),
                        ["DiffBackupStatus"] = Hidden(65),
                        ["LogBackupStatus"] = Hidden(66),
                        ["BackupStatus"] = Hidden(67),
                        ["PartialBackupStatus"] = Hidden(68),
                        ["PartialDiffBackupStatus"] = Hidden(69),
                        ["FGBackupStatus"] = Hidden(70),
                        ["FGDiffBackupStatus"] = Hidden(71),
                        ["SnapshotAgeStatus"] = Hidden(72),
                        ["create_date"] = Hidden(73),
                        ["UTCOffset"] = Hidden(74),
                        ["LastFullDurationSec"] = Hidden(75),
                        ["LastDiffDuration"] = Hidden(76),
                        ["LastDiffDurationSec"] = Hidden(77),
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@IncludeCritical", ParamType = "BIT" },
                    new() { ParamName = "@IncludeWarning", ParamType = "BIT" },
                    new() { ParamName = "@IncludeNA", ParamType = "BIT" },
                    new() { ParamName = "@IncludeOK", ParamType = "BIT" },
                    new() { ParamName = "@ShowHidden", ParamType = "BIT" },
                }
            }
        };

        /// <summary>
        /// Per-database backup history, reached by drilling into a database from the detail grid.  Wraps the
        /// legacy dbo.LastBackup_Get proc.
        /// </summary>
        private static SystemReport LastBackupReport => new()
        {
            ViewType = typeof(BackupsView),
            ReportName = "Last Backup",
            SchemaName = "dbo",
            ProcedureName = "LastBackup_Get",
            QualifiedProcedureName = "dbo.LastBackup_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string>(),
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Last Backup",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["name"] = new ColumnMetadata { DisplayIndex = 0, Alias = "Database" },
                        ["recovery_model"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Recovery Model" },
                        ["backup_type_desc"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Type" },
                        ["backup_start_date_utc"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Start Date" },
                        ["backup_finish_date_utc"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Finish Date" },
                        ["BackupDuration"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Duration" },
                        ["BackupSizeGB"] = Num(6, "Backup Size (GB)"),
                        ["BackupMBsec"] = Num(7, "Backup MB/sec"),
                        ["BackupSizeCompressedGB"] = Num(8, "Backup Size (Compressed) (GB)"),
                        ["CompressionSavingPct"] = Num(9, "Compression Saving %", "P1"),
                        ["BackupWriteMBsec"] = Num(10, "Backup Write MB/sec"),
                        ["IsPartnerBackup"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Is Partner Backup" },
                        ["Partner"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Partner" },
                        ["is_damaged"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Is Damaged" },
                        ["has_backup_checksums"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Checksum" },
                        ["IsCompressed"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Is Compressed" },
                        ["is_password_protected"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Password Protected" },
                        ["IsEncrypted"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Encrypted" },
                        ["is_snapshot"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Snapshot Backup" },
                        ["has_bulk_logged_data"] = new ColumnMetadata { DisplayIndex = 19, Alias = "Has Bulk Logged Data" },
                        ["is_readonly"] = new ColumnMetadata { DisplayIndex = 20, Alias = "Readonly" },
                        ["is_force_offline"] = new ColumnMetadata { DisplayIndex = 21, Alias = "Force Offline" },
                        ["is_single_user"] = new ColumnMetadata { DisplayIndex = 22, Alias = "Single User" },
                        ["key_algorithm"] = new ColumnMetadata { DisplayIndex = 23, Alias = "Key Algorithm" },
                        ["encryptor_type"] = new ColumnMetadata { DisplayIndex = 24, Alias = "Encryptor Type" },
                        ["compression_algorithm"] = new ColumnMetadata { DisplayIndex = 25, Alias = "Compression Algorithm" },
                        // Hidden technical columns.
                        ["DatabaseID"] = Hidden(26),
                        ["type"] = Hidden(27),
                        ["BackupDurationSec"] = Hidden(28),
                        ["has_incomplete_metadata"] = Hidden(29),
                        ["is_copy_only"] = Hidden(30),
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@DatabaseID", ParamType = "INT" },
                }
            }
        };

        #endregion Report definition
    }
}
