using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class Alerts : UserControl, ISetContext
    {
        public Alerts()
        {
            InitializeComponent();
        }

        private List<Int32> InstanceIDs;

        public bool UseAlertName
        {
            get => pivotByAlertNameToolStripMenuItem.Checked; set => pivotByAlertNameToolStripMenuItem.Checked = value;
        }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.RegularInstanceIDs.ToList();
            RefreshAlertConfig();
            RefreshAlerts();
        }

        private DataTable GetAlertsConfig()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.AlertsConfig_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        private void RefreshAlertConfig()
        {
            dgvAlertsConfig.Columns.Clear();
            dgvAlertsConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance" });

            DataTable dt = GetAlertsConfig();

            string pivotCol = UseAlertName ? "name" : "Alert";

            foreach (DataRow r in dt.DefaultView.ToTable(true, pivotCol).Select("", pivotCol))
            {
                if (r[pivotCol] != DBNull.Value)
                {
                    DataGridViewTextBoxColumn col = new() { HeaderText = (string)r[pivotCol], Name = (string)r[pivotCol] };
                    dgvAlertsConfig.Columns.Add(col);
                }
            }
            string lastInstance = "";
            List<DataGridViewRow> rows = new();
            DataGridViewRow row = null;
            foreach (DataRow r in dt.Rows)
            {
                string instance = (string)r["InstanceDisplayName"];
                if (instance != lastInstance)
                {
                    row = new DataGridViewRow();
                    row.CreateCells(dgvAlertsConfig);
                    row.Cells[0].Value = instance;
                    rows.Add(row);
                }
                if (r[pivotCol] != DBNull.Value)
                {
                    string alertName = (string)r[pivotCol];
                    var idx = dgvAlertsConfig.Columns[alertName].Index;

                    bool enabled = (Byte)r["enabled"] == 0x1;
                    bool notification = (Int32)r["has_notification"] > 0;

                    row.Cells[idx].Value = enabled ? "Y" + (notification ? "" : "**") : "N";
                    if (enabled && !notification)
                    {
                        row.Cells[idx].ToolTipText = "Alert configured without notification";
                    }

                    row.Cells[idx].SetStatusColor(enabled ? (notification ? DashColors.Success : DashColors.Warning) : DashColors.Fail);
                }
                lastInstance = instance;
            }
            dgvAlertsConfig.Rows.AddRange(rows.ToArray());
            dgvAlertsConfig.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetAlerts()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Alerts_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        private void RefreshAlerts()
        {
            DataTable dt = GetAlerts();
            dgvAlerts.DataSource = dt;
            dgvAlerts.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void PivotByAlertNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshAlertConfig();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvAlertsConfig);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshAlertConfig();
        }

        private void TsRefreshAlerts_Click(object sender, EventArgs e)
        {
            RefreshAlerts();
        }

        private void TsCopyAlerts_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvAlerts);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvAlertsConfig);
        }

        private void TsExcelAlerts_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvAlerts);
        }
    }
}