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

        private void refreshAlertConfig()
        {
            dgvAlertsConfig.Columns.Clear();

            dgvAlertsConfig.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Instance", HeaderText = "Instance" });
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.AlertsConfig_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
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
                    string instance = (string)r["Instance"];
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

                        row.Cells[idx].Style.BackColor = enabled ? (notification ? Color.Green : Color.Yellow) : Color.Red;
                    }
                    lastInstance = instance;
                }
                dgvAlertsConfig.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                dgvAlertsConfig.Rows.AddRange(rows.ToArray());
            }
        }


        private void refreshAlerts()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                SqlCommand cmd = new SqlCommand("dbo.Alerts_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvAlerts.DataSource = dt;
            }
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
    }
}
