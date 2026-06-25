using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.Drives
{
    internal class DrivesView : CustomReportView
    {
        private const int DrivesViewMaxRows = 30;

        private ToolStripDropDownButton tsConfigureDrives;
        private ToolStripMenuItem mnuThresholdInstance;
        private ToolStripButton tsGridView;
        private ToolStripButton tsDrivesView;
        private ToolStripMenuItem tsIncludeAllMetrics;
        private bool cardView;
        private bool userSelectedGridView;
        private Panel pnlDrives;
        private readonly List<DriveControl> driveControlPool = new();

        public DrivesView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigureDrives = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigureDrives",
                ToolTipText = "Configure drive thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            var mnuThresholdRoot = new ToolStripMenuItem("Configure Root Thresholds");
            mnuThresholdRoot.Click += (_, _) => Configure(-1, -1);
            mnuThresholdInstance = new ToolStripMenuItem("Configure Instance Thresholds");
            mnuThresholdInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) Configure(id, -1);
            };
            tsConfigureDrives.DropDownItems.AddRange(new ToolStripItem[] { mnuThresholdRoot, mnuThresholdInstance });

            tsDrivesView = new ToolStripButton
            {
                Name = "tsDrivesView",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Properties.Resources.Hard_Drive,
                ImageTransparentColor = Color.Magenta,
                Text = "Drives View",
            };
            tsDrivesView.Click += (_, _) => SwitchToCardView();

            tsGridView = new ToolStripButton
            {
                Name = "tsGridView",
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Properties.Resources.Table_16x,
                ImageTransparentColor = Color.Magenta,
                Text = "Table View",
            };
            tsGridView.Click += (_, _) => SwitchToGridView();
            tsIncludeAllMetrics = new ToolStripMenuItem("Toggle Metrics") { CheckOnClick = true, Image = Properties.Resources.AddComputedField_16x, ToolTipText = "Toggle drive growth metrics" };
            tsIncludeAllMetrics.Click += (_, _) =>
            {
                SetIncludeMetricsParam(tsIncludeAllMetrics.Checked);
                SwitchToGridView();
                RefreshData();
            };

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigureDrives);
            ToolStrip.Items.Insert(insertAt + 1, tsDrivesView);
            ToolStrip.Items.Insert(insertAt + 2, tsGridView);
            ToolStrip.Items.Insert(insertAt + 3, tsIncludeAllMetrics);
        }

        private void UpdateToolbarState()
        {
            mnuThresholdInstance.Enabled = TryGetSingleInstanceId(out _);
            var rowCount = Grids.FirstOrDefault()?.RowCount ?? 0;
            tsDrivesView.Enabled = !cardView && rowCount <= DrivesViewMaxRows;
            tsGridView.Enabled = cardView;
        }

        #endregion Toolbar

        #region Parameters

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

        private void SetIncludeMetricsParam(bool includeMetrics)
        {
            var p = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals("@IncludeMetrics", StringComparison.OrdinalIgnoreCase));
            if (p == null) return;
            p.Param.Value = includeMetrics;
            p.UseDefaultValue = false;
        }

        protected override void OnBeforeRefresh()
        {
            base.OnBeforeRefresh();
            var pDriveName = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals("@DriveName", StringComparison.OrdinalIgnoreCase));
            if (pDriveName != null)
            {
                pDriveName.Param.Value = context?.DriveName != null ? context.DriveName : DBNull.Value;
                pDriveName.UseDefaultValue = false;
            }
            var pIncludeMetrics = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals("@IncludeMetrics", StringComparison.OrdinalIgnoreCase));
            if (pIncludeMetrics != null && pIncludeMetrics.UseDefaultValue)
            {
                pIncludeMetrics.Param.Value = tsIncludeAllMetrics.Checked;
                pIncludeMetrics.UseDefaultValue = false;
            }
        }

        #endregion Parameters

        #region Configuration dialogs

        private void Configure(int instanceId, int driveId)
        {
            var drv = DriveThreshold.GetDriveThreshold(instanceId, driveId);
            var frm = new DriveThresholdConfig
            {
                DriveThreshold = drv
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        #endregion Configuration dialogs

        #region View switching (card vs grid)

        private void SwitchToCardView()
        {
            cardView = true;
            userSelectedGridView = false;
            ShowCardView();
            UpdateToolbarState();
        }

        private void SwitchToGridView()
        {
            cardView = false;
            userSelectedGridView = true;
            HideCardView();
            UpdateToolbarState();
        }

        private static void PopulateDrive(DriveControl ctrl, DataRowView drv, bool displayInstanceName)
        {
            var drive = ctrl.Drive;
            drive.WarningThreshold = drv["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)drv["DriveWarningThreshold"];
            drive.CriticalThreshold = drv["DriveCriticalThreshold"] == DBNull.Value ? 0 : (decimal)drv["DriveCriticalThreshold"];
            drive.DriveLabel = drv["Label"] == DBNull.Value ? "" : (string)drv["Label"];
            drive.InstanceName = (string)drv["InstanceDisplayName"];
            drive.DriveLetter = (string)drv["Name"];
            drive.DriveCapacityGB = (decimal)drv["TotalGB"];
            drive.FreeSpaceGB = (decimal)drv["FreeGB"];
            drive.DriveStatus = (DBADashStatusEnum)drv["Status"];
            drive.Inherited = (bool)drv["IsInheritedThreshold"];
            drive.InstanceID = (int)drv["InstanceID"];
            drive.DriveID = (int)drv["DriveID"];
            drive.DriveCheckTypeChar = char.Parse((string)drv["DriveCheckType"]);
            drive.SnapshotDate = (DateTime)drv["SnapshotDate"];
            drive.SnapshotStatus = (DBADashStatusEnum)drv["SnapshotStatus"];
            ctrl.DisplayInstanceName = displayInstanceName;
            ctrl.Drive = drive;
        }

        private DriveControl GetOrCreateDriveControl(int index)
        {
            if (index < driveControlPool.Count) return driveControlPool[index];
            var ctrl = new DriveControl { Dock = DockStyle.Top };
            ctrl.ApplyTheme();
            driveControlPool.Add(ctrl);
            return ctrl;
        }

        private void ShowCardView()
        {
            if (pnlDrives == null)
            {
                pnlDrives = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            }

            pnlDrives.SuspendLayout();
            TablePanel.SuspendLayout();
            try
            {
                var grid = Grids.FirstOrDefault();
                var dataRows = new List<DataRowView>();
                if (grid != null)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.DataBoundItem is DataRowView drv)
                            dataRows.Add(drv);
                    }
                }

                var displayInstanceName = context?.RegularInstanceIDs.Count > 1;

                // Remove non-pooled controls (spacer, labels, history panel) but keep pooled DriveControls
                for (var i = pnlDrives.Controls.Count - 1; i >= 0; i--)
                {
                    if (pnlDrives.Controls[i] is not DriveControl)
                    {
                        var ctrl = pnlDrives.Controls[i];
                        pnlDrives.Controls.RemoveAt(i);
                        ctrl.Dispose();
                    }
                }

                // Update or add pooled controls to match the data
                for (var i = 0; i < dataRows.Count; i++)
                {
                    var ctrl = GetOrCreateDriveControl(i);
                    PopulateDrive(ctrl, dataRows[i], displayInstanceName);
                    ctrl.Visible = true;
                    if (!pnlDrives.Controls.Contains(ctrl))
                        pnlDrives.Controls.Add(ctrl);
                }

                // Hide excess pooled controls
                for (var i = dataRows.Count; i < driveControlPool.Count; i++)
                {
                    driveControlPool[i].Visible = false;
                }

                if (dataRows.Count == 0)
                {
                    pnlDrives.Controls.Add(new Label { Text = "No drives to display matching the selected filters", Dock = DockStyle.Fill });
                }
                else
                {
                    pnlDrives.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 20 });
                }

                AddHistory();

                foreach (var g in Grids) g.Visible = false;
                if (!TablePanel.Controls.Contains(pnlDrives))
                {
                    TablePanel.Controls.Add(pnlDrives);
                }
                pnlDrives.Visible = true;
                pnlDrives.BringToFront();
            }
            finally
            {
                pnlDrives.ResumeLayout(true);
                TablePanel.ResumeLayout(true);
            }
        }

        private void HideCardView()
        {
            TablePanel.SuspendLayout();
            try
            {
                if (pnlDrives != null) pnlDrives.Visible = false;
                foreach (var g in Grids) g.Visible = true;
            }
            finally
            {
                TablePanel.ResumeLayout(true);
            }
        }

        private void AddHistory()
        {
            if (context?.DriveName == null) return;
            var grid = Grids.FirstOrDefault();
            if (grid == null || grid.RowCount != 1) return;
            if (grid.Rows[0].DataBoundItem is not DataRowView drv) return;

            pnlDrives.Controls.Add(new Label
            {
                Text = Convert.ToString(drv["Label"]) + " (" + Convert.ToString(drv["Name"]) + ")",
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = DashColors.TrimbleBlue,
                Font = new Font(Font.FontFamily, 20f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 50
            });

            var pnlHistory = new Panel { Dock = DockStyle.Fill };
            pnlHistory.Controls.Add(new Label
            {
                Text = "History",
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = DashColors.TrimbleBlue,
                Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 50
            });
            var history = new DriveHistory();
            history.ApplyTheme();
            history.Name = context.DriveName;
            history.DriveID = (int)drv["DriveID"];
            history.Dock = DockStyle.Fill;
            pnlHistory.Controls.Add(history);
            pnlDrives.Controls.Add(pnlHistory);
            history.BringToFront();
            pnlHistory.BringToFront();
            history.RefreshData();
        }

        /// <summary>
        /// The panel that contains the grid results in the base class.
        /// </summary>
        private Control TablePanel => splitTablesCharts.Panel2;

        #endregion View switching

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            foreach (var grid in Grids)
            {
                ApplyThresholdStyling(grid);
            }

            var rowCount = Grids.FirstOrDefault()?.RowCount ?? 0;
            if (rowCount > DrivesViewMaxRows || tsIncludeAllMetrics.Checked)
            {
                cardView = false;
            }
            else if (!cardView && !userSelectedGridView)
            {
                cardView = true;
            }

            if (cardView)
            {
                ShowCardView();
            }
            else
            {
                HideCardView();
            }

            UpdateToolbarState();
        }

        private static void ApplyThresholdStyling(DBADashDataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is not DataRowView drv) continue;
                var status = (DBADashStatusEnum)drv["Status"];
                var snapshotStatus = (DBADashStatusEnum)drv["SnapshotStatus"];

                if (grid.Columns["SnapshotAgeMins"] != null)
                    row.Cells["SnapshotAgeMins"].SetStatusColor(snapshotStatus);

                if (drv["DriveCheckType"] != DBNull.Value && (string)drv["DriveCheckType"] == "G")
                {
                    if (grid.Columns["FreeGB"] != null)
                        row.Cells["FreeGB"].SetStatusColor(status);
                    if (grid.Columns["PctFreeSpace"] != null)
                        row.Cells["PctFreeSpace"].SetStatusColor(DBADashStatusEnum.NA);
                    if (grid.Columns["DriveWarningThreshold"] != null)
                        row.Cells["DriveWarningThreshold"].Style.Format = "0.0 GB";
                    if (grid.Columns["DriveCriticalThreshold"] != null)
                        row.Cells["DriveCriticalThreshold"].Style.Format = "0.0 GB";
                }
                else
                {
                    if (grid.Columns["PctFreeSpace"] != null)
                        row.Cells["PctFreeSpace"].SetStatusColor(status);
                    if (grid.Columns["FreeGB"] != null)
                        row.Cells["FreeGB"].SetStatusColor(DBADashStatusEnum.NA);
                    if (grid.Columns["DriveWarningThreshold"] != null)
                        row.Cells["DriveWarningThreshold"].Style.Format = "P2";
                    if (grid.Columns["DriveCriticalThreshold"] != null)
                        row.Cells["DriveCriticalThreshold"].Style.Format = "P2";
                }

                if (grid.Columns["Configure"] != null)
                {
                    var inherited = drv["IsInheritedThreshold"] != DBNull.Value && (bool)drv["IsInheritedThreshold"];
                    row.Cells["Configure"].Style.Font = inherited
                        ? new Font(grid.Font, FontStyle.Regular)
                        : new Font(grid.Font, FontStyle.Bold);
                }
            }

            if (grid.Columns["Configure"] != null)
            {
                grid.Columns["Configure"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        #endregion Grid post-processing

        #region Custom links

        private sealed class DriveConfigureLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not DrivesView view) return;
                var instanceId = row.Cells["InstanceID"].Value as int? ?? -1;
                var driveId = row.Cells["DriveID"].Value as int? ?? -1;
                view.Configure(instanceId, driveId);
            }
        }

        private sealed class DriveHistoryLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                var label = row.Cells["Label"].Value == DBNull.Value ? "" : (string)row.Cells["Label"].Value;
                var instanceName = (string)row.Cells["InstanceDisplayName"].Value;
                var driveLetter = (string)row.Cells["Name"].Value;
                var driveId = (int)row.Cells["DriveID"].Value;
                var frm = new DriveHistoryView
                {
                    DriveID = driveId,
                    Text = @$"{instanceName} | {driveLetter} {label}"
                };
                frm.ShowSingleInstance();
            }
        }

        private sealed class DriveFilesLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                var label = row.Cells["Label"].Value == DBNull.Value ? "" : (string)row.Cells["Label"].Value;
                var instanceId = (int)row.Cells["InstanceID"].Value;
                var instanceName = (string)row.Cells["InstanceDisplayName"].Value;
                var driveLetter = (string)row.Cells["Name"].Value;
                DriveControl.ShowFilesForDrive(driveLetter, label, instanceId, instanceName, (Control)sender);
            }
        }

        #endregion Custom links

        #region Report definition

        private static ColumnMetadata Hidden(int displayIndex) =>
            new() { Visible = false, DisplayIndex = displayIndex };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(DrivesView),
            ReportName = "Drives",
            SchemaName = "dbo",
            ProcedureName = "DrivesReport_Get",
            QualifiedProcedureName = "dbo.DrivesReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.Drives.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Drives",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 0, Alias = "Instance" },
                        ["Name"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Name" },
                        ["Label"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Label" },
                        ["TotalGB"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Total GB", FormatString = "0.0" },
                        ["FreeGB"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Free GB", FormatString = "0.0" },
                        ["PctFreeSpace"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Free %", FormatString = "P1" },
                        ["DriveCheckConfiguredLevel"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Configured Level" },
                        ["DriveWarningThreshold"] = new ColumnMetadata { DisplayIndex = 7, Alias = "Warning" },
                        ["DriveCriticalThreshold"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Critical" },
                        ["SnapshotDate"] = new ColumnMetadata { DisplayIndex = 9, Alias = "Snapshot Date", FormatString = "yyyy-MM-dd HH:mm" },
                        ["SnapshotAgeMins"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Snapshot Age (Mins)", FormatString = "N0" },
                        ["ChangeUsedGB24Hrs"] = new ColumnMetadata { DisplayIndex = 11, Alias = "Used 24Hrs Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeUsedGB7Days"] = new ColumnMetadata { DisplayIndex = 12, Alias = "Used 7 Days Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeUsedGB30Days"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Used 30 Days Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeUsedGB90Days"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Used 90 Days Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeDriveSize24Hrs"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Drive Size 24Hrs Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeDriveSize7Days"] = new ColumnMetadata { DisplayIndex = 16, Alias = "Drive Size 7 Days Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeDriveSize30Days"] = new ColumnMetadata { DisplayIndex = 17, Alias = "Drive Size 30 Days Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        ["ChangeDriveSize90Days"] = new ColumnMetadata { DisplayIndex = 18, Alias = "Drive Size 90 Days Change (GB+-)", FormatString = "+#,##0.0GB;-#,##0.0GB;-" },
                        // Hidden technical columns
                        ["DriveID"] = Hidden(19),
                        ["InstanceID"] = Hidden(20),
                        ["Instance"] = new ColumnMetadata { Visible = false, DisplayIndex = 21, Alias = "Instance (Connection)" },
                        ["Status"] = Hidden(22),
                        ["SnapshotStatus"] = Hidden(23),
                        ["DriveCheckType"] = Hidden(24),
                        ["IsInheritedThreshold"] = Hidden(25),
                        // Unbound link columns
                        ["Files"] = new ColumnMetadata { DisplayIndex = 26, Alias = "Files", Link = new DriveFilesLink(), Description = "View database files on this drive" },
                        ["History"] = new ColumnMetadata { DisplayIndex = 27, Alias = "History", Link = new DriveHistoryLink(), Description = "View drive space history" },
                        ["Configure"] = new ColumnMetadata { DisplayIndex = 28, Alias = "Configure", Link = new DriveConfigureLink(), Description = "Configure drive thresholds" },
                    }
                }
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
                    new() { ParamName = "@DriveName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@IncludeMetrics", ParamType = "BIT" },
                }
            }
        };

        #endregion Report definition
    }
}
