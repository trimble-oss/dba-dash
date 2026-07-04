using DBADash;
using DBADashGUI.CustomReports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.LastGoodCheckDB
{
    internal class LastGoodCheckDBView : CustomReportView
    {
        private ToolStripDropDownButton tsConfigure;
        private ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;

        public LastGoodCheckDBView()
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
                ToolTipText = "Configure Last Good CheckDB thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            configureInstanceThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Instance Thresholds");
            configureInstanceThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id))
                    ConfigureThresholds(id, -1);
            };

            var configureRootThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Root Thresholds");
            configureRootThresholdsToolStripMenuItem.Click += (_, _) => ConfigureThresholds(-1, -1);

            tsConfigure.DropDownItems.AddRange(new ToolStripItem[]
            {
                configureInstanceThresholdsToolStripMenuItem,
                configureRootThresholdsToolStripMenuItem
            });

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigure);
        }

        private void UpdateToolbarState()
        {
            configureInstanceThresholdsToolStripMenuItem.Enabled = TryGetSingleInstanceId(out _);
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

        public void ConfigureThresholds(int instanceID, int databaseID)
        {
            var frm = new LastGoodCheckDBConfig
            {
                Threshold = LastGoodCheckDBThreshold.GetLastGoodCheckDBThreshold(instanceID, databaseID)
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        #endregion Configuration

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            foreach (var grid in Grids)
            {
                grid.RowsAdded -= Grid_RowsAdded;
                grid.RowsAdded += Grid_RowsAdded;
                ApplyConfiguredLevelStyling(grid, 0, grid.RowCount);
            }
            UpdateToolbarState();
        }

        private void Grid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is DBADashDataGridView grid)
            {
                ApplyConfiguredLevelStyling(grid, e.RowIndex, e.RowCount);
            }
        }

        // The Configure column is shown in bold when a threshold is configured at the database level specifically
        // (rather than inherited from the instance/root) - this doesn't fit the declarative CellHighlightingRuleSet
        // model (which colors cells, not fonts), so it's applied here instead, hooked to RowsAdded so it survives
        // grid sorting (sorting a bound DataGridView resets and re-adds all rows).
        private static void ApplyConfiguredLevelStyling(DBADashDataGridView grid, int startIndex, int rowCount)
        {
            if (grid.Columns["Configure"] == null) return;
            Font boldFont = null;

            for (var idx = startIndex; idx < startIndex + rowCount && idx < grid.Rows.Count; idx++)
            {
                var row = grid.Rows[idx];
                if (row.DataBoundItem is not DataRowView drv) continue;

                var isConfiguredHere = drv["ConfiguredLevel"] != DBNull.Value && (string)drv["ConfiguredLevel"] == "Database";
                row.Cells["Configure"].Style.Font = isConfiguredHere ? boldFont ??= new Font(grid.Font, FontStyle.Bold) : grid.Font;
            }

            grid.Columns["Configure"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        #endregion Grid post-processing

        #region Custom links

        private sealed class ConfigureLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not LastGoodCheckDBView view) return;
                var instanceId = (int)row.Cells["InstanceID"].Value;
                var databaseId = (int)row.Cells["DatabaseID"].Value;
                view.ConfigureThresholds(instanceId, databaseId);
            }
        }

        #endregion Custom links

        #region Report definition

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport ReportInstance => new()
        {
            ViewType = typeof(LastGoodCheckDBView),
            ReportName = "Last Good CheckDB",
            SchemaName = "dbo",
            ProcedureName = "LastGoodCheckDBReport_Get",
            QualifiedProcedureName = "dbo.LastGoodCheckDBReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.LastGoodCheckDB.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Last Good CheckDB",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["name"] = new() { Alias = "Database" },
                        ["LastGoodCheckDbTimeUTC"] = new() { Alias = "Last Good Check DB", Highlighting = new CellHighlightingRuleSet("Status") { IsStatusColumn = true } },
                        ["DaysSinceLastGoodCheckDB"] = new() { Alias = "Days Since Last Good CheckDB", Highlighting = new CellHighlightingRuleSet("Status") { IsStatusColumn = true } },
                        ["create_date_utc"] = new() { Alias = "Create Date" },
                        ["LastGoodCheckDBExcludedReason"] = new() { Alias = "Excluded Reason" },
                        ["Configure"] = new() { Alias = "Configure", Link = new ConfigureLink(), Description = "Configure Last Good CheckDB thresholds" },
                        // Hidden columns
                        ["InstanceID"] = Hidden(),
                        ["DatabaseID"] = Hidden(),
                        ["Instance"] = Hidden(),
                        ["state"] = Hidden(),
                        ["state_desc"] = Hidden(),
                        ["is_in_standby"] = Hidden(),
                        ["HrsSinceLastGoodCheckDB"] = Hidden(),
                        ["Status"] = Hidden(),
                        ["StatusDescription"] = Hidden(),
                        ["ConfiguredLevel"] = Hidden(),
                        ["WarningThresholdHrs"] = Hidden(),
                        ["CriticalThresholdHrs"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
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
