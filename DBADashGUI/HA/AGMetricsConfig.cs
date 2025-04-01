using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;

namespace DBADashGUI.HA
{
    public partial class AGMetricsConfig : Form
    {
        public AGMetricsConfig()
        {
            InitializeComponent();
            dgvConfig.AutoGenerateColumns = false;
            dgvConfig.Columns.AddRange(
                new DataGridViewTextBoxColumn { DataPropertyName = "MetricName", HeaderText = "Metric", ReadOnly = true },
                new DataGridViewTextBoxColumn { DataPropertyName = "MetricType", HeaderText = "Type", ReadOnly = true },
                new DataGridViewCheckBoxColumn { DataPropertyName = "IsEnabled", HeaderText = "Enable/Disable", ReadOnly = false }
                );
            dgvConfig.DataBindingComplete += (s, e) => dgvConfig.AutoResizeColumns();
            this.ApplyTheme();
        }

        public int InstanceID { get; set; } = -1;


        private static async Task<DataTable> GetAGMetricsConfig(int instanceId)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("AvailabilityGroupMetricsConfig_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            await cn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);
            dt.Columns["IsEnabled"]!.ReadOnly = false;
            return dt;
        }

        private static async Task UpdateAGMetrics(int instanceId, IEnumerable<string> enabledMetrics)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("AvailabilityGroupMetricsConfig_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.AddWithValue("@EnabledMetrics", string.Join(',', enabledMetrics));
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task DeleteAGMetrics(int instanceId)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("AvailabilityGroupMetricsConfig_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task RefreshData()
        {
            var dt = await GetAGMetricsConfig(InstanceID);
            dgvConfig.DataSource = new DataView(dt);
            dgvConfig.Refresh();
            chkInherit.Checked = false;
            chkInherit.Visible = InstanceID != -1;
            Text = $"Availability Group Metrics Configuration " + (InstanceID == -1 ? " (Root)" : " (Instance)");
            if (dt.Rows.Count > 0)
            {
                var instanceId = (int)dt.Rows[0]["InstanceID"];
                if (instanceId == -1 && InstanceID > 0)
                {
                    chkInherit.Checked = true;
                }
            }
            ToggleGrid(!chkInherit.Checked);
        }

        private async void AGMetricsConfig_Load(object sender, EventArgs e)
        {
            if (InstanceID == 0)
            {
                MessageBox.Show("Invalid Instance ID", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
            }
            await RefreshData();
        }

        private async void OK_Click(object sender, EventArgs e)
        {
            var dt = ((DataView)dgvConfig.DataSource).Table;
            var enabledMetrics = dt!.Rows.Cast<DataRow>().Where(r => (bool)r["IsEnabled"])
                .Select(r => (string)r["MetricName"]);
            try
            {
                if(chkInherit.Checked && InstanceID >0)
                {
                    await DeleteAGMetrics(InstanceID);
                }
                else
                {
                    await UpdateAGMetrics(InstanceID, enabledMetrics);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            this.DialogResult = DialogResult.OK;
        }

        private async void Inherit_Click(object sender, EventArgs e)
        {
            if(chkInherit.Checked)
            {
                await RefreshData();
                chkInherit.Checked = true;
            }
            ToggleGrid(!chkInherit.Checked);
            
        }

        private void ToggleGrid(bool enabled)
        {
            dgvConfig.Enabled = enabled;
            if(enabled)
            {
                dgvConfig.ApplyTheme();
            }
            else
            {
                dgvConfig.BackgroundColor = Color.LightGray;
                dgvConfig.DefaultCellStyle.BackColor = Color.LightGray;
            }
        }
    }
}
