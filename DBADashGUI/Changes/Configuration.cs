using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class Configuration : UserControl
    {
        public Configuration()
        {
            InitializeComponent();
        }

        public List<int> InstanceIDs;

        private DataTable GetConfiguration()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Configuration_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("@ConfiguredOnly", configuredOnlyToolStripMenuItem.Checked);
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            cmd.Parameters.AddWithValue("AdviceOnly", adviceConfiguredToolStripMenuItem.Checked);
            DataTable dt = new();
            da.Fill(dt);
            return dt;
        }

        public void RefreshData()
        {
            dgvConfig.Columns.Clear();
            dgvConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance", Frozen = Common.FreezeKeyColumn });

            var dt = GetConfiguration();

            foreach (DataRow r in dt.DefaultView.ToTable(true, "name").Rows)
            {
                DataGridViewTextBoxColumn col = new() { HeaderText = (string)r["name"], Name = (string)r["name"] };
                dgvConfig.Columns.Add(col);
            }
            var lastInstance = "";
            List<DataGridViewRow> rows = new();
            DataGridViewRow row = null;
            foreach (DataRow r in dt.Rows)
            {
                var instance = (string)r["ConnectionID"];
                var displayName = (string)r["InstanceDisplayName"];
                if (instance != lastInstance)
                {
                    row = new DataGridViewRow();
                    row.CreateCells(dgvConfig);
                    row.Cells[0].Value = displayName;
                    rows.Add(row);
                }

                var configName = (string)r["name"];
                var idx = dgvConfig.Columns[configName]!.Index;
                row!.Cells[idx].Value = r["value"];
                var isDefault = (bool?)r["IsDefault"].DBNullToNull();

                DBADashStatus.DBADashStatusEnum status;
                string notice;
                if (AdviceHighlighting)
                {
                    notice = (string)r["ConfigurationNotice"].DBNullToNull();
                    status = DBADashStatus.ConvertToDBADashStatusEnum((int)r["ConfigurationStatus"]) ?? DBADashStatus.DBADashStatusEnum.NA;
                }
                else
                {
                    status = isDefault == true ? DBADashStatus.DBADashStatusEnum.NA : DBADashStatus.DBADashStatusEnum.WarningLow;
                    notice = isDefault == true ? "Default" : "Not Default";
                }
                row.Cells[idx].ToolTipText = notice;

                row.Cells[idx].SetStatusColor(status);
                row.Cells[idx].Style.Font = isDefault == false ? new Font(dgvConfig.Font, FontStyle.Bold) : new Font(dgvConfig.Font, FontStyle.Italic);
                lastInstance = instance;
            }
            dgvConfig.Rows.AddRange(rows.ToArray());
            dgvConfig.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private bool AdviceHighlighting => adviceConfiguredALLToolStripMenuItem.Checked || adviceConfiguredToolStripMenuItem.Checked;

        private void ConfiguredOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            configuredOnlyToolStripMenuItem.Checked = configuredOnlyToolStripMenuItem == item;
            configuredALLToolStripMenuItem.Checked = configuredALLToolStripMenuItem == item;
            adviceConfiguredALLToolStripMenuItem.Checked = adviceConfiguredALLToolStripMenuItem == item;
            adviceConfiguredToolStripMenuItem.Checked = adviceConfiguredToolStripMenuItem == item;
            RefreshData();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgvConfig.CopyGrid();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvConfig.ExportToExcel();
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvConfig.PromptColumnSelection();
        }
    }
}