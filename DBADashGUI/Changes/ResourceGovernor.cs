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
            dgv.RegisterClearFilter(tsClearFilter);
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
            var historyMode = false;
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

            dgv.DataSource = new DataView(dt);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private static DataTable GetResourceGovernorConfiguration(List<int> InstanceIDs)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ResourceGovernorConfiguration_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private static DataTable GetResourceGovernorConfigurationHistory(int InstanceID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ResourceGovernorConfigurationHistory_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            switch (e.RowIndex)
            {
                case >= 0 when e.ColumnIndex == colScript.Index:
                {
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    var script = (string)row["script"];
                    var instance = (string)row["Instance"];
                    Common.ShowCodeViewer(script, "Resource Governor - " + instance);
                    break;
                }
                case >= 0 when e.ColumnIndex == colLinkInstance.Index:
                {
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    var instanceId = (int)row["InstanceID"];
                    backupInstanceIDs = InstanceIDs;
                    tsBack.Enabled = true;
                    InstanceIDs = new List<int>() { instanceId };
                    RefreshDataLocal();
                    break;
                }
                case >= 0 when e.ColumnIndex == colDiff.Index:
                {
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    var script = (string)row["script"];
                    var scriptPrevious = Convert.ToString(row["script_previous"]);
                    var frm = new Diff();
                    frm.SetText(scriptPrevious, script);
                    frm.ShowDialog();
                    break;
                }
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
            var diffVisible = colDiff.Visible;
            colDiff.Visible = false;
            colScript.Visible = false;
            dgv.CopyGrid();
            colScript.Visible = true;
            colDiff.Visible = diffVisible;
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            tsCompare.Enabled = dgv.SelectedRows.Count == 2;
        }

        private void TsCompare_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count != 2) return;
            var row1 = (DataRowView)dgv.SelectedRows[0].DataBoundItem;
            var row2 = (DataRowView)dgv.SelectedRows[1].DataBoundItem;
            var script1 = "/* " + (string)row1["Instance"] + " (" + ((DateTime)row1["ValidFrom"]).ToString("yyyy-MM-dd hh:mm") + ")" + " */" + Environment.NewLine + (string)row1["script"];
            var script2 = "/* " + (string)row2["Instance"] + " (" + ((DateTime)row2["ValidFrom"]).ToString("yyyy-MM-dd hh:mm") + ")" + " */" + Environment.NewLine + (string)row2["script"];
            var frm = new Diff();
            frm.SetText(script1, script2);
            frm.ShowDialog();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            var diffVisible = colDiff.Visible;
            colDiff.Visible = false;
            colScript.Visible = false;
            dgv.ExportToExcel();
            colScript.Visible = true;
            colDiff.Visible = diffVisible;
        }

        private void ResourceGovernor_Load(object sender, EventArgs e)
        {
            dgv.ApplyTheme();
        }
    }
}