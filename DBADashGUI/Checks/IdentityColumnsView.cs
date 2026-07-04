using DBADash;
using DBADashGUI.CustomReports;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    internal class IdentityColumnsView : CustomReportView
    {
        private ToolStripDropDownButton tsConfigure;
        private ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;
        private ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;

        public IdentityColumnsView()
        {
            Report = ReportInstance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigure = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigure",
                ToolTipText = "Configure Identity Columns thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var configureRootThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Root Thresholds");
            configureRootThresholdsToolStripMenuItem.Click += (_, _) => ConfigureThresholds(-1, -1, "");

            configureInstanceThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Instance Thresholds");
            configureInstanceThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id))
                    ConfigureThresholds(id, -1, "");
            };

            configureDatabaseThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Database Thresholds");
            configureDatabaseThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id) && context.DatabaseID > 0)
                    ConfigureThresholds(id, context.DatabaseID, "");
            };

            tsConfigure.DropDownItems.AddRange(new ToolStripItem[]
            {
                configureRootThresholdsToolStripMenuItem,
                configureInstanceThresholdsToolStripMenuItem,
                configureDatabaseThresholdsToolStripMenuItem
            });

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigure);
        }

        private void UpdateToolbarState()
        {
            configureInstanceThresholdsToolStripMenuItem.Enabled = TryGetSingleInstanceId(out _);
            configureDatabaseThresholdsToolStripMenuItem.Enabled = TryGetSingleInstanceId(out _) && context?.DatabaseID > 0;
        }

        private bool TryGetSingleInstanceId(out int instanceId)
        {
            instanceId = -1;
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

        #endregion Toolbar

        #region Configuration

        public void ConfigureThresholds(int instanceID, int databaseID, string objectName)
        {
            using var frm = new IdentityColumnsThresholdConfig { InstanceID = instanceID, DatabaseID = databaseID, ObjectName = objectName };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        #endregion Configuration

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            UpdateToolbarState();
        }

        #region Custom links

        private sealed class EditThresholdLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not IdentityColumnsView view) return;
                var instanceId = (int)row.Cells["InstanceID"].Value;
                var databaseId = (int)row.Cells["DatabaseID"].Value;
                var objectName = (string)row.Cells["object_name"].Value;
                view.ConfigureThresholds(instanceId, databaseId, objectName);
            }
        }

        #endregion Custom links

        #region Report definition

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport ReportInstance => new()
        {
            ViewType = typeof(IdentityColumnsView),
            ReportName = "Identity Columns",
            SchemaName = "dbo",
            ProcedureName = "IdentityColumnsReport_Get",
            QualifiedProcedureName = "dbo.IdentityColumnsReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.IdentityColumns.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Identity Columns",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["DB"] = new() { Alias = "Database" },
                        ["schema_name"] = new() { Alias = "Schema" },
                        ["object_name"] = new() { Alias = "Table" },
                        ["column_name"] = new() { Alias = "Column" },
                        ["type"] = new() { Alias = "Type", Description = "Data type for identity value" },
                        ["min_ident"] = new() { Alias = "Min Identity", Visible = false, Description = "Minimum identity value supported by the column type." },
                        ["max_ident"] = new() { Alias = "Max Identity", Visible = false, Description = "Maximum identity value supported by the column type." },
                        ["max_rows"] = new() { Alias = "Max Rows", Visible = false, Description = "Maximum rows possible with unique identity values." },
                        ["increment_value"] = new() { Alias = "Increment", FormatString = "N0", Visible = false, Description = "Identity increment value" },
                        ["seed_value"] = new() { Alias = "Seed", FormatString = "N0", Visible = false, Description = "Identity seed value" },
                        ["row_count"] = new() { Alias = "Rows", FormatString = "N0", Description = "Number of rows in table" },
                        ["last_value"] = new() { Alias = "Last Identity", FormatString = "N0", Description = "Last identity value used" },
                        ["pct_used"] = new()
                        {
                            Alias = "% Used",
                            FormatString = "P1",
                            Description = "Percent used. The greatest value of % Ident Used and % Rows Used.",
                            Highlighting = new CellHighlightingRuleSet("IdentityPctStatus") { IsStatusColumn = true }
                        },
                        ["pct_free"] = new()
                        {
                            Alias = "% Free",
                            FormatString = "P1",
                            Description = "Percent free.  Calculated from % used.",
                            Highlighting = new CellHighlightingRuleSet("IdentityPctStatus") { IsStatusColumn = true }
                        },
                        ["pct_ident_used"] = new() { Alias = "% Ident Used", FormatString = "P1", Description = "Percent of identity values used. Calculated as last identity value as a % of max identity value.  If last identity value is negative it's a % of max rows" },
                        ["pct_rows_used"] = new() { Alias = "% Rows Used", FormatString = "P1", Description = "Percentage of rows used.  Calculated as row count as a % of max rows" },
                        ["remaining_ident_count"] = new() { Alias = "Ident Remaining", FormatString = "N0", Description = "Number of identity values remaining based on max identity value and last identity value." },
                        ["remaining_row_count"] = new() { Alias = "Rows Remaining", FormatString = "N0", Description = "Number of rows remaining based on Max Rows and Rows." },
                        ["avg_ident_per_day"] = new() { Alias = "Avg Ident/day", FormatString = "N0", Description = "Avg identity values used per day over the last ~month (Calc Days)" },
                        ["avg_rows_per_day"] = new() { Alias = "Avg Rows/day", FormatString = "N0", Description = "Avg rows added to the table per day of the last ~month (Calc Days)" },
                        ["avg_calc_days"] = new() { Alias = "Avg Calc Days", FormatString = "N0", Visible = false, Description = "Number of days Avg Ident/day and Avg Rows/day have been calculated over" },
                        ["estimated_days"] = new()
                        {
                            Alias = "Estimated Days",
                            FormatString = "N0",
                            Description = "Estimated days remaining until table runs out of identity values",
                            Highlighting = new CellHighlightingRuleSet("IdentityDaysStatus") { IsStatusColumn = true }
                        },
                        ["estimated_date"] = new()
                        {
                            Alias = "Estimated Date",
                            FormatString = "d",
                            NullValue = ">100 years",
                            Description = "Estimated date table will run out of identity values",
                            Highlighting = new CellHighlightingRuleSet("IdentityDaysStatus") { IsStatusColumn = true }
                        },
                        ["ident_estimated_days"] = new() { Alias = "Ident Estimated Days", FormatString = "N0", Visible = false, Description = "Estimated days remaining until table runs out of identity values.  Based on Ident Remaining and Avg Ident/day" },
                        ["row_estimated_days"] = new() { Alias = "Row Estimated Days", FormatString = "N0", Visible = false, Description = "Estimated days remaining until table runs out of identity values.  Based on Rows Remaining and Avg Rows/day" },
                        ["SnapshotDate"] = new()
                        {
                            Alias = "Snapshot Date",
                            Description = "Date identity data was collected from the SQL instance",
                            Highlighting = new CellHighlightingRuleSet("SnapshotStatus") { IsStatusColumn = true }
                        },
                        ["PctUsedWarningThreshold"] = new() { Alias = "Warning Threshold (%)", FormatString = "P1", Visible = false },
                        ["PctUsedCriticalThreshold"] = new() { Alias = "Critical Threshold (%)", FormatString = "P1", Visible = false },
                        ["DaysWarningThreshold"] = new() { Alias = "Warning Threshold (Days)", Visible = false },
                        ["DaysCriticalThreshold"] = new() { Alias = "Critical Threshold (Days)", Visible = false },
                        ["ThresholdConfigurationLevel"] = new() { Alias = "Config Level", Visible = false },
                        ["Edit"] = new() { Alias = "Edit", Link = new EditThresholdLink(), Description = "Configure thresholds for this table" },
                        // Hidden columns
                        ["InstanceID"] = Hidden(),
                        ["DatabaseID"] = Hidden(),
                        ["object_id"] = Hidden(),
                        ["SnapshotStatus"] = Hidden(),
                        ["IdentityStatus"] = Hidden(),
                        ["IdentityPctStatus"] = Hidden(),
                        ["IdentityDaysStatus"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@DatabaseID", ParamType = "INT" },
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
