using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.HA;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.LogShipping
{
    /// <summary>
    /// Log Shipping tab implemented as a system report.  Inherits from <see cref="CustomReportView"/> to reuse
    /// the standard report toolbar (copy/excel/refresh/column chooser), trigger collections, status/snapshot
    /// highlighting, drill-down navigation and back button, while adding the Log Shipping specific status filter
    /// and threshold/metric configuration.  Modelled on <see cref="DBFiles.SpaceTracking"/>.
    /// </summary>
    internal class LogShippingView : CustomReportView
    {
        private const string ConfigureColumnName = "Configure";

        private ToolStripMenuItem mnuMetricsRoot;
        private ToolStripMenuItem mnuMetricsInstance;
        private ToolStripMenuItem mnuThresholdInstance;
        private ToolStripSeparator mnuConfigureSeparator;

        public LogShippingView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            var tsConfigureLS = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigureLogShipping",
                ToolTipText = "Configure Log Shipping thresholds and metric collection",
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

            mnuMetricsRoot = new ToolStripMenuItem("Metric Collection (Root)");
            mnuMetricsRoot.Click += (_, _) => ConfigureMetrics(-1);
            mnuMetricsInstance = new ToolStripMenuItem("Metric Collection (Instance)");
            mnuMetricsInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigureMetrics(id);
            };

            mnuConfigureSeparator = new ToolStripSeparator();
            tsConfigureLS.DropDownItems.AddRange(new ToolStripItem[]
            {
                mnuThresholdRoot, mnuThresholdInstance, mnuConfigureSeparator, mnuMetricsRoot, mnuMetricsInstance
            });

            // Insert just after the Parameters button.  The status filter is added by the base class (the report
            // sets ShowStatusFilter) and sits immediately before this Configure button.
            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigureLS);
        }

        private void UpdateToolbarState()
        {
            var hasSingle = TryGetSingleInstanceId(out _);
            // Metric collection config is available to admins and the App role only (not AppReadOnly).
            var canConfigMetrics = DBADashUser.IsAdmin || DBADashUser.Roles.Contains("App");
            mnuMetricsRoot.Visible = canConfigMetrics;
            mnuMetricsInstance.Visible = canConfigMetrics;
            mnuConfigureSeparator.Visible = canConfigMetrics;
            mnuMetricsInstance.Enabled = hasSingle;
            mnuThresholdInstance.Enabled = hasSingle;
        }

        #endregion Toolbar

        #region Parameters

        /// <summary>
        /// Sets the @InstanceID drill-down parameter.  Replaces the parameter object (rather than mutating it)
        /// so the shallow-copied navigation state retains the original value for back-navigation.
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
            using var frm = new LogShippingThresholdsConfig { InstanceID = instanceId, DatabaseID = databaseId };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private static void ConfigureMetrics(int instanceId)
        {
            using var metricsConfig = new RepositoryMetricsConfig
            {
                InstanceID = instanceId,
                MetricType = RepositoryMetricsConfig.RepositoryMetricTypes.LogShipping
            };
            metricsConfig.ShowDialog();
        }

        #endregion Configuration dialogs

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            foreach (var grid in Grids)
            {
                ApplyThresholdStyling(grid);
            }
            UpdateToolbarState();
        }

        /// <summary>
        /// Bold the Configure link where a threshold is explicitly configured at that level (matching the
        /// styling in the legacy LogShippingControl).
        /// </summary>
        private static void ApplyThresholdStyling(DBADashDataGridView grid)
        {
            if (grid.Columns[ConfigureColumnName] == null) return;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is not DataRowView drv) continue;
                var table = drv.Row.Table;
                bool bold;
                if (grid.ResultSetID == 0)
                {
                    bold = table.Columns.Contains("InstanceLevelThreshold")
                           && drv["InstanceLevelThreshold"].DBNullToNull() is bool b && b;
                }
                else
                {
                    bold = table.Columns.Contains("ThresholdConfiguredLevel")
                           && (drv["ThresholdConfiguredLevel"] as string) == "Database";
                }
                row.Cells[ConfigureColumnName].Style.Font =
                    new Font(grid.Font, bold ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        #endregion Grid post-processing

        #region Custom links

        /// <summary>
        /// Drill down from the instance summary to a single instance, narrowing both result sets in place.
        /// </summary>
        private sealed class LogShippingInstanceDrillDownLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not LogShippingView view) return;
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
        private sealed class LogShippingConfigureThresholdLink : LinkColumnInfo
        {
            public bool DatabaseLevel { get; init; }

            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not LogShippingView view) return;
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
        private static CellHighlightingRuleSet StatusHighlight() =>
            new("Status") { IsStatusColumn = true };

        private static CellHighlightingRuleSet SnapshotHighlight() =>
            new("SnapshotAgeStatus") { IsStatusColumn = true };

        private static ColumnMetadata Hidden(int displayIndex) =>
            new() { Visible = false, DisplayIndex = displayIndex };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(LogShippingView),
            ReportName = "Log Shipping",
            SchemaName = "dbo",
            ProcedureName = "LogShippingReport_Get",
            QualifiedProcedureName = "dbo.LogShippingReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.LogRestores.ToString(),
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
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Instance", Link = new LogShippingInstanceDrillDownLink(), Description = "Click to view databases for this instance" },
                        ["StatusDescription"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Status", Highlighting = StatusHighlight() },
                        ["Status"] = Hidden(3),
                        ["LogShippedDBCount"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Log Shipped DBs" },
                        ["WarningCount"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Warning" },
                        ["CriticalCount"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Critical" },
                        ["MaxTotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Max Total Time Behind" },
                        ["MinTotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Min Total Time Behind" },
                        ["AvgTotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Avg Total Time Behind" },
                        ["MaxLatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Max Latency of Last" },
                        ["MinLatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Min Latency of Last" },
                        ["AvgLatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Avg Latency of Last" },
                        ["MaxTimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Max Time Since Last" },
                        ["MinTimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Min Time Since Last" },
                        ["AvgTimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Avg Time Since Last" },
                        ["SnapshotAgeDuration"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Snapshot Age", Highlighting = SnapshotHighlight() },
                        ["MinDateOfLastBackupRestored"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Backup Date of Oldest File" },
                        ["MaxDateOfLastBackupRestored"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Backup Date of Newest File" },
                        ["MinLastRestoreCompleted"] = new ColumnMetadata { DisplayIndex = 19, Alias = "Restore Date of Oldest File" },
                        ["MaxLastRestoreCompleted"] = new ColumnMetadata { DisplayIndex = 20, Alias = "Restore Date of Newest File" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 21, Alias = "Configure", Link = new LogShippingConfigureThresholdLink { DatabaseLevel = false }, Description = "Configure instance-level thresholds" },
                        // Numeric (minutes) columns - hidden by default, available via the column chooser.
                        ["MaxTotalTimeBehind"] = Hidden(22),
                        ["MinTotalTimeBehind"] = Hidden(23),
                        ["AvgTotalTimeBehind"] = Hidden(24),
                        ["MaxLatencyOfLast"] = Hidden(25),
                        ["MinLatencyOfLast"] = Hidden(26),
                        ["AvgLatencyOfLast"] = Hidden(27),
                        ["MaxTimeSinceLast"] = Hidden(28),
                        ["MinTimeSinceLast"] = Hidden(29),
                        ["AvgTimeSinceLast"] = Hidden(30),
                        ["SnapshotAge"] = Hidden(31),
                        ["SnapshotAgeStatus"] = Hidden(32),
                        ["InstanceLevelThreshold"] = Hidden(33),
                        ["DatabaseLevelThresholds"] = Hidden(34),
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
                        ["name"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Database" },
                        ["StatusDescription"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Status", Highlighting = StatusHighlight() },
                        ["Status"] = Hidden(5),
                        ["restore_date"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Restore Date" },
                        ["backup_start_date"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Backup Start Date" },
                        ["TotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Total Time Behind" },
                        ["LatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Latency of Last" },
                        ["TimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Time Since Last" },
                        ["SnapshotAgeDuration"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Snapshot Age", Highlighting = SnapshotHighlight() },
                        ["SnapshotDate"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Snapshot Date" },
                        ["last_file"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Last File" },
                        ["ThresholdConfiguredLevel"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Threshold Level" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Configure", Link = new LogShippingConfigureThresholdLink { DatabaseLevel = true }, Description = "Configure database-level thresholds" },
                        // Numeric (minutes) columns - hidden by default, available via the column chooser.
                        ["TotalTimeBehind"] = Hidden(16),
                        ["LatencyOfLast"] = Hidden(17),
                        ["TimeSinceLast"] = Hidden(18),
                        ["SnapshotAge"] = Hidden(19),
                        ["SnapshotAgeStatus"] = Hidden(20),
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

        #endregion Report definition
    }
}
