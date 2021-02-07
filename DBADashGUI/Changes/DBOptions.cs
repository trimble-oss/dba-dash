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
        }

        private void refreshDBSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DBSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgv.DataSource = dt;
                }
            }
        }

        private void refreshDBInfo()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DatabasesAllInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                    if (DatabaseID > 0)
                    {
                        cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                    }
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
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
                    }
                }
            }
        }

        private void refreshHistory()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.DBOptionsHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", String.Join(",", InstanceIDs));
                    if (DatabaseID > 0)
                    {
                        cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                    }
                    cmd.Parameters.AddWithValue("ExcludeStateChanges", excludeStateChangesToolStripMenuItem.Checked);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
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
                }
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
                        r.Cells[col].Style.BackColor = (Int32)dgv.Rows[idx].Cells[col].Value > 0 ? Color.Yellow : Color.White;
                    }
                    foreach (var col in criticalCols)
                    {
                        r.Cells[col].Style.BackColor = (Int32)dgv.Rows[idx].Cells[col].Value > 0 ? Color.Red : Color.White;
                    }
                }
            }
        }
    }
}
