﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
namespace DBADashGUI.LastGoodCheckDB
{
    public partial class LastGoodCheckDBControl : UserControl
    {
        public List<Int32> InstanceIDs;
        public string connectionString;

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

        public void RefreshData()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.LastGoodCheckDB_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    cmd.Parameters.AddWithValue("IncludeCritical", IncludeCritical);
                    cmd.Parameters.AddWithValue("IncludeWarning", IncludeWarning);
                    cmd.Parameters.AddWithValue("IncludeOK", IncludeOK);
                    cmd.Parameters.AddWithValue("IncludeNA", IncludeNA);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgvLastGoodCheckDB.AutoGenerateColumns = false;
                    dgvLastGoodCheckDB.DataSource = new DataView(dt);
                    dgvLastGoodCheckDB.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
            configureInstanceThresholdsToolStripMenuItem.Enabled = InstanceIDs.Count == 1;
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

        private void dgvLastGoodCheckDB_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvLastGoodCheckDB.Rows[idx].DataBoundItem;
                var Status = (DBADashStatus.DBADashStatusEnum)row["Status"];
                var statusC = DBADashStatus.GetStatusColour(Status);
                dgvLastGoodCheckDB.Rows[idx].Cells["LastGoodCheckDBTime"].Style.BackColor = statusC;
                dgvLastGoodCheckDB.Rows[idx].Cells["DaysSinceLastGoodCheckDB"].Style.BackColor = statusC;
                if ((string)row["ConfiguredLevel"]== "Database")
                {
                    dgvLastGoodCheckDB.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvLastGoodCheckDB.Font, FontStyle.Bold);
                }
                else
                {
                    dgvLastGoodCheckDB.Rows[idx].Cells["Configure"].Style.Font = new Font(dgvLastGoodCheckDB.Font, FontStyle.Regular);
                }
            }
         }

        private void dgvLastGoodCheckDB_CellContentClick(object sender, DataGridViewCellEventArgs e)
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

        public void ConfigureThresholds(Int32 InstanceID,Int32 DatabaseID)
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


        private void configureInstanceThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (InstanceIDs.Count == 1)
            {
                ConfigureThresholds(InstanceIDs[0], -1);
            }
        }

        private void configureRootThresholdsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigureThresholds(-1,-1);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.CopyDataGridViewToClipboard(dgvLastGoodCheckDB);
            Configure.Visible = true;
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Configure.Visible = false;
            Common.PromptSaveDataGridView(ref dgvLastGoodCheckDB);
            Configure.Visible = true;
        }
    }



}
