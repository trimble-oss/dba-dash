using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Changes
{
    public partial class ResourceGovernor : UserControl, INavigation, ISetContext
    {
        private List<int> InstanceIDs;
        private List<int> backupInstanceIDs;

        public bool CanNavigateBack => tsBack.Enabled;

        public ResourceGovernor()
        {
            InitializeComponent();
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        private void RefreshData()
        {
            tsBack.Enabled = false;
            backupInstanceIDs = new List<int>();
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
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
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            colLinkInstance.Visible = !historyMode;
            colInstance.Visible = historyMode;
            colValidTo.Visible = historyMode;
            colSnapshotDate.Visible = !historyMode;
            colDiff.Visible = historyMode;

            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private static DataTable GetResourceGovernorConfiguration(List<int> InstanceIDs)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.ResourceGovernorConfiguration_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private static DataTable GetResourceGovernorConfigurationHistory(int InstanceID)
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

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == colScript.Index)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                string script = (string)row["script"];
                string instance = (string)row["Instance"];
                Common.ShowCodeViewer(script, "Resource Governor - " + instance);
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == colLinkInstance.Index)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                int instanceId = (int)row["InstanceID"];
                backupInstanceIDs = InstanceIDs;
                tsBack.Enabled = true;
                InstanceIDs = new List<int>() { instanceId };
                RefreshDataLocal();
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == colDiff.Index)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                string script = (string)row["script"];
                string scriptPrevious = Convert.ToString(row["script_previous"]);
                var frm = new Diff();
                frm.SetText(scriptPrevious, script);
                frm.ShowDialog();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataLocal();
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                InstanceIDs = backupInstanceIDs;
                RefreshData();
                return true;
            }
            else
            {
                return false;
            }
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            bool diffVisible = colDiff.Visible;
            colDiff.Visible = false;
            colScript.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colScript.Visible = true;
            colDiff.Visible = diffVisible;
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            tsCompare.Enabled = dgv.SelectedRows.Count == 2;
        }

        private void TsCompare_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 2)
            {
                DataRowView row1 = (DataRowView)dgv.SelectedRows[0].DataBoundItem;
                DataRowView row2 = (DataRowView)dgv.SelectedRows[1].DataBoundItem;
                string script1 = "/* " + (string)row1["Instance"] + " (" + ((DateTime)row1["ValidFrom"]).ToString("yyyy-MM-dd hh:mm") + ")" + " */" + Environment.NewLine + (string)row1["script"];
                string script2 = "/* " + (string)row2["Instance"] + " (" + ((DateTime)row2["ValidFrom"]).ToString("yyyy-MM-dd hh:mm") + ")" + " */" + Environment.NewLine + (string)row2["script"];
                var frm = new Diff();
                frm.SetText(script1, script2);
                frm.ShowDialog();
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            bool diffVisible = colDiff.Visible;
            colDiff.Visible = false;
            colScript.Visible = false;
            Common.PromptSaveDataGridView(ref dgv);
            colScript.Visible = true;
            colDiff.Visible = diffVisible;
        }

        private void ResourceGovernor_Load(object sender, EventArgs e)
        {
            dgv.ApplyTheme();
        }
    }
}