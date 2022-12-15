using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class DBOptions : UserControl, ISetContext
    {
        private List<Int32> InstanceIDs;
        private Int32 DatabaseID;

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

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            DatabaseID = context.DatabaseID;
            RefreshData();
        }

        private void RefreshData()
        {
            if (InstanceIDs != null)
            {
                RefreshHistory();
                if (SummaryMode)
                {
                    RefreshDBSummary();
                }
                else
                {
                    RefreshDBInfo();
                }
            }
        }

        private void Pivot(ref DataTable dt)
        {
            var pivotDT = new DataTable();
            pivotDT.Columns.Add("Setting");
            pivotDT.Columns.Add("Value");
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName is not "InstanceID" and not "DatabaseID")
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

        private void RefreshDBSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 ? true : Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
        }

        public DataTable GetDBInfo()
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
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 ? true : Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private void RefreshDBInfo()
        {
            DataTable dt = GetDBInfo();

            if (dt.Rows.Count == 1)
            {
                Pivot(ref dt);
            }
            else
            {
                dgv.DataSource = dt;
                dgv.Columns["InstanceID"].Visible = false;
                dgv.Columns["DatabaseID"].Visible = false;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    col.HeaderText = col.HeaderText.Titleize();
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
                dgv.Columns[1].Frozen = Common.FreezeKeyColumn; //hidden
                dgv.Columns[2].Frozen = Common.FreezeKeyColumn; //hidden
                dgv.Columns[3].Frozen = Common.FreezeKeyColumn;
            }
        }

        private void RefreshHistory()
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
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 ? true : Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
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

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvHistory);
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsRefreshInfo_Click(object sender, EventArgs e)
        {
            if (SummaryMode)
            {
                RefreshDBSummary();
            }
            else
            {
                RefreshDBInfo();
            }
        }

        private void TsCopyInfo_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void ExcludeStateChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsSummary_Click(object sender, EventArgs e)
        {
            SummaryMode = true;
            RefreshDBSummary();
        }

        private void TsDetail_Click(object sender, EventArgs e)
        {
            SummaryMode = false;
            RefreshDBInfo();
        }

        private void DBOptions_Load(object sender, EventArgs e)
        {
            if (DatabaseID > 0)
            {
                SummaryMode = false;
            }
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (SummaryMode)
            {
                var warningCols = new string[] { "Auto Create Stats Disabled", "Auto Update Stats Disabled", "Old Compat Level", "In Recovery", "Offline", "Trustworthy" };
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
                        r.Cells[col].SetStatusColor((Int32)r.Cells[col].Value > 0 ? DashColors.Fail : Color.White);
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

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvHistory);
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            using (var frm = new SelectColumns() { Columns = dgv.Columns })
            {
                frm.ShowDialog(this);
            }
        }
    }
}