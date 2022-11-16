using DBADashGUI.Drives;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.Properties
{
    public partial class DrivesControl : UserControl, ISetContext
    {
        public DrivesControl()
        {
            InitializeComponent();
        }

        public Int32 DrivesViewMaxRows = 30;
        private DBADashContext context;

        public bool IncludeCritical
        {
            get
            {
                return criticalToolStripMenuItem.Checked;
            }
            set
            {
                criticalToolStripMenuItem.Checked = value;
            }
        }

        public bool IncludeWarning
        {
            get
            {
                return warningToolStripMenuItem.Checked;
            }
            set
            {
                warningToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeNA
        {
            get
            {
                return undefinedToolStripMenuItem.Checked;
            }
            set
            {
                undefinedToolStripMenuItem.Checked = value;
            }
        }
        public bool IncludeOK
        {
            get
            {
                return OKToolStripMenuItem.Checked;
            }
            set
            {
                OKToolStripMenuItem.Checked = value;
            }
        }

        public bool gridview = false;



        DataView dvDrives;

        private DataTable GetDrives()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Drives_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", context.RegularInstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                cmd.Parameters.AddWithValue("IncludeMetrics", includeAllMetricsToolStripMenuItem.Checked);

                DataTable dt = new();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        public void SetContext(DBADashContext context)
        {
            this.context = context;
            IncludeNA = context.RegularInstanceIDs.Count == 1;
            IncludeOK = context.RegularInstanceIDs.Count == 1;
            IncludeWarning = true;
            IncludeCritical = true;
            RefreshData();
        }

        public void RefreshData()
        {
            pnlDrives.Controls.Clear();
            DataTable dt = GetDrives();
            dvDrives = new DataView(dt);

            if (dt.Rows.Count > DrivesViewMaxRows || gridview)
            {
                ShowGridView();
            }
            else
            {
                ShowDrivesView();
            }

            configureInstanceThresholdsToolStripMenuItem.Enabled = context.RegularInstanceIDs.Count == 1;
        }

        private DataGridView dgv;
        public void ShowGridView()
        {
            gridview = true;
            pnlDrives.Controls.Clear();
            pnlSpacing.Visible = false;
            dgv = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AllowUserToResizeColumns = true,
                ReadOnly = true,
                AutoGenerateColumns = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                DataSource = dvDrives,
                Dock = DockStyle.Fill,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", DataPropertyName = "InstanceDisplayName", HeaderText = "Instance" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Name", DataPropertyName = "Name", HeaderText = "Name" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Label", DataPropertyName = "Label", HeaderText = "Label" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "TotalGB", DataPropertyName = "TotalGB", HeaderText = "Total GB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "0.0" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "FreeGB", DataPropertyName = "FreeGB", HeaderText = "Free GB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "0.0" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "PctFreeSpace", DataPropertyName = "PctFreeSpace", HeaderText = "Free %", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DriveCheckConfiguredLevel", DataPropertyName = "DriveCheckConfiguredLevel", HeaderText = "Configured Level" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Warning", DataPropertyName = "DriveWarningThreshold", HeaderText = "Warning" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Critical", DataPropertyName = "DriveCriticalThreshold", HeaderText = "Critical" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "SnapshotDate", DataPropertyName = "SnapshotDate", HeaderText = "Snapshot Date", DefaultCellStyle = new DataGridViewCellStyle() { Format = "yyyy-MM-dd HH:mm" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "SnapshotAge", DataPropertyName = "SnapshotAgeMins", HeaderText = "Snapshot Age (Mins)", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
            if (includeAllMetricsToolStripMenuItem.Checked)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedUsedGB24Hrs", DataPropertyName = "ChangeUsedGB24Hrs", HeaderText = "Used 24Hrs Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedUsedGB7Days", DataPropertyName = "ChangeUsedGB7Days", HeaderText = "Used 7 Days Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedUsedGB30Days", DataPropertyName = "ChangeUsedGB30Days", HeaderText = "Used 30 Days Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedUsedGB90Days", DataPropertyName = "ChangeUsedGB90Days", HeaderText = "Used 90 Days Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });

                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedDriveSize24Hrs", DataPropertyName = "ChangeDriveSize24Hrs", HeaderText = "Drive Size 24Hrs Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedDriveSize7Days", DataPropertyName = "ChangeDriveSize7Days", HeaderText = "Drive Size 7 Days Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedDriveSize30Days", DataPropertyName = "ChangeDriveSize30Days", HeaderText = "Drive Size 30 Days Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ChangedDriveSize90Days", DataPropertyName = "ChangeDriveSize90Days", HeaderText = "Drive Size 90 Days Change (GB+-)", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle() { Format = "+#,##0.0GB;-#,##0.0GB;-" } });
            }
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "History", HeaderText = "History", UseColumnTextForLinkValue = true, Text = "History", LinkColor = DashColors.LinkColor });
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "Configure", HeaderText = "Configure", UseColumnTextForLinkValue = true, Text = "Configure", LinkColor = DashColors.LinkColor });
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            pnlDrives.Controls.Add(dgv);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            tsGridView.Enabled = false;
            tsDrivesView.Enabled = dvDrives.Table.Rows.Count <= DrivesViewMaxRows;
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];
                var SnapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotStatus"];
                dgv.Rows[idx].Cells["SnapshotAge"].SetStatusColor(SnapshotStatus);
                if (row["DriveCheckType"] != DBNull.Value && (string)row["DriveCheckType"] == "G")
                {
                    dgv.Rows[idx].Cells["FreeGB"].SetStatusColor(Status);
                    dgv.Rows[idx].Cells["PctFreeSpace"].SetStatusColor(Color.White);
                    dgv.Rows[idx].Cells["Warning"].Style.Format = "0.0 GB";
                    dgv.Rows[idx].Cells["Critical"].Style.Format = "0.0 GB";
                }
                else
                {
                    dgv.Rows[idx].Cells["PctFreeSpace"].SetStatusColor(Status);
                    dgv.Rows[idx].Cells["FreeGB"].SetStatusColor(Color.White);
                    dgv.Rows[idx].Cells["Warning"].Style.Format = "P2";
                    dgv.Rows[idx].Cells["Critical"].Style.Format = "P2";
                }


                if ((bool)row["IsInheritedThreshold"])
                {
                    dgv.Rows[idx].Cells["Configure"].Style.Font = new Font(dgv.Font, FontStyle.Regular);
                }
                else
                {
                    dgv.Rows[idx].Cells["Configure"].Style.Font = new Font(dgv.Font, FontStyle.Bold);
                }
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgv.Columns[e.ColumnIndex].Name == "Configure")
                {
                    DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    Configure((Int32)row["InstanceID"], (Int32)row["DriveID"]);
                }
                else if (dgv.Columns[e.ColumnIndex].Name == "History")
                {
                    DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    var frm = new DriveHistoryView
                    {
                        DriveID = (Int32)row["DriveID"],
                        Text = row["Instance"] + " | " + (string)row["Name"] + " " + (row["Label"] == DBNull.Value ? "" : (string)row["Label"])
                    };
                    frm.Show();
                }
            }
        }

        public void ShowDrivesView()
        {
            gridview = false;
            var driveControls = new List<DriveControl>();
            pnlDrives.Controls.Clear();
            pnlSpacing.Visible = true;
            foreach (DataRow r in dvDrives.Table.Rows)
            {
                var drv = new DriveControl();
                drv.Drive.WarningThreshold = r["DriveWarningThreshold"] == DBNull.Value ? 0 : (decimal)r["DriveWarningThreshold"];
                drv.Drive.CriticalThreshold = r["DriveCriticalThreshold"] == DBNull.Value ? 0 : (decimal)r["DriveCriticalThreshold"];

                drv.Drive.DriveLabel = r["Label"] == DBNull.Value ? "" : (string)r["Label"];
                drv.Drive.InstanceName = (string)r["InstanceDisplayName"];
                drv.Drive.DriveLetter = (string)r["Name"];
                drv.Drive.DriveCapacityGB = (decimal)r["TotalGB"];
                drv.Drive.FreeSpaceGB = (decimal)r["FreeGB"];
                drv.Drive.DriveStatus = (DBADashStatusEnum)r["Status"];
                drv.Drive.Inherited = (bool)r["IsInheritedThreshold"];
                drv.Drive.InstanceID = (Int32)r["InstanceID"];
                drv.Drive.DriveID = (Int32)r["DriveID"];
                drv.Drive.DriveCheckTypeChar = char.Parse((string)r["DriveCheckType"]);
                drv.Drive.SnapshotDate = (DateTime)r["SnapshotDate"];
                drv.Drive.SnapshotStatus = (DBADashStatusEnum)r["SnapshotStatus"];
                drv.DisplayInstanceName = context.RegularInstanceIDs.Count > 1;
                drv.Dock = DockStyle.Top;
                driveControls.Add(drv);
            }

            if (driveControls.Count == 0)
            {
                pnlDrives.Controls.Add(new Label() { Text = "No drives to display matching the selected filters", Dock = DockStyle.Fill });
            }
            else
            {
                pnlDrives.Controls.AddRange(driveControls.ToArray());
            }
            tsDrivesView.Enabled = false;
            tsGridView.Enabled = true;
        }

        public void Configure(Int32 InstanceID, Int32 DriveID)
        {
            var drv = DriveThreshold.GetDriveThreshold(InstanceID, DriveID);
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



        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (context.RegularInstanceIDs.Count == 1)
            {
                Configure(context.RegularInstanceIDs.First(), -1);
            }
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configure(-1, -1);
        }

        private void CriticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void WarningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void UndefinedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void OKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }


        private void TsDrivesView_Click(object sender, EventArgs e)
        {
            ShowDrivesView();
        }

        private void TsGridView_Click(object sender, EventArgs e)
        {
            ShowGridView();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            if (!gridview)
            {
                ShowGridView();
            }
            dgv.Columns["Configure"].Visible = false;
            dgv.Columns["History"].Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            dgv.Columns["Configure"].Visible = true;
            dgv.Columns["History"].Visible = true;

        }

        private void IncludeAllMetricsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gridview = true;
            RefreshData();

        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            if (!gridview)
            {
                ShowGridView();
            }
            dgv.Columns["Configure"].Visible = false;
            dgv.Columns["History"].Visible = false;
            Common.PromptSaveDataGridView(ref dgv);
            dgv.Columns["Configure"].Visible = true;
            dgv.Columns["History"].Visible = true;
        }

    }
}
