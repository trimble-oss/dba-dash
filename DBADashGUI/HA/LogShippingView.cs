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
        private ToolStripMenuItem mnuMetricsRoot;
        private ToolStripMenuItem mnuMetricsInstance;
        private ToolStripMenuItem mnuThresholdInstance;
        private ToolStripSeparator mnuConfigureSeparator;
        private ToolStripMenuItem mnuPairs;
        private ToolStripMenuItem mnuExclusionsRoot;
        private ToolStripMenuItem mnuExclusionsInstance;
        private ToolStripSeparator mnuExclusionsSeparator;
        private ToolStripSeparator mnuPairsSeparator;
        private ToolStripButton tsSetPrimary;

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

            mnuPairs = new ToolStripMenuItem("Log Shipping Pairs (Missing Databases)")
            {
                ToolTipText = "Configure primary/secondary instance pairs to alert on databases that are not log shipped"
            };
            mnuPairs.Click += (_, _) => ConfigurePairs();

            mnuExclusionsRoot = new ToolStripMenuItem("Exclusions (Root)")
            {
                ToolTipText = "Exclude databases from log shipping monitoring for all instances"
            };
            mnuExclusionsRoot.Click += (_, _) => ConfigureExclusions(-1);
            mnuExclusionsInstance = new ToolStripMenuItem("Exclusions (Instance)")
            {
                ToolTipText = "Exclude databases from log shipping monitoring for the selected (secondary) instance"
            };
            mnuExclusionsInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigureExclusions(id);
            };
            mnuExclusionsSeparator = new ToolStripSeparator();

            mnuConfigureSeparator = new ToolStripSeparator();
            mnuPairsSeparator = new ToolStripSeparator();
            tsConfigureLS.DropDownItems.AddRange(new ToolStripItem[]
            {
                mnuThresholdRoot, mnuThresholdInstance, mnuExclusionsSeparator, mnuExclusionsRoot, mnuExclusionsInstance,
                mnuConfigureSeparator, mnuMetricsRoot, mnuMetricsInstance, mnuPairsSeparator, mnuPairs
            });

            // Insert just after the Parameters button.  The status filter is added by the base class (the report
            // sets ShowStatusFilter) and sits immediately before this Configure button.
            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigureLS);

            tsSetPrimary = new ToolStripButton("Set Primary")
            {
                Name = "tsSetPrimary",
                ToolTipText = "Configure the log shipping primary for this instance to alert on databases that are not log shipped",
                Image = Properties.Resources.AddConnection_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Visible = false
            };
            tsSetPrimary.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigurePairs(id);
            };
            ToolStrip.Items.Insert(insertAt + 1, tsSetPrimary);
        }

        private void UpdateToolbarState()
        {
            var hasSingle = TryGetSingleInstanceId(out var instanceId);
            // Metric collection, exclusion & log shipping pair config is available to admins and the App role only (not AppReadOnly).
            var canConfigMetrics = DBADashUser.IsAdmin || DBADashUser.Roles.Contains("App");
            mnuMetricsRoot.Visible = canConfigMetrics;
            mnuMetricsInstance.Visible = canConfigMetrics;
            mnuConfigureSeparator.Visible = canConfigMetrics;
            mnuPairs.Visible = canConfigMetrics;
            mnuPairsSeparator.Visible = canConfigMetrics;
            mnuExclusionsRoot.Visible = canConfigMetrics;
            mnuExclusionsInstance.Visible = canConfigMetrics;
            mnuExclusionsSeparator.Visible = canConfigMetrics;
            mnuExclusionsInstance.Enabled = hasSingle;
            mnuMetricsInstance.Enabled = hasSingle;
            mnuThresholdInstance.Enabled = hasSingle;
            tsSetPrimary.Visible = canConfigMetrics && hasSingle && IsPrimaryNotConfigured(instanceId);
        }

        /// <summary>
        /// True if the instance has log shipped databases (a row in the summary) but no primary configured in the log
        /// shipping pairs.
        /// </summary>
        private bool IsPrimaryNotConfigured(int instanceId)
        {
            if (reportDS == null || reportDS.Tables.Count == 0) return false;
            var row = reportDS.Tables[0].Rows.Cast<DataRow>()
                .FirstOrDefault(r => r.Field<int>("InstanceID") == instanceId);
            return row != null && row["PrimaryInstances"] == DBNull.Value;
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

        private void ConfigurePairs(int? secondaryInstanceId = null)
        {
            using var frm = new LogShippingPairsConfig { SecondaryInstanceID = secondaryInstanceId };
            frm.ShowDialog();
            if (frm.HasChanges)
            {
                RefreshData();
            }
        }

        private void ConfigureExclusions(int instanceId)
        {
            var levelName = instanceId == -1
                ? null
                : CommonData.Instances?.Rows.Cast<DataRow>()
                    .FirstOrDefault(r => r.Field<int>("InstanceID") == instanceId)?
                    .Field<string>("InstanceDisplayName");
            using var frm = new LogShippingExclusionsConfig { InstanceID = instanceId, LevelName = levelName };
            if (frm.ShowDialog() == DialogResult.OK)
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
            UpdateToolbarState();
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
                var databaseId = -1;
                if (DatabaseLevel)
                {
                    // Databases missing from the secondary have no DatabaseID (and no link text).  Don't fall back to
                    // configuring the instance level thresholds from a database row.
                    if (row.Cells["DatabaseID"].Value.DBNullToNull() is not int id) return;
                    databaseId = id;
                }
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

        // Configure link bolding: bold when a threshold is explicitly configured at that level (matching the
        // styling in the legacy LogShippingControl).
        private static CellHighlightingRuleSet ConfiguredHighlight(string column, string valueWhenConfigured) =>
            new(column)
            {
                Rules = new List<CellHighlightingRule>
                {
                    new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = valueWhenConfigured, RelativeFontStyle = FontStyle.Bold },
                }
            };

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
                        ["PrimaryInstances"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Primary Instance", Description = "Primary instance from the configured log shipping pairs.  Blank if not configured (Configure > Log Shipping Pairs)" },
                        ["StatusDescription"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Status", Highlighting = StatusHighlight() },
                        ["Status"] = Hidden(4),
                        ["LogShippedDBCount"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Log Shipped DBs" },
                        ["WarningCount"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Warning" },
                        ["CriticalCount"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Critical" },
                        ["MissingCount"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Missing", Description = "Databases on the primary that are not log shipped to this instance.  Requires log shipping pairs to be configured." },
                        ["NewDatabaseCount"] = new ColumnMetadata { DisplayIndex = 9, Alias = "New DBs", Description = "Databases within the new database exclude period (N/A status).  Configured in the thresholds." },
                        ["MaxTotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Max Total Time Behind" },
                        ["MinTotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Min Total Time Behind" },
                        ["AvgTotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Avg Total Time Behind" },
                        ["MaxLatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Max Latency of Last" },
                        ["MinLatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Min Latency of Last" },
                        ["AvgLatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Avg Latency of Last" },
                        ["MaxTimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Max Time Since Last" },
                        ["MinTimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Min Time Since Last" },
                        ["AvgTimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Avg Time Since Last" },
                        ["SnapshotAgeDuration"] = new ColumnMetadata { DisplayIndex = 19, Alias = "Snapshot Age", Highlighting = SnapshotHighlight() },
                        ["MinDateOfLastBackupRestored"] = new ColumnMetadata { DisplayIndex = 20, Alias = "Backup Date of Oldest File" },
                        ["MaxDateOfLastBackupRestored"] = new ColumnMetadata { DisplayIndex = 21, Alias = "Backup Date of Newest File" },
                        ["MinLastRestoreCompleted"] = new ColumnMetadata { DisplayIndex = 22, Alias = "Restore Date of Oldest File" },
                        ["MaxLastRestoreCompleted"] = new ColumnMetadata { DisplayIndex = 23, Alias = "Restore Date of Newest File" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 24, Alias = "Configure", Link = new LogShippingConfigureThresholdLink { DatabaseLevel = false }, Description = "Configure instance-level thresholds", Highlighting = ConfiguredHighlight("InstanceLevelThreshold", "True") },
                        // Numeric (minutes) columns - hidden by default, available via the column chooser.
                        ["MaxTotalTimeBehind"] = Hidden(25),
                        ["MinTotalTimeBehind"] = Hidden(26),
                        ["AvgTotalTimeBehind"] = Hidden(27),
                        ["MaxLatencyOfLast"] = Hidden(28),
                        ["MinLatencyOfLast"] = Hidden(29),
                        ["AvgLatencyOfLast"] = Hidden(30),
                        ["MaxTimeSinceLast"] = Hidden(31),
                        ["MinTimeSinceLast"] = Hidden(32),
                        ["AvgTimeSinceLast"] = Hidden(33),
                        ["SnapshotAge"] = Hidden(34),
                        ["SnapshotAgeStatus"] = Hidden(35),
                        ["InstanceLevelThreshold"] = Hidden(36),
                        ["DatabaseLevelThresholds"] = Hidden(37),
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
                        ["PrimaryInstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Primary Instance", Description = "Primary instance from the configured log shipping pairs" },
                        ["StatusDescription"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Status", Highlighting = StatusHighlight() },
                        ["ExcludedBy"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Excluded By", Description = "Exclusion pattern that matched the database (Configure > Exclusions)" },
                        ["Status"] = Hidden(7),
                        ["restore_date"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Restore Date" },
                        ["backup_start_date"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Backup Start Date" },
                        ["TotalTimeBehindDuration"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Total Time Behind" },
                        ["LatencyOfLastDuration"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Latency of Last" },
                        ["TimeSinceLastDuration"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Time Since Last" },
                        ["SnapshotAgeDuration"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Snapshot Age", Highlighting = SnapshotHighlight() },
                        ["SnapshotDate"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Snapshot Date" },
                        ["last_file"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Last File" },
                        ["ThresholdConfiguredLevel"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Threshold Level" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Configure", Link = new LogShippingConfigureThresholdLink { DatabaseLevel = true }, Description = "Configure database-level thresholds", Highlighting = ConfiguredHighlight("ThresholdConfiguredLevel", "Database") },
                        // Numeric (minutes) columns - hidden by default, available via the column chooser.
                        ["TotalTimeBehind"] = Hidden(18),
                        ["LatencyOfLast"] = Hidden(19),
                        ["TimeSinceLast"] = Hidden(20),
                        ["SnapshotAge"] = Hidden(21),
                        ["SnapshotAgeStatus"] = Hidden(22),
                        ["IsMissing"] = Hidden(23),
                        ["PrimaryInstanceID"] = Hidden(24),
                        ["state_desc"] = new ColumnMetadata { Visible = false, DisplayIndex = 25, Alias = "State" },
                        ["IsExcluded"] = Hidden(26),
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
