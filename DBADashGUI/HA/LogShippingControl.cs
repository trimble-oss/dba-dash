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

namespace DBADashGUI.LogShipping
{
    public partial class LogShippingControl : UserControl
    {

        private List<int> instanceIDs;
        private List<int> cachedInstanceIDs;

        public List<int> InstanceIDs {
            get {
                return instanceIDs;
            }
            set {
                cachedInstanceIDs = value;
                instanceIDs = value;
            }
        }
    

        public bool IncludeCritical
        {
            get
            {
                return tsCritical.Checked;
            }
            set
            {
                tsCritical.Checked = value;
            }
        }

        public bool IncludeWarning
        {
            get
            {
                return tsWarning.Checked;
            }
            set
            {
                tsWarning.Checked = value;
            }
        }
        public bool IncludeNA
        {
            get
            {
                return tsNA.Checked;
            }
            set
            {
                tsNA.Checked = value;
            }
        }
        public bool IncludeOK
        {
            get
            {
                return tsOK.Checked;
            }
            set
            {
                tsOK.Checked = value;
            }
        }

        private void refreshSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.LogShippingSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs",InstanceIDs.AsDataTable());
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvSummary.AutoGenerateColumns = false;
                if (dgvSummary.Columns.Count == 0)
                {
                    dgvSummary.Columns.Add(new DataGridViewLinkColumn() { HeaderText = "Instance", DataPropertyName = "Instance", SortMode= DataGridViewColumnSortMode.Automatic });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn {Name="Status", HeaderText = "Status", DataPropertyName = "StatusDescription" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Log Shipped DBs", DataPropertyName = "LogshippedDBCount" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Warning", DataPropertyName = "WarningCount" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Critical", DataPropertyName = "CriticalCount" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Max Total Time Behind", DataPropertyName = "MaxTotalTimeBehind" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Max Latency of Last", DataPropertyName = "MaxLatencyOfLast" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Max Time Since Last", DataPropertyName = "TimeSinceLast" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { Name="SnapshotAge",HeaderText = "Snapshot Age", DataPropertyName = "SnapshotAge" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Backup Date of Oldest File", DataPropertyName = "MinDateOfLastBackupRestored" });
                    dgvSummary.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Restore Date of Oldest File", DataPropertyName = "MinLastRestoreCompleted" });
                    dgvSummary.Columns.Add(new DataGridViewLinkColumn() {Name="Configure", HeaderText = "Configure",  Text="Configure", UseColumnTextForLinkValue=true});
                }
                dgvSummary.DataSource = new DataView(dt);
                dgvSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        public void RefreshData()
        {
            tsBack.Enabled = (cachedInstanceIDs.Count > 1 && instanceIDs.Count == 1);
            refreshSummary();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.LogShipping_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
				Common.ConvertUTCToLocal(ref dt);
                dt.Columns["restore_date_utc"].ColumnName = "restore_date";
                dt.Columns["backup_start_date_utc"].ColumnName = "backup_start_date";
                dgvLogShipping.AutoGenerateColumns = false;
                dgvLogShipping.DataSource = new DataView(dt);
                dgvLogShipping.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
        }


        public LogShippingControl()
        {
            InitializeComponent();
        }

        private void tsFilter_Click(object sender, EventArgs e)
        {
            RefreshData();
        }


        private void dgvLogShipping_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgvLogShipping.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    var row = (DataRowView)dgvLogShipping.Rows[e.RowIndex].DataBoundItem;
                    ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"]);
                }
            }
        }

        public void ConfigureThresholds(Int32 InstanceID,Int32 DatabaseID)
        {
            var frm = new LogShippingThresholdsConfig
            {
                InstanceID = InstanceID,
                DatabaseID = DatabaseID
            };
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void dgvLogShipping_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvLogShipping.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotAgeStatus"];
                dgvLogShipping.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);
                dgvLogShipping.Rows[idx].Cells["Status"].Style.BackColor = DBADashStatus.GetStatusColour(Status);
                if ((string)row["ThresholdConfiguredLevel"] == "Database")
                {
                    dgvLogShipping.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvLogShipping.Font, FontStyle.Bold);
                }
                else
                {
                    dgvLogShipping.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvLogShipping.Font, FontStyle.Regular);
                }
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            dgvSummary.Columns["Configure"].Visible = false;
            Common.CopyDataGridViewToClipboard(dgvSummary);
            dgvSummary.Columns["Configure"].Visible = true;
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            dgvSummary.Columns["Configure"].Visible = false;
            Common.PromptSaveDataGridView(ref dgvSummary);
            dgvSummary.Columns["Configure"].Visible = true;             
        }

        private void dgvSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0)
            {
                var r = (DataRowView)dgvSummary.Rows[e.RowIndex].DataBoundItem;
                if (e.ColumnIndex == 0)
                {                   
                    instanceIDs = new List<int> { (int)r["InstanceID"] };
                    IncludeCritical = true;
                    IncludeNA = true;
                    IncludeOK = true;
                    IncludeWarning = true;
                    RefreshData();
                }
                else if (dgvSummary.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    ConfigureThresholds((Int32)r["InstanceID"], -1);
                }
            }
         
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            instanceIDs = cachedInstanceIDs;
            IncludeCritical = true;
            IncludeNA = false;
            IncludeOK =false;
            IncludeWarning = true;
            RefreshData();
        }

        private void dgvSummary_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvSummary.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];
                var snapshotStatus = (DBADashStatus.DBADashStatusEnum)row["SnapshotAgeStatus"];
                dgvSummary.Rows[idx].Cells["SnapshotAge"].Style.BackColor = DBADashStatus.GetStatusColour(snapshotStatus);
                dgvSummary.Rows[idx].Cells["Status"].Style.BackColor = DBADashStatus.GetStatusColour(Status);
                if ((bool)row["InstanceLevelThreshold"])
                {
                    dgvSummary.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvSummary.Font, FontStyle.Bold);
                }
                else
                {
                    dgvSummary.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvSummary.Font, FontStyle.Regular);
                }
                    
            }
        }

        private void tsCopyDetail_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvLogShipping);
            Configure.Visible = true;
        }

        private void tsExportExcelDetail_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.PromptSaveDataGridView(ref dgvLogShipping);
            Configure.Visible = true;
        }
    }
}
