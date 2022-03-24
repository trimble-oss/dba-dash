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

namespace DBADashGUI.Changes
{
    public partial class Alerts : UserControl
    {
        public Alerts()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;
 
        public bool UseAlertName
        {
            get
            {
                return pivotByAlertNameToolStripMenuItem.Checked;
            }
            set
            {
                pivotByAlertNameToolStripMenuItem.Checked = value;
            }
        }

        public void RefreshData()
        {
            refreshAlertConfig();
            refreshAlerts();
        }

        private DataTable getAlertsConfig()
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("dbo.AlertsConfig_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));

                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
         }

        private void refreshAlertConfig()
        {
            dgvAlertsConfig.Columns.Clear();
            dgvAlertsConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance" });

            DataTable dt = getAlertsConfig();
               
            string pivotCol = UseAlertName ? "name" : "Alert";

            foreach (DataRow r in dt.DefaultView.ToTable(true, pivotCol).Select("", pivotCol))
            {
                if (r[pivotCol] != DBNull.Value)
                {
                    DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn() { HeaderText = (string)r[pivotCol], Name = (string)r[pivotCol] };
                    dgvAlertsConfig.Columns.Add(col);
                }
            }
            string lastInstance = "";
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
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


        private DataTable getAlerts()
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("dbo.Alerts_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }          
        }

        private void refreshAlerts()
        {
        
            DataTable dt = getAlerts();            
            dgvAlerts.DataSource = dt;
            dgvAlerts.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                           
        }

        private void pivotByAlertNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            refreshAlertConfig();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvAlertsConfig);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshAlertConfig();
        }

        private void tsRefreshAlerts_Click(object sender, EventArgs e)
        {
            refreshAlerts();
        }

        private void tsCopyAlerts_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvAlerts);
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvAlertsConfig);
        }

        private void tsExcelAlerts_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvAlerts);
        }
    }
}
