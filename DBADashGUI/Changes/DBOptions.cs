using DBADash;
using DBADashGUI.HA;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    internal class DBOptionsReport : CustomReportView
    {
        private const int MAX_VLF_WARNING_THRESHOLD = 1000;
        private const int MAX_VLF_CRITICAL_THRESHOLD = 10000;

        private ToolStripDropDownButton tsConfigureMetrics;
        private ToolStripMenuItem tsConfigureRoot;
        private ToolStripMenuItem tsConfigureInstance;
        private ToolStripButton tsDetail;
        private ToolStripButton tsSummary;

        private bool IsSummaryMode => Report is SystemReport sr && sr.ProcedureName == "DBOptionsSummary_Get";

        public DBOptionsReport()
        {
            PostGridRefresh += DBOptionsReport_PostGridRefresh;
            Report = SummaryInstance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        private void AddToolbarButtons()
        {
            tsDetail = new ToolStripButton("Detail") { ToolTipText = "Switch to detail view showing all databases", Image = Properties.Resources.Table_16x };
            tsDetail.Click += (_, _) => SwitchToDetail();

            tsSummary = new ToolStripButton("Summary") { ToolTipText = "Switch back to summary view", Visible = false, Image = Properties.Resources.Table_16x };
            tsSummary.Click += (_, _) => SwitchToSummary();

            tsConfigureRoot = new ToolStripMenuItem("Root") { ToolTipText = "Configure database collection metrics at root level" };
            tsConfigureRoot.Click += (_, _) => ConfigureMetrics(-1);

            tsConfigureInstance = new ToolStripMenuItem("Instance") { ToolTipText = "Configure database collection metrics for this instance" };
            tsConfigureInstance.Click += (_, _) =>
            {
                if (context?.InstanceIDs?.Count == 1)
                {
                    ConfigureMetrics(context.InstanceIDs.First());
                }
            };

            tsConfigureMetrics = new ToolStripDropDownButton("Configure Metrics")
            {
                ToolTipText = "Configure database collection metrics",
                DropDownItems = { tsConfigureRoot, tsConfigureInstance },
                Image = Properties.Resources.SettingsOutline_16x
            };

            var insertAt = ToolStrip.Items.IndexOfKey("tsCols");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt++, tsDetail);
            ToolStrip.Items.Insert(insertAt++, tsSummary);
            ToolStrip.Items.Insert(insertAt, tsConfigureMetrics);
        }

        private void SwitchToDetail()
        {
            PushNavigationState();
            Report = DetailInstance;
            DrillDownGridFilters = null;
            customParams = Report.GetCustomSqlParameters();
            RefreshData();
        }

        private void SwitchToSummary()
        {
            ClearNavigationStack();
            Report = SummaryInstance;
            DrillDownGridFilters = null;
            customParams = Report.GetCustomSqlParameters();
            RefreshData();
        }

        private void DBOptionsReport_PostGridRefresh(object sender, EventArgs e)
        {
            tsConfigureInstance.Enabled = context?.InstanceIDs?.Count == 1;

            // Sync toolbar state with current mode
            tsDetail.Visible = IsSummaryMode;
            tsSummary.Visible = !IsSummaryMode;

            // Apply manual highlighting for detail mode (cross-column comparisons)
            if (!IsSummaryMode && Grids.Count > 0)
            {
                var detailGrid = Grids[0];
                detailGrid.RowsAdded -= Detail_RowsAdded;
                detailGrid.RowsAdded += Detail_RowsAdded;

                if (detailGrid.Rows.Count > 0)
                {
                    Detail_RowsAdded(detailGrid, new DataGridViewRowsAddedEventArgs(0, detailGrid.Rows.Count));
                }
            }
        }

        /// <summary>
        /// Manual highlighting for cross-column comparisons that can't be expressed with rules
        /// </summary>
        private void Detail_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is not DataGridView dgv) return;

            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                if (idx >= dgv.Rows.Count) break;
                var r = dgv.Rows[idx];

                // compatibility_level vs MaxSupportedCompatibilityLevel (cross-column comparison)
                if (dgv.Columns.Contains("compatibility_level") && dgv.Columns.Contains("MaxSupportedCompatibilityLevel"))
                {
                    var maxCompatLevel = r.Cells["MaxSupportedCompatibilityLevel"].Value == DBNull.Value ? 0 : Convert.ToInt16(r.Cells["MaxSupportedCompatibilityLevel"].Value);
                    r.Cells["compatibility_level"].SetStatusColor(Convert.ToInt16(r.Cells["compatibility_level"].Value) < maxCompatLevel
                        ? DBADashStatus.DBADashStatusEnum.Warning
                        : DBADashStatus.DBADashStatusEnum.OK);
                }

                // is_trustworthy_on: NA for msdb (database_id=4), otherwise rule-based
                if (dgv.Columns.Contains("is_trustworthy_on") && dgv.Columns.Contains("database_id"))
                {
                    if (Convert.ToInt32(r.Cells["database_id"].Value) == 4) // msdb
                    {
                        r.Cells["is_trustworthy_on"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                    }
                    else
                    {
                        r.Cells["is_trustworthy_on"].SetStatusColor(r.Cells["is_trustworthy_on"].Value as bool? == false
                            ? DBADashStatus.DBADashStatusEnum.OK
                            : DBADashStatus.DBADashStatusEnum.Critical);
                    }
                }
            }
        }

        private static void ConfigureMetrics(int instanceId)
        {
            using var metricsConfig = new RepositoryMetricsConfig() { InstanceID = instanceId, MetricType = RepositoryMetricsConfig.RepositoryMetricTypes.Databases };
            metricsConfig.ShowDialog();
        }

        #region Report metadata helpers

        /// <summary>
        /// Creates a highlighting rule set that colors cells based on count: > 0 shows the specified status, 0 shows OK, null shows NA
        /// </summary>
        private static CellHighlightingRuleSet CountStatusRuleSet(string columnName, DBADashStatus.DBADashStatusEnum nonZeroStatus) => new(columnName)
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.IsNull, Status = DBADashStatus.DBADashStatusEnum.NA },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status = nonZeroStatus },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.OK },
            }
        };

        /// <summary>
        /// Creates a drill-down link to detail mode with an optional filter on the detail grid (result set 0).
        /// Maps both @InstanceIDs and @InstanceGroupName so Azure DB instances (NULL InstanceID) work.
        /// </summary>
        private static SystemDrillDownLinkColumnInfo DrillDownLink(string detailFilter = null) => new()
        {
            ReportFactory = () => DetailInstance,
            DrillDownMode = DrillDownMode.ExistingWindow,
            ColumnToParameterMap = new Dictionary<string, string>
            {
                { "@InstanceIDs", "InstanceID" },
                { "@InstanceGroupName", "Instance" },
            },
            GridFilters = string.IsNullOrEmpty(detailFilter) ? null : new Dictionary<int, string> { { 0, detailFilter } },
        };

        /// <summary>
        /// Creates a drill-down link that builds the filter dynamically from row values.
        /// </summary>
        private static SystemDrillDownLinkColumnInfo DrillDownLinkDynamic(Func<DataGridViewRow, string> filterBuilder) => new()
        {
            ReportFactory = () => DetailInstance,
            DrillDownMode = DrillDownMode.ExistingWindow,
            ColumnToParameterMap = new Dictionary<string, string>
            {
                { "@InstanceIDs", "InstanceID" },
                { "@InstanceGroupName", "Instance" },
            },
            GridFilterFactory = row =>
            {
                var filter = filterBuilder(row);
                return string.IsNullOrEmpty(filter) ? null : new Dictionary<int, string> { { 0, filter } };
            },
        };

        private static ColumnMetadata SummaryColumnWithHighlighting(string alias, string description, DBADashStatus.DBADashStatusEnum nonZeroStatus, string columnName, string detailFilter) => new()
        {
            Alias = alias,
            Description = description,
            Link = DrillDownLink(detailFilter),
            Highlighting = CountStatusRuleSet(columnName, nonZeroStatus),
        };

        /// <summary>
        /// Boolean rule: true = OK, false = notOkStatus
        /// </summary>
        private static CellHighlightingRuleSet BoolTrueIsOk(string columnName, DBADashStatus.DBADashStatusEnum notOkStatus) => new(columnName)
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "True", Status = DBADashStatus.DBADashStatusEnum.OK },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = notOkStatus },
            }
        };

        /// <summary>
        /// Boolean rule: false = OK, true = notOkStatus
        /// </summary>
        private static CellHighlightingRuleSet BoolFalseIsOk(string columnName, DBADashStatus.DBADashStatusEnum notOkStatus) => new(columnName)
        {
            Rules = new List<CellHighlightingRule>
            {
                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "False", Status = DBADashStatus.DBADashStatusEnum.OK },
                new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = notOkStatus },
            }
        };

        #endregion

        #region Report definitions

        public static SystemReport SummaryInstance => new()
        {
            ViewType = typeof(DBOptionsReport),
            ReportName = "DB Options",
            SchemaName = "dbo",
            ProcedureName = "DBOptionsSummary_Get",
            QualifiedProcedureName = "dbo.DBOptionsSummary_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string> { CollectionType.Databases.ToString() },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Summary",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Instance", new ColumnMetadata { Alias = "Instance", Description = "Instance group name", Link = DrillDownLink() }},
                            { "InstanceID", new ColumnMetadata { Visible = false } },
                            { "Page Verify Not Optimal", SummaryColumnWithHighlighting("Page Verify\nNot Optimal", "Page verify should be set to CHECKSUM which can help detect corruption", DBADashStatus.DBADashStatusEnum.Critical, "Page Verify Not Optimal", "page_verify_option <> 2") },
                            { "Auto Close", SummaryColumnWithHighlighting("Auto\nClose", "Auto close should be set to OFF. Opening and closing the database after each connection can result in performance issues", DBADashStatus.DBADashStatusEnum.Critical, "Auto Close", "is_auto_close_on = 1") },
                            { "Auto Shrink", SummaryColumnWithHighlighting("Auto\nShrink", "Auto shrink should be set to OFF. Constant growing and shrinking of database files will result in performance issues", DBADashStatus.DBADashStatusEnum.Critical, "Auto Shrink", "is_auto_shrink_on = 1") },
                            { "Auto Create Stats Disabled", SummaryColumnWithHighlighting("Auto Create\nStats Disabled", "Auto create statistics should be enabled", DBADashStatus.DBADashStatusEnum.Warning, "Auto Create Stats Disabled", "is_auto_create_stats_on = 0") },
                            { "Auto Update Stats Disabled", SummaryColumnWithHighlighting("Auto Update\nStats Disabled", "Auto update statistics should be enabled", DBADashStatus.DBADashStatusEnum.Warning, "Auto Update Stats Disabled", "is_auto_update_stats_on = 0") },
                            { "Old Compat Level", new ColumnMetadata
                                {
                                    Alias = "Old Compat\nLevel",
                                    Description = "Use the latest compatibility level to benefit from new performance improvements",
                                    Link = DrillDownLinkDynamic(row => $"compatibility_level < {Convert.ToInt32(row.Cells["Max Supported Compatibility Level"].Value)}"),
                                    Highlighting = CountStatusRuleSet("Old Compat Level", DBADashStatus.DBADashStatusEnum.Warning),
                                }
                            },
                            { "Trustworthy", SummaryColumnWithHighlighting("Trust\nworthy", "Trustworthy should be set to OFF. This setting has security risks", DBADashStatus.DBADashStatusEnum.Warning, "Trustworthy", "is_trustworthy_on = 1 AND DatabaseName <> 'msdb'") },
                            { "Online", new ColumnMetadata { Description = "Database is online ready for queries", Link = DrillDownLink("state = 0") } },
                            { "Offline", SummaryColumnWithHighlighting("Offline", "Database has been taken offline and is unavailable", DBADashStatus.DBADashStatusEnum.Warning, "Offline", "state IN(6, 10)") },
                            { "Restoring", new ColumnMetadata { Description = "Database is restoring", Link = DrillDownLink("state = 1") } },
                            { "Recovering", SummaryColumnWithHighlighting("Recovering", "Database is waiting for recovery process to complete", DBADashStatus.DBADashStatusEnum.Warning, "Recovering", "state = 2") },
                            { "Recovery Pending", SummaryColumnWithHighlighting("Recovery\nPending", "SQL Server encountered a resource related error during recovery", DBADashStatus.DBADashStatusEnum.Critical, "Recovery Pending", "state = 3") },
                            { "Suspect", SummaryColumnWithHighlighting("Suspect", "Database couldn't be recovered and might be damaged", DBADashStatus.DBADashStatusEnum.Critical, "Suspect", "state = 4") },
                            { "Emergency", SummaryColumnWithHighlighting("Emergency", "User has changed the database state to EMERGENCY", DBADashStatus.DBADashStatusEnum.Critical, "Emergency", "state = 5") },
                            { "Standby", new ColumnMetadata { Description = "Database is restoring and has been brought online with STANDBY", Link = DrillDownLink("is_in_standby = 1") } },
                            { "Max VLF Count", new ColumnMetadata
                                {
                                    Alias = "Max VLF\nCount",
                                    Description = "Too many virtual log files (VLF) can slow down log backups and database recovery",
                                    Link = DrillDownLinkDynamic(row => row.Cells["Max VLF Count"].Value == DBNull.Value ? null : $"VLFCount >= {Math.Min(MAX_VLF_WARNING_THRESHOLD + 1, Convert.ToInt32(row.Cells["Max VLF Count"].Value))}"),
                                    Highlighting = new CellHighlightingRuleSet("Max VLF Count")
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.IsNull, Status = DBADashStatus.DBADashStatusEnum.NA },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "10000", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "1000", Status = DBADashStatus.DBADashStatusEnum.Warning },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.OK },
                                        }
                                    }
                                }
                            },
                            { "Not Using Indirect Checkpoints", SummaryColumnWithHighlighting("Not Using\nIndirect\nCheckpoints", "Indirect checkpoints can improve database recovery time and reduce checkpoint related I/O spiking", DBADashStatus.DBADashStatusEnum.Warning, "Not Using Indirect Checkpoints", "target_recovery_time_in_seconds=0 and database_id>4") },
                            { "None-Default Target Recovery Time", SummaryColumnWithHighlighting("Non-Default\nTarget\nRecovery Time", "Defaults: 0=Automatic checkpoints, 60=Indirect checkpoints (default from SQL 2016)", DBADashStatus.DBADashStatusEnum.Warning, "None-Default Target Recovery Time", "target_recovery_time_in_seconds NOT IN(0,60)") },
                            { "Max Supported Compatibility Level", new ColumnMetadata { Visible = false } },
                            { "RCSI Count", new ColumnMetadata { Alias = "RCSI\nCount", Description = "Count of user databases using read committed snapshot isolation level", Link = DrillDownLink("is_read_committed_snapshot_on=1 AND database_id > 4") } },
                            { "User Database Count", new ColumnMetadata { Alias = "User DB\nCount", Description = "Count of databases excluding system databases", Link = DrillDownLink("database_id > 4") } },
                            { "ADR", new ColumnMetadata { Description = "Count of databases with ADR (Accelerated Database Recovery) enabled", Link = DrillDownLink("is_accelerated_database_recovery_on=1") } },
                            { "Optimized Locking", new ColumnMetadata { Alias = "Optimized\nLocking", Description = "Count of databases with optimized locking enabled (SQL 2025)", Link = DrillDownLink("is_optimized_locking_on=1") } },
                            { "TDE", new ColumnMetadata { Description = "Count of user databases with Transparent data encryption (TDE) enabled", Link = DrillDownLink("is_encrypted = 1 AND database_id > 4") } },
                            { "Read Only", new ColumnMetadata { Alias = "Read\nOnly", Description = "Count of databases marked read only", Link = DrillDownLink("is_read_only = 1") } },
                            { "CDC", new ColumnMetadata { Description = "Count of databases where change data capture (CDC) is enabled", Link = DrillDownLink("is_cdc_enabled = 1") } },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Options History",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Instance", new ColumnMetadata { Alias = "Instance", Description = "Instance name" } },
                            { "InstanceGroupName", new ColumnMetadata { Alias = "Instance\nGroup", Description = "Instance group name" } },
                            { "DB", new ColumnMetadata { Alias = "Database", Description = "Database name" } },
                            { "Setting", new ColumnMetadata { Alias = "Setting", Description = "Setting that was changed" } },
                            { "OldValue", new ColumnMetadata { Alias = "Old Value", Description = "Previous value of the setting" } },
                            { "NewValue", new ColumnMetadata { Alias = "New Value", Description = "New value of the setting" } },
                            { "ChangeDate", new ColumnMetadata { Alias = "Change Date", Description = "Date and time the change occurred" } },
                        }
                    }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@ExcludeStateChanges",
                    Name = "Exclude State Changes",
                    PickerItems = new Dictionary<object, string>
                    {
                        { true, "Yes" },
                        { false, "No" },
                    },
                    DefaultValue = true,
                    MenuBar = false,
                    DataType = typeof(bool)
                },
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceIDs",
                        ParamType = "IDS",
                    },
                    new()
                    {
                        ParamName = "@DatabaseID",
                        ParamType = "INT",
                    },
                    new()
                    {
                        ParamName = "@ExcludeStateChanges",
                        ParamType = "BIT",
                    },
                }
            },
        };

        public static SystemReport DetailInstance => new()
        {
            ViewType = typeof(DBOptionsReport),
            ReportName = "DB Options Detail",
            SchemaName = "dbo",
            ProcedureName = "DBOptionsDetail_Get",
            QualifiedProcedureName = "dbo.DBOptionsDetail_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string> { CollectionType.Databases.ToString() },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Detail",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Instance", new ColumnMetadata { Alias = "Instance", Description = "Instance group name" } },
                            { "DatabaseID", new ColumnMetadata { Visible = false } },
                            { "InstanceID", new ColumnMetadata { Visible = false } },
                            { "DatabaseName", new ColumnMetadata { Alias = "Database\nName", Description = "Database name" } },
                            { "database_id", new ColumnMetadata { Alias = "DB\nID", Description = "Database ID" } },
                            { "compatibility_level", new ColumnMetadata { Alias = "Compat\nLevel", Description = "Database compatibility level" } },
                            { "MaxSupportedCompatibilityLevel", new ColumnMetadata { Visible = false } },
                            { "state", new ColumnMetadata
                                {
                                    Alias = "State",
                                    Description = "Database state code",
                                    Highlighting = new CellHighlightingRuleSet("state")
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.OK },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "2", Status = DBADashStatus.DBADashStatusEnum.Warning },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "3", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "4", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "5", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.NA },
                                        }
                                    }
                                }
                            },
                            { "state_desc", new ColumnMetadata
                                {
                                    Alias = "State\nDesc",
                                    Description = "Database state description",
                                    Highlighting = new CellHighlightingRuleSet("state", evaluateConditionAgainstDataSource: true)
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.OK },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "2", Status = DBADashStatus.DBADashStatusEnum.Warning },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "3", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "4", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "5", Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.NA },
                                        }
                                    }
                                }
                            },
                            { "recovery_model_desc", new ColumnMetadata { Alias = "Recovery\nModel", Description = "Database recovery model" } },
                            { "page_verify_option", new ColumnMetadata
                                {
                                    Alias = "Page\nVerify\nOption",
                                    Description = "Page verify option code",
                                    Highlighting = new CellHighlightingRuleSet("page_verify_option_desc", evaluateConditionAgainstDataSource: true)
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "CHECKSUM", Status = DBADashStatus.DBADashStatusEnum.OK },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Critical },
                                        }
                                    }
                                }
                            },
                            { "page_verify_option_desc", new ColumnMetadata
                                {
                                    Alias = "Page\nVerify",
                                    Description = "Page verify should be set to CHECKSUM which can help detect corruption",
                                    Highlighting = new CellHighlightingRuleSet("page_verify_option_desc")
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "CHECKSUM", Status = DBADashStatus.DBADashStatusEnum.OK },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Critical },
                                        }
                                    }
                                }
                            },
                            { "is_auto_close_on", new ColumnMetadata
                                {
                                    Alias = "Auto\nClose",
                                    Description = "Auto close should be set to OFF",
                                    Highlighting = BoolFalseIsOk("is_auto_close_on", DBADashStatus.DBADashStatusEnum.Critical),
                                }
                            },
                            { "is_auto_shrink_on", new ColumnMetadata
                                {
                                    Alias = "Auto\nShrink",
                                    Description = "Auto shrink should be set to OFF",
                                    Highlighting = BoolFalseIsOk("is_auto_shrink_on", DBADashStatus.DBADashStatusEnum.Critical),
                                }
                            },
                            { "is_auto_create_stats_on", new ColumnMetadata
                                {
                                    Alias = "Auto\nCreate\nStats",
                                    Description = "Auto create statistics should be enabled",
                                    Highlighting = BoolTrueIsOk("is_auto_create_stats_on", DBADashStatus.DBADashStatusEnum.Warning),
                                }
                            },
                            { "is_auto_update_stats_on", new ColumnMetadata
                                {
                                    Alias = "Auto\nUpdate\nStats",
                                    Description = "Auto update statistics should be enabled",
                                    Highlighting = BoolTrueIsOk("is_auto_update_stats_on", DBADashStatus.DBADashStatusEnum.Warning),
                                }
                            },
                            { "is_trustworthy_on", new ColumnMetadata { Alias = "Trust\nworthy", Description = "Trustworthy should be set to OFF" } },
                            { "target_recovery_time_in_seconds", new ColumnMetadata
                                {
                                    Alias = "Target\nRecovery\nTime (s)",
                                    Description = "Defaults: 0=Automatic checkpoints, 60=Indirect checkpoints",
                                    Highlighting = new CellHighlightingRuleSet("target_recovery_time_in_seconds")
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.IsNull, Status = DBADashStatus.DBADashStatusEnum.NA },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "60", Status = DBADashStatus.DBADashStatusEnum.OK },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning },
                                        }
                                    }
                                }
                            },
                            { "VLFCount", new ColumnMetadata
                                {
                                    Alias = "VLF\nCount",
                                    Description = "Too many virtual log files can slow down log backups and database recovery",
                                    FormatString = "N0",
                                    Highlighting = new CellHighlightingRuleSet("VLFCount")
                                    {
                                        Rules = new List<CellHighlightingRule>
                                        {
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.IsNull, Status = DBADashStatus.DBADashStatusEnum.NA },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = MAX_VLF_CRITICAL_THRESHOLD.ToString(), Status = DBADashStatus.DBADashStatusEnum.Critical },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = MAX_VLF_WARNING_THRESHOLD.ToString(), Status = DBADashStatus.DBADashStatusEnum.Warning },
                                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.OK },
                                        }
                                    }
                                }
                            },
                            { "is_read_committed_snapshot_on", new ColumnMetadata { Alias = "RCSI", Description = "Read Committed Snapshot Isolation" } },
                            { "is_accelerated_database_recovery_on", new ColumnMetadata { Alias = "ADR", Description = "Accelerated Database Recovery enabled" } },
                            { "is_optimized_locking_on", new ColumnMetadata { Alias = "Optimized\nLocking", Description = "Optimized locking enabled (SQL 2025)" } },
                            { "is_encrypted", new ColumnMetadata { Alias = "TDE", Description = "Transparent data encryption enabled" } },
                            { "is_read_only", new ColumnMetadata { Alias = "Read\nOnly", Description = "Database is read only" } },
                            { "is_cdc_enabled", new ColumnMetadata { Alias = "CDC", Description = "Change data capture enabled" } },
                            { "is_in_standby", new ColumnMetadata { Alias = "Standby", Description = "Database is in standby mode" } },
                            { "LastGoodCheckDbTime", new ColumnMetadata
                                {
                                    Alias = "Last Good\nCheckDB",
                                    Description = "Last good CHECKDB time",
                                    Highlighting = new CellHighlightingRuleSet("LastGoodCheckDBStatus", evaluateConditionAgainstDataSource: true)
                                    {
                                        IsStatusColumn = true,
                                    }
                                }
                            },
                            { "LastGoodCheckDBStatus", new ColumnMetadata
                                {
                                    Visible = false,
                                    Highlighting = new CellHighlightingRuleSet("LastGoodCheckDBStatus")
                                    {
                                        IsStatusColumn = true,
                                    }
                                }
                            },
                            { "collation_name", new ColumnMetadata { Alias = "Collation", Description = "Database collation" } },
                            { "is_query_store_on", new ColumnMetadata { Alias = "Query\nStore", Description = "Query store enabled" } },
                            { "is_broker_enabled", new ColumnMetadata { Alias = "Broker\nEnabled", Description = "Service broker enabled" } },
                            { "log_reuse_wait_desc", new ColumnMetadata { Alias = "Log Reuse\nWait", Description = "Log reuse wait description" } },
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Options History",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Instance", new ColumnMetadata { Alias = "Instance", Description = "Instance name" } },
                            { "InstanceGroupName", new ColumnMetadata { Alias = "Instance\nGroup", Description = "Instance group name" } },
                            { "DB", new ColumnMetadata { Alias = "Database", Description = "Database name" } },
                            { "Setting", new ColumnMetadata { Alias = "Setting", Description = "Setting that was changed" } },
                            { "OldValue", new ColumnMetadata { Alias = "Old Value", Description = "Previous value of the setting" } },
                            { "NewValue", new ColumnMetadata { Alias = "New Value", Description = "New value of the setting" } },
                            { "ChangeDate", new ColumnMetadata { Alias = "Change Date", Description = "Date and time the change occurred" } },
                        }
                    }
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@ExcludeStateChanges",
                    Name = "Exclude State Changes",
                    PickerItems = new Dictionary<object, string>
                    {
                        { true, "Yes" },
                        { false, "No" },
                    },
                    DefaultValue = true,
                    MenuBar = true,
                    DataType = typeof(bool)
                },
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceIDs",
                        ParamType = "IDS",
                    },
                    new()
                    {
                        ParamName = "@DatabaseID",
                        ParamType = "INT",
                    },
                    new()
                    {
                        ParamName = "@InstanceGroupName",
                        ParamType = "NVARCHAR",
                    },
                    new()
                    {
                        ParamName = "@ExcludeStateChanges",
                        ParamType = "BIT",
                    },
                }
            },
        };

        #endregion
    }
}
