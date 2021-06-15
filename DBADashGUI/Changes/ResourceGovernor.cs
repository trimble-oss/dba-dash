using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class ResourceGovernor : UserControl
    {
        public List<Int32> InstanceIDs;
        private List<Int32> backupInstanceIDs;
        public ResourceGovernor()
        {
            InitializeComponent();
        }


        public void RefreshData()
        {
            tsBack.Visible = false;
            backupInstanceIDs = new List<int>();
            refreshData();
        }

        private void refreshData()
        {
            dgv.AutoGenerateColumns = false;
            DataTable dt;
            bool historyMode = false;
            if (InstanceIDs.Count == 1)
            {
                historyMode = true;
                dt = GetResourceGovernorConfigurationHistory(InstanceIDs[0]);
            }
            else
            {
                dt = GetResourceGovernorConfiguration(InstanceIDs);
            }
            Common.ConvertUTCToLocal(ref dt);
            colLinkInstance.Visible = !historyMode;
            colInstance.Visible = historyMode;
            colValidTo.Visible = historyMode;
            colSnapshotDate.Visible = !historyMode;
            colDiff.Visible = historyMode;

            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }


        private static DataTable GetResourceGovernorConfiguration(List<Int32> InstanceIDs)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd = new SqlCommand("dbo.ResourceGovernorConfiguration_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
        private DataTable GetResourceGovernorConfigurationHistory(int InstanceID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.ResourceGovernorConfigurationHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == colScript.Index)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                string script = (string)row["script"];
                var frm = new CodeViewer() { SQL = script };
                frm.ShowDialog();
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == colLinkInstance.Index)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                int instanceId = (int)row["InstanceID"];
                backupInstanceIDs = InstanceIDs;
                tsBack.Visible = true;
                InstanceIDs = new List<int>() { instanceId };
                refreshData();
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == colDiff.Index)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                string script = (string)row["script"];
                string scriptPrevious = Convert.ToString(row["script_previous"]);
                var frm = new Diff();
                frm.setText(scriptPrevious, script);
                frm.ShowDialog();
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            InstanceIDs = backupInstanceIDs;
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }
    }
}
