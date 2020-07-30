using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace DBAChecksGUI.Properties
{
    public partial class DrivesControl : UserControl
    {
        public DrivesControl()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;
        public string ConnectionString;
        public Int32 DrivesViewMaxRows = 30;

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


        private void setLinkText(DriveThreshold drive,Label lbl)
        {
            if (drive == null)
            {
                lbl.Text = "{Not Configured}";
                return;
            }
            var pctGB = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? "%" : "GB";
            var warning = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? drive.WarningThreshold * 100 : drive.WarningThreshold;
            var critical = drive.DriveCheckType == Drive.DriveCheckTypeEnum.Percent ? drive.CriticalThreshold * 100 : drive.CriticalThreshold;
            if (drive.Inherited)
            {
                lbl.Text = "Inherited";
            }
            else if(drive.DriveCheckType == DriveThreshold.DriveCheckTypeEnum.None)
            {
                lbl.Text = "Disabled";
            }
            else
            {
                lbl.Text = string.Format("Warning: {0:0.0}{1}, Critical:{2:0.0}{1}", warning, pctGB, critical);
            }
        }

        DataView dvDrives;

        public void RefreshData()
        {
            pnlDrives.Controls.Clear();

            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.Drives_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",",InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dvDrives = new DataView(dt);

                if (dt.Rows.Count > DrivesViewMaxRows || gridview)
                {
                    ShowGridView();
                }
                else
                {
                    ShowDrivesView();
                }
          
            }
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
        }

        private DataGridView dgv;
        public void ShowGridView()
        {
            gridview = true;
            pnlDrives.Controls.Clear();
            pnlSpacing.Visible = false;
            dgv = new DataGridView();
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.RowHeadersVisible = false;
            dgv.ReadOnly = true;
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", DataPropertyName = "Instance", HeaderText = "Instance" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Name", DataPropertyName = "Name", HeaderText = "Name" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Label", DataPropertyName = "Label", HeaderText = "Label" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "TotalGB", DataPropertyName = "TotalGB", HeaderText = "Total GB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "0.0" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "FreeGB", DataPropertyName = "FreeGB", HeaderText = "Free GB", DefaultCellStyle = new DataGridViewCellStyle() { Format = "0.0" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "PctFreeSpace", DataPropertyName = "PctFreeSpace", HeaderText = "Free %", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DriveCheckConfiguredLevel", DataPropertyName = "DriveCheckConfiguredLevel", HeaderText = "Configured Level" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Warning",DataPropertyName="DriveWarningThreshold", HeaderText = "Warning" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Critical", DataPropertyName = "DriveCriticalThreshold", HeaderText = "Critical" });
            dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "Configure", HeaderText = "Configure", UseColumnTextForLinkValue = true, Text = "Configure" });
            dgv.AutoGenerateColumns = false;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.RowsAdded += Dgv_RowsAdded;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgv.DataSource = dvDrives;
            dgv.Dock =  DockStyle.Fill;
            pnlDrives.Controls.Add(dgv);
            tsGridView.Enabled = false;
            tsDrivesView.Enabled = dvDrives.Table.Rows.Count<= DrivesViewMaxRows;
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
                var Status = (DBAChecksStatus.DBAChecksStatusEnum)row["Status"];
                if(row["DriveCheckType"]!=DBNull.Value && (string)row["DriveCheckType"]== "G")
                {
                    dgv.Rows[idx].Cells["FreeGB"].Style.BackColor = DBAChecksStatus.GetStatusColour(Status);
                    dgv.Rows[idx].Cells["PctFreeSpace"].Style.BackColor = Color.White;
                    dgv.Rows[idx].Cells["Warning"].Style.Format = "0.0 GB";
                    dgv.Rows[idx].Cells["Critical"].Style.Format = "0.0 GB";
                }
                else
                {
                    dgv.Rows[idx].Cells["PctFreeSpace"].Style.BackColor = DBAChecksStatus.GetStatusColour(Status);
                    dgv.Rows[idx].Cells["FreeGB"].Style.BackColor = Color.White;
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
            if (dgv.Columns[e.ColumnIndex].Name == "Configure")
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                Configure((Int32)row["InstanceID"], (Int32)row["DriveID"]);
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
                drv.Drive.InstanceName = (string)r["Instance"];
                drv.Drive.DriveLetter = (string)r["Name"];
                drv.Drive.DriveCapacityGB = (decimal)r["TotalGB"];
                drv.Drive.FreeSpaceGB = (decimal)r["FreeGB"];
                drv.Drive.DriveStatus = (Drive.DriveStatusEnum)r["Status"];
                drv.Drive.Inherited = (bool)r["IsInheritedThreshold"];
                drv.Drive.InstanceID = (Int32)r["InstanceID"];
                drv.Drive.DriveID = (Int32)r["DriveID"];
                drv.Drive.DriveCheckTypeChar = char.Parse((string)r["DriveCheckType"]);
                drv.DisplayInstanceName = InstanceIDs.Count > 1;
                drv.Drive.ConnectionString = ConnectionString;
                drv.Dock = DockStyle.Top;
                driveControls.Add(drv);
            }
            pnlDrives.Controls.AddRange(driveControls.ToArray());
            tsDrivesView.Enabled = false;
            tsGridView.Enabled = true;
        }

        public void Configure(Int32 InstanceID,Int32 DriveID)
        {
            var drv = DriveThreshold.GetDriveThreshold(InstanceID, DriveID,ConnectionString);
            var frm = new DriveThresholdConfig();
            frm.DriveThreshold = drv;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }


        
        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                Configure(InstanceIDs[0], -1);
            }
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configure(-1, -1);
        }

        private void criticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void warningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void undefinedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void OKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshData();
        }


        private void tsDrivesView_Click(object sender, EventArgs e)
        {
            ShowDrivesView();
        }

        private void tsGridView_Click(object sender, EventArgs e)
        {
            ShowGridView();
        }
    }
}
