using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using Humanizer;
namespace DBADashGUI.Changes
{
    public partial class DBOptions : UserControl
    {

        public List<Int32> InstanceIDs;
        public Int32 DatabaseID;

        public DBOptions()
        {
            InitializeComponent();
        }

        public bool SummaryMode
        {
            get
            {
                if (DatabaseID > 0)
                {
                    return false;
                }
                else
                {
                    return tsDetail.Visible;
                }
            }
            set
            {
                tsDetail.Visible = value;
                tsSummary.Visible = !value;
            }
        }

        public void RefreshData()
        {
            if (InstanceIDs != null)
            {
                refreshHistory();
                if (SummaryMode)
                {
                    refreshDBSummary();
                }
                else
                {
                    refreshDBInfo();
                }
            }
        }

        private void pivot(ref DataTable dt)
        {
            var pivotDT = new DataTable();
            pivotDT.Columns.Add("Setting");
            pivotDT.Columns.Add("Value");
            foreach(DataColumn col in dt.Columns)
            {
                if (col.ColumnName != "InstanceID" && col.ColumnName != "DatabaseID")
                {
                    var r = pivotDT.NewRow();
                    r[0] = col.ColumnName;
                    r[1] = dt.Rows[0][col];
                    pivotDT.Rows.Add(r);
                }
            }
            dgv.DataSource = pivotDT;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void refreshDBSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));               
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            
        }

        private void refreshDBInfo()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DatabasesAllInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                ;
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 1)
                {
                    pivot(ref dt);
                }
                else
                {
                    dgv.DataSource = dt;
                    dgv.Columns["InstanceID"].Visible = false;
                    dgv.Columns["DatabaseID"].Visible = false;
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        col.HeaderText = col.HeaderText.Titleize();
                    }
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
            }
            
        }

        private void refreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBOptionsHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                cmd.Parameters.AddWithValue("ExcludeStateChanges", excludeStateChangesToolStripMenuItem.Checked);
                
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                foreach (DataRow r in dt.Rows)
                {
                    if (r["OldValue"].GetType() == typeof(byte[]))
                    {
                        r["OldValue"] = Common.ByteArrayToString((byte[])r["OldValue"]);
                    }
                    if (r["NewValue"].GetType() == typeof(byte[]))
                    {
                        r["NewValue"] = Common.ByteArrayToString((byte[])r["NewValue"]);
                    }
                }
                dgvHistory.AutoGenerateColumns = false;
                dgvHistory.DataSource = dt;
                dgvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }          
        }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHistory);
        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }

        private void tsRefreshInfo_Click(object sender, EventArgs e)
        {
            if (SummaryMode)
            {
                refreshDBSummary();
            }
            else
            {
                refreshDBInfo();
            }
        }

        private void tsCopyInfo_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void excludeStateChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }

        private void tsSummary_Click(object sender, EventArgs e)
        {
            SummaryMode = true;
            refreshDBSummary();
        }

        private void tsDetail_Click(object sender, EventArgs e)
        {
            SummaryMode = false;
            refreshDBInfo();
        }

        private void DBOptions_Load(object sender, EventArgs e)
        {
            if (DatabaseID > 0)
            {
                SummaryMode = false;
            }
           
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (SummaryMode)
            {
                var warningCols = new string[] { "Auto Create Stats Disabled", "Auto Update Stats Disabled", "Old Compat Level", "In Recovery", "Offline","Trustworthy" };
                var criticalCols = new string[] { "Page Verify Not Optimal", "Auto Close", "Auto Shrink", "Suspect", "Emergency" };
                for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
                {
                    var r = dgv.Rows[idx];
                    foreach (var col in warningCols)
                    {
                        r.Cells[col].SetStatusColor((Int32)r.Cells[col].Value > 0 ? DashColors.Warning : Color.White);
                    }
                    foreach (var col in criticalCols)
                    {
                        r.Cells[col].SetStatusColor((Int32)r.Cells[col].Value > 0 ? DashColors.Fail  : Color.White);
                    }
                    Color vlfStatusColor = DashColors.NotApplicable;
                    if (r.Cells["Max VLF Count"].Value != DBNull.Value)
                    {
                        vlfStatusColor = (Int32)r.Cells["Max VLF Count"].Value > 10000 ? DashColors.Fail : ((Int32)r.Cells["Max VLF Count"].Value > 1000 ? DashColors.Warning : Color.White);
                    }

                    r.Cells["Max VLF Count"].SetStatusColor(vlfStatusColor);
                    
                }
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void tsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHistory);
        }

        private void tsCols_Click(object sender, EventArgs e)
        {
            using(var frm = new SelectColumns() { Columns = dgv.Columns } )
            {
                frm.ShowDialog(this);
            }
        }
    }
}
