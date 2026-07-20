using DBADash;
using DBADashGUI.CustomReports;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.DBFiles
{
    internal class DBFilesView : CustomReportView
    {
        private ToolStripStatusLabel lblStats;
        private ToolStripDropDownButton tsConfigure;
        private ToolStripMenuItem configureDatabaseThresholdsToolStripMenuItem;
        private ToolStripMenuItem configureInstanceThresholdsToolStripMenuItem;

        public DBFilesView()
        {
            Report = ReportInstance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
            AddStatusBarLabels();
        }

        private void AddStatusBarLabels()
        {
            lblStats = new ToolStripStatusLabel { Name = "lblStats" };
            var sep = new ToolStripStatusLabel("|") { Name = "tsStatsSep" };
            StatusStrip.Items.Insert(0, lblStats);
            StatusStrip.Items.Insert(1, sep);
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigure = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigure",
                ToolTipText = "Configure file thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            configureDatabaseThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Database Thresholds");
            configureDatabaseThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id) && GetDatabaseID() > 0)
                    ConfigureThresholds(id, (int)GetDatabaseID(), -1);
            };

            configureInstanceThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Instance Thresholds");
            configureInstanceThresholdsToolStripMenuItem.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id))
                    ConfigureThresholds(id, -1, -1);
            };

            var configureRootThresholdsToolStripMenuItem = new ToolStripMenuItem("Configure Root Thresholds");
            configureRootThresholdsToolStripMenuItem.Click += (_, _) => ConfigureThresholds(-1, -1, -1);

            tsConfigure.DropDownItems.AddRange(new ToolStripItem[]
            {
                configureDatabaseThresholdsToolStripMenuItem,
                configureInstanceThresholdsToolStripMenuItem,
                configureRootThresholdsToolStripMenuItem
            });

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigure);
        }


        private void UpdateToolbarState()
        {
            var singleInstance = TryGetSingleInstanceId(out _);
            configureInstanceThresholdsToolStripMenuItem.Enabled = singleInstance;
            configureDatabaseThresholdsToolStripMenuItem.Enabled = singleInstance && GetDatabaseID() > 0;
            SetLevelPickerVisibility();
        }

        private void SetLevelPickerVisibility()
        {
            var visible = context?.DriveName == null;
            foreach (var item in ToolStrip.Items.Cast<ToolStripItem>().Where(i => i.Tag?.ToString() == "Picker" && i.Text == "Level"))
            {
                item.Visible = visible;
            }
        }

        #endregion Toolbar

        #region Parameters

        private bool IsFileGroupLevel
        {
            get
            {
                var p = customParams?.FirstOrDefault(p =>
                    p.Param.ParameterName.Equals("@FilegroupLevel", StringComparison.OrdinalIgnoreCase));
                if (p == null || p.UseDefaultValue) return true;
                return p.Param.Value is true or 1;
            }
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

        private int? GetDatabaseID()
        {
            return context?.DatabaseID > 0 ? context.DatabaseID : null;
        }

        protected override void OnContextChanged(bool isDrillDown)
        {
            base.OnContextChanged(isDrillDown);
            SetLevelPickerVisibility();
        }

        protected override void OnBeforeRefresh()
        {
            base.OnBeforeRefresh();

            SetParam("@DatabaseID", GetDatabaseID().HasValue ? GetDatabaseID() : DBNull.Value);
            SetParam("@DriveName", context?.DriveName != null ? context.DriveName : DBNull.Value);

            // The @DriveName filter matches against physical_name, which only exists at the file level
            // (dbo.FileStatus). Force file-level so the drive filtering works - the Level picker is also
            // hidden in this case (see SetLevelPickerVisibility).
            if (context?.DriveName != null)
            {
                SetParam("@FilegroupLevel", false);
            }
        }

        private void SetParam(string paramName, object value)
        {
            var p = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            if (p == null) return;
            p.Param.Value = value;
            p.UseDefaultValue = false;
        }


        #endregion Parameters

        #region Configuration

        public void ConfigureThresholds(int instanceID, int databaseID, int dataSpaceID)
        {
            var frm = new FileThresholdConfig
            {
                InstanceID = instanceID,
                DataSpaceID = dataSpaceID,
                DatabaseID = databaseID
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
                ToggleFileLevel(grid);
                grid.RowsAdded -= Grid_RowsAdded;
                grid.RowsAdded += Grid_RowsAdded;
                ApplyThresholdStyling(grid, 0, grid.RowCount);
                grid.GridFilterChanged -= Grid_GridFilterChanged;
                grid.GridFilterChanged += Grid_GridFilterChanged;
            }
            UpdateToolbarState();
            UpdateTotals();
        }

        private void Grid_GridFilterChanged(object sender, EventArgs e) => UpdateTotals();

        // FilegroupPctFree/FilegroupFreeMB status depends on both FreeSpaceStatus and FreeSpaceCheckType, and
        // the Configure column font depends on ConfiguredLevel/data_space_id - neither fits the declarative
        // CellHighlightingRuleSet model (single source column per target), so they're applied here instead.
        // This is hooked to RowsAdded (rather than a one-shot pass) so formatting survives grid sorting, since
        // sorting a bound DataGridView resets and re-adds all rows.
        private void Grid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is DBADashDataGridView grid)
            {
                ApplyThresholdStyling(grid, e.RowIndex, e.RowCount);
            }
        }

        private void UpdateTotals()
        {
            var grid = Grids.FirstOrDefault();
            if (grid == null) return;

            double totalSize;
            int totalFiles;
            if (IsFileGroupLevel)
            {
                totalFiles = grid.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && row.Cells["FilegroupNumberOfFiles"].Value != null)
                    .Sum(row => int.Parse(row.Cells["FilegroupNumberOfFiles"].Value.ToString()!));

                totalSize = grid.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && row.Cells["FilegroupSizeMB"].Value != null)
                    .Sum(row => Convert.ToDouble(row.Cells["FilegroupSizeMB"].Value.ToString()));
            }
            else
            {
                totalFiles = grid.RowCount;
                totalSize = grid.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && row.Cells["FileSizeMB"].Value != null)
                    .Sum(row => Convert.ToDouble(row.Cells["FileSizeMB"].Value.ToString()));
            }
            lblStats.Text = $"File Count {totalFiles}. Total Size: {totalSize.Megabytes()}";
        }

        private void ToggleFileLevel(DBADashDataGridView grid)
        {
            var isFileLevel = !IsFileGroupLevel;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Name.StartsWith("FileLevel_"))
                {
                    col.Visible = isFileLevel;
                }
            }
        }

        private static void ApplyThresholdStyling(DBADashDataGridView grid, int startIndex, int rowCount)
        {
            var hasConfigureColumn = grid.Columns["Configure"] != null;
            // Reuse a single bold font across all rows (created lazily) and the grid's own font for the regular
            // case rather than allocating a Font per row every time this runs (including on every sort).
            Font boldFont = null;

            for (var idx = startIndex; idx < startIndex + rowCount && idx < grid.Rows.Count; idx++)
            {
                var row = grid.Rows[idx];
                if (row.DataBoundItem is not DataRowView drv) continue;

                var status = (DBADashStatusEnum)drv["FreeSpaceStatus"];
                var checkType = drv["FreeSpaceCheckType"] == DBNull.Value ? "-" : (string)drv["FreeSpaceCheckType"];

                if (grid.Columns["FilegroupPctFree"] != null)
                    row.Cells["FilegroupPctFree"].SetStatusColor(checkType == "%" ? status : DBADashStatusEnum.NA);
                if (grid.Columns["FilegroupFreeMB"] != null)
                    row.Cells["FilegroupFreeMB"].SetStatusColor(checkType == "M" ? status : DBADashStatusEnum.NA);

                if (hasConfigureColumn)
                {
                    var isConfiguredHere = drv["ConfiguredLevel"] != DBNull.Value &&
                                            ((string)drv["ConfiguredLevel"] == "FG" ||
                                             ((string)drv["ConfiguredLevel"] == "DB" && (int)drv["data_space_id"] == 0));
                    row.Cells["Configure"].Style.Font =
                        isConfiguredHere ? boldFont ??= new Font(grid.Font, FontStyle.Bold) : grid.Font;
                }
            }

            if (hasConfigureColumn)
            {
                grid.Columns["Configure"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        #endregion Grid post-processing

        #region Custom links

        private sealed class FileConfigureLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not DBFilesView view) return;
                var instanceId = (int)row.Cells["InstanceID"].Value;
                var databaseId = (int)row.Cells["DatabaseID"].Value;
                var dataSpaceId = (int)row.Cells["data_space_id"].Value;
                view.ConfigureThresholds(instanceId, databaseId, dataSpaceId);
            }
        }

        private sealed class FileHistoryLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                var dbId = (int)row.Cells["DatabaseID"].Value;
                var dataSpaceId = row.Cells["data_space_id"].Value == DBNull.Value ? null : (int?)row.Cells["data_space_id"].Value;
                var instance = (string)row.Cells["InstanceGroupName"].Value;
                var dbName = (string)row.Cells["name"].Value;
                var fileName = row.Cells["file_name"].Value == DBNull.Value ? null : (string)row.Cells["file_name"].Value;

                var frm = new DBSpaceHistoryView
                {
                    DatabaseID = dbId,
                    DataSpaceID = dataSpaceId,
                    InstanceGroupName = instance,
                    DBName = dbName,
                    FileName = fileName
                };
                frm.ShowSingleInstance();
            }
        }

        #endregion Custom links

        #region Report definition

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport ReportInstance => new()
        {
            ViewType = typeof(DBFilesView),
            ReportName = "DB Files",
            SchemaName = "dbo",
            ProcedureName = "DBFilesReport_Get",
            QualifiedProcedureName = "dbo.DBFilesReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.DBFiles.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "DB Files",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["name"] = new() { Alias = "Database" },
                        ["file_name"] = new() { Alias = "File Name" },
                        ["type_desc"] = new() { Alias = "Type" },
                        ["physical_name"] = new() { Alias = "Physical Name" },
                        ["FileSizeMB"] = new() { Alias = "File Size (MB)", FormatString = "N1" },
                        ["FileUsedMB"] = new() { Alias = "File Used (MB)", FormatString = "N1" },
                        ["FileFreeMB"] = new() { Alias = "File Free (MB)", FormatString = "N1" },
                        ["FilePctFree"] = new() { Alias = "File Free %", FormatString = "P1" },
                        ["GrowthMB"] = new() { Alias = "Growth (MB)", FormatString = "#,##0.#" },
                        ["GrowthPct"] = new() { Alias = "Growth %" },
                        ["MaxSizeMB"] = new() { Alias = "Max Size (MB)", FormatString = "#,##0.#" },
                        ["Filegroup_name"] = new() { Alias = "Filegroup" },
                        ["FilegroupSizeMB"] = new() { Alias = "Filegroup Size (MB)", FormatString = "N1" },
                        ["FilegroupUsedMB"] = new() { Alias = "Filegroup Used (MB)", FormatString = "N1" },
                        ["FilegroupFreeMB"] = new() { Alias = "Filegroup Free (MB)", FormatString = "N1" },
                        ["FilegroupPctFree"] = new() { Alias = "Filegroup Free %", FormatString = "P1" },
                        ["FilegroupMaxSizeMB"] = new() { Alias = "Filegroup Max Size (MB)", FormatString = "N0" },
                        ["FilegroupPctOfMaxSize"] = new() { Alias = "Filegroup % Of Max Size", FormatString = "P1", Highlighting = new CellHighlightingRuleSet("PctMaxSizeStatus") { IsStatusColumn = true } },
                        ["FilegroupUsedPctOfMaxSize"] = new() { Alias = "Filegroup Used % Max Size", FormatString = "P1" },
                        ["FilegroupNumberOfFiles"] = new() { Alias = "Filegroup Number of Files" },
                        ["FilegroupAutogrowFileCount"] = new() { Alias = "Filegroup Autogrow File Count", Highlighting = new CellHighlightingRuleSet("FilegroupAutogrowStatus") { IsStatusColumn = true } },
                        ["ExcludedReason"] = new() { Alias = "Excluded Reason" },
                        ["MaxSizeExcludedReason"] = new() { Alias = "Max Size Excluded Reason" },
                        ["is_read_only"] = new() { Alias = "Read Only" },
                        ["is_db_read_only"] = new() { Alias = "DB ReadOnly" },
                        ["is_in_standby"] = new() { Alias = "Standby" },
                        ["state_desc"] = new() { Alias = "State" },
                        ["ConfiguredLevel"] = new() { Alias = "Configured Level" },
                        ["FileSnapshotAge"] = new() { Alias = "File Snapshot Age", Highlighting = new CellHighlightingRuleSet("FileSnapshotAgeStatus") { IsStatusColumn = true } },
                        ["History"] = new() { Alias = "History", Link = new FileHistoryLink(), Description = "View file space history" },
                        ["Configure"] = new() { Alias = "Configure", Link = new FileConfigureLink(), Description = "Configure file thresholds" },
                        // Hidden columns
                        ["FileID"] = Hidden(),
                        ["InstanceID"] = Hidden(),
                        ["DatabaseID"] = Hidden(),
                        ["data_space_id"] = Hidden(),
                        ["Instance"] = Hidden(),
                        ["InstanceGroupName"] = Hidden(),
                        ["ConnectionID"] = Hidden(),
                        ["FreeSpaceStatus"] = Hidden(),
                        ["FreeSpaceWarningThreshold"] = Hidden(),
                        ["FreeSpaceCriticalThreshold"] = Hidden(),
                        ["FreeSpaceCheckType"] = Hidden(),
                        ["FileSnapshotDate"] = Hidden(),
                        ["FileSnapshotAgeStatus"] = Hidden(),
                        ["PctMaxSizeStatus"] = Hidden(),
                        ["FilegroupAutogrowStatus"] = Hidden(),
                        ["type"] = Hidden(),
                        ["state"] = Hidden(),
                        ["max_size"] = Hidden(),
                        ["growth"] = Hidden(),
                        ["is_percent_growth"] = Hidden(),
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
                    new() { ParamName = "@DatabaseID", ParamType = "INT" },
                    new() { ParamName = "@FilegroupLevel", ParamType = "BIT" },
                    new() { ParamName = "@Types", ParamType = "VARCHAR" },
                    new() { ParamName = "@DriveName", ParamType = "NVARCHAR" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@FilegroupLevel",
                    Name = "Level",
                    DefaultValue = true,
                    DataType = typeof(bool),
                    PickerItems = new Dictionary<object, string>
                    {
                        { true, "Filegroup" },
                        { false, "File" },
                    },
                    MenuBar = true
                },
                new()
                {
                    ParameterName = "@Types",
                    Name = "Type",
                    MultiSelect = true,
                    PickerItems = new Dictionary<object, string>
                    {
                        { "0", "ROWS" },
                        { "1", "LOG" },
                        { "2", "FILESTREAM" },
                        { "4", "FULLTEXT" },
                    },
                    MenuBar = true
                }
            }
        };

        #endregion Report definition
    }
}
