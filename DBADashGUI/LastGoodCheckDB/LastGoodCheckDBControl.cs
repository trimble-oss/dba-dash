using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.LastGoodCheckDB
{
    public partial class LastGoodCheckDBControl : UserControl, ISetContext
    {
        private List<Int32> InstanceIDs;

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

        public LastGoodCheckDBControl()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.RegularInstanceIDs.ToList();
            IncludeCritical = true;
            IncludeWarning = true;
            IncludeNA = context.InstanceID > 0;
            IncludeOK = context.InstanceID > 0;
            RefreshData();
        }

        public void RefreshData()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.LastGoodCheckDB_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);

                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                dgvLastGoodCheckDB.AutoGenerateColumns = false;
                dgvLastGoodCheckDB.DataSource = new DataView(dt);
                dgvLastGoodCheckDB.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
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

        private void DgvLastGoodCheckDB_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvLastGoodCheckDB.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];
                var statusC = Status.GetColor();
                var r = dgvLastGoodCheckDB.Rows[idx];
                r.Cells["LastGoodCheckDBTime"].SetStatusColor(statusC);
                r.Cells["DaysSinceLastGoodCheckDB"].SetStatusColor(statusC);
                if ((string)row["ConfiguredLevel"] == "Database")
                {
                    r.Cells["Configure"].Style.Font = new Font(dgvLastGoodCheckDB.Font, FontStyle.Bold);
                }
                else
                {
                    r.Cells["Configure"].Style.Font = new Font(dgvLastGoodCheckDB.Font, FontStyle.Regular);
                }
            }
        }

        private void DgvLastGoodCheckDB_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (dgvLastGoodCheckDB.Columns[e.ColumnIndex].HeaderText == "Configure")
                {
                    var row = (DataRowView)dgvLastGoodCheckDB.Rows[e.RowIndex].DataBoundItem;
                    ConfigureThresholds((Int32)row["InstanceID"], (Int32)row["DatabaseID"]);
                }
            }
        }

        public void ConfigureThresholds(Int32 InstanceID, Int32 DatabaseID)
        {
            var frm = new LastGoodCheckDBConfig();
            var threshold = LastGoodCheckDBThreshold.GetLastGoodCheckDBThreshold(InstanceID, DatabaseID);
            frm.Threshold = threshold;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void ConfigureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void ConfigureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1, -1);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvLastGoodCheckDB);
            Configure.Visible = true;
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.PromptSaveDataGridView(ref dgvLastGoodCheckDB);
            Configure.Visible = true;
        }

        private void LastGoodCheckDB_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgvLastGoodCheckDB);
        }
    }
}