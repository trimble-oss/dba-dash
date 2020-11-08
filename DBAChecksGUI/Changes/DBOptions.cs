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

namespace DBAChecksGUI.Changes
{
    public partial class DBOptions : UserControl
    {

        public List<Int32> InstanceIDs;
        public Int32 DatabaseID;

        public DBOptions()
        {
            InitializeComponent();
        }

        public void RefreshData()
        {
            refreshHistory();
            if (InstanceIDs.Count == 1)
            {
                refreshDBInfo();
            }
            else
            {
                tsRefreshInfo.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                tsRefreshInfo.Text = "Show All";
                lblInfo.Visible = true;
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

        private void refreshDBInfo()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                SqlCommand cmd = new SqlCommand("dbo.DatabasesAllInfo_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
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
            tsRefreshInfo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsRefreshInfo.Text = "Refresh";
            lblInfo.Visible = false;
        }

        private void refreshHistory()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                SqlCommand cmd = new SqlCommand("dbo.DBOptionsHistory_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
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
                foreach(DataRow r in dt.Rows)
                {
                    if(r["OldValue"].GetType() == typeof(byte[])){
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
            refreshDBInfo();
        }

        private void tsCopyInfo_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void excludeStateChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }
    }
}
