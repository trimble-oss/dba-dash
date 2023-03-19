using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

        private static readonly DataGridViewColumn[] Cols ={
                new DataGridViewLinkColumn(){ Name="Acknowledge", HeaderText="Acknowledge", Text="Acknowledge", LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewLinkColumn(){ Name="Configure", HeaderText="Configure", Text="Configure", UseColumnTextForLinkValue=true,LinkColor = DashColors.LinkColor, SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ Name="Instance", HeaderText="Instance", DataPropertyName="Instance", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Alert Name", DataPropertyName="Alert Name", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Message ID", DataPropertyName="Message ID", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Severity", DataPropertyName="Severity", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewCheckBoxColumn(){ HeaderText="Enabled", DataPropertyName="Enabled", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Delay Between Responses", DataPropertyName="Delay Between Responses", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ Name="Last Occurrence", HeaderText="Last Occurrence", DataPropertyName="Last Occurrence", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ Name="Acknowledged Date", HeaderText="Acknowledged Date", DataPropertyName="AcknowledgeDate", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Days Since Last Occurrence", DataPropertyName="Days Since Last Occurrence", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Last Response", DataPropertyName="Last Response", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Notification Message", DataPropertyName="Notification Message", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewCheckBoxColumn(){ HeaderText="Include Event Description", DataPropertyName="Include Event Description", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Database Name", DataPropertyName="Database Name", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Event Description Keyword", DataPropertyName="Event Description Keyword", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Occurrence Count", DataPropertyName="Occurrence Count", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Count Reset", DataPropertyName="Count Reset", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Job ID", DataPropertyName="Job ID", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Job Name", DataPropertyName="Job Name", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewCheckBoxColumn(){ HeaderText="Has Notification", DataPropertyName="Has Notification", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Category ID", DataPropertyName="Category ID", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewTextBoxColumn(){ HeaderText="Performance Condition", DataPropertyName="Performance Condition", SortMode = DataGridViewColumnSortMode.Automatic},
                new DataGridViewCheckBoxColumn(){ HeaderText="Is Critical Alert", DataPropertyName="Is Critical Alert", SortMode = DataGridViewColumnSortMode.Automatic},
        };

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
            if (dgvAlerts.Columns.Count == 0)
            {
                dgvAlerts.Columns.AddRange(Cols);
                dgvAlerts.AutoGenerateColumns = false;
            }
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

        private void DgvAlerts_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvAlerts.Rows[idx].DataBoundItem;
                var status = (DBADashStatus.DBADashStatusEnum)row["AlertStatus"];
                dgvAlerts.Rows[idx].Cells["Last Occurrence"].SetStatusColor(status);

                dgvAlerts.Rows[idx].Cells["Acknowledge"].Value = status switch
                {
                    DBADashStatus.DBADashStatusEnum.Acknowledged => "Clear",
                    DBADashStatus.DBADashStatusEnum.Critical => "Acknowledge",
                    DBADashStatus.DBADashStatusEnum.Warning => "Acknowledge",
                    _ => ""
                };
            }
        }

        private void DgvAlerts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvAlerts.Rows[e.RowIndex].DataBoundItem;
                int id = (int)row["id"];
                int instanceID = (int)row["InstanceID"];
                var status = (DBADashStatus.DBADashStatusEnum)row["AlertStatus"];
                if (dgvAlerts.Columns[e.ColumnIndex].Name == "Acknowledge")
                {
                    AcknowledgeAlert(instanceID, id, status == DBADashStatus.DBADashStatusEnum.Acknowledged);
                    RefreshAlerts();
                }
                else if (dgvAlerts.Columns[e.ColumnIndex].Name == "Configure")
                {
                    AlertConfig frm = new() { AlertRow = row.Row };
                    frm.ShowDialog();
                    if (frm.DialogResult == DialogResult.OK)
                    {
                        RefreshAlerts();
                    }
                }
            }
        }

        private static void AcknowledgeAlert(int InstanceID, int id, bool clear = false)
        {
            using (SqlConnection cn = new(Common.ConnectionString))
            using (SqlCommand cmd = new("dbo.Alerts_Ack", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddIfGreaterThanZero("id", id);
                cmd.Parameters.AddIfGreaterThanZero("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("Clear", clear);
                cmd.ExecuteNonQuery();
            }
        }

        private void AcknowledgeALLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dt = (DataTable)dgvAlerts.DataSource;
            if (dt == null) return;
            var instanceIDs = dt.AsEnumerable()
                .Where(row => row.Field<int>("AlertStatus") == 1 || row.Field<int>("AlertStatus") == 2)
                .Select(row => row.Field<int>("InstanceID"))
                .Distinct();
            foreach (var instanceID in instanceIDs)
            {
                AcknowledgeAlert(instanceID, -1, false);
            }
            RefreshAlerts();
        }

        private void ClearALLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dt = (DataTable)dgvAlerts.DataSource;
            if (dt == null) return;
            var instanceIDs = dt.AsEnumerable()
                .Where(row => row.Field<int>("AlertStatus") == 5)
                .Select(row => row.Field<int>("InstanceID"))
                .Distinct();
            foreach (var instanceID in instanceIDs)
            {
                AcknowledgeAlert(instanceID, -1, true);
            }
            RefreshAlerts();
        }
    }
}