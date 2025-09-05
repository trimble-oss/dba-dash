using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.Performance.IMetric;

namespace DBADashGUI.HA
{
    public partial class RepositoryMetricsConfig : Form
    {
        public RepositoryMetricsConfig()
        {
            InitializeComponent();
            dgvConfig.AutoGenerateColumns = false;
            dgvConfig.Columns.AddRange(
                new DataGridViewTextBoxColumn { DataPropertyName = "MetricName", HeaderText = "Metric", ReadOnly = true },
                new DataGridViewTextBoxColumn { DataPropertyName = "MetricLevel", HeaderText = "Level", ReadOnly = true },
                new DataGridViewCheckBoxColumn { DataPropertyName = "IsEnabled", HeaderText = "Enable/Disable", ReadOnly = false }
                );
            dgvConfig.DataBindingComplete += (s, e) => dgvConfig.AutoResizeColumns();
            this.ApplyTheme();
        }

        public enum RepositoryMetricTypes
        {
            AG,
            LogShipping,
            SlowQueries,
            Databases
        }

        public int InstanceID { get; set; } = -1;
        public RepositoryMetricTypes MetricType { get; set; } = RepositoryMetricTypes.AG;

        private static async Task<DataTable> GetAGMetricsConfig(int instanceId, RepositoryMetricTypes metricType)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("RepositoryMetricsConfig_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.AddWithValue("MetricType", metricType.ToString());
            await cn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);
            dt.Columns["IsEnabled"]!.ReadOnly = false;
            return dt;
        }

        private static async Task UpdateAGMetrics(int instanceId, IEnumerable<string> enabledMetrics, RepositoryMetricTypes metricType)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("RepositoryMetricsConfig_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.AddWithValue("@EnabledMetrics", string.Join(',', enabledMetrics));
            cmd.Parameters.AddWithValue("MetricType", metricType.ToString());

            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task DeleteAGMetrics(int instanceId, RepositoryMetricTypes metricType)
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("RepositoryMetricsConfig_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", instanceId);
            cmd.Parameters.AddWithValue("MetricType", metricType.ToString());
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public string TitleText
        {
            get
            {
                var level = InstanceID == -1 ? " (Root)" : " (Instance)";
                return MetricType switch
                {
                    RepositoryMetricTypes.AG => $"Availability Group Metrics Configuration {level}",
                    RepositoryMetricTypes.LogShipping => $"Log Shipping Metrics Configuration {level}",
                    RepositoryMetricTypes.SlowQueries => $"Slow Queries Metrics Configuration {level}",
                    RepositoryMetricTypes.Databases => $"Database Metrics Configuration {level}",
                    _ => throw new NotImplementedException()
                };
            }
        }

        private async Task RefreshData()
        {
            var dt = await GetAGMetricsConfig(InstanceID, MetricType);
            dgvConfig.DataSource = new DataView(dt);
            dgvConfig.Refresh();
            chkInherit.Checked = false;
            chkInherit.Visible = InstanceID != -1;
            Text = TitleText;
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
                if (chkInherit.Checked && InstanceID > 0)
                {
                    await DeleteAGMetrics(InstanceID, MetricType);
                }
                else
                {
                    await UpdateAGMetrics(InstanceID, enabledMetrics, MetricType);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return;
            }
            this.DialogResult = DialogResult.OK;
        }

        private async void Inherit_Click(object sender, EventArgs e)
        {
            if (chkInherit.Checked)
            {
                await RefreshData();
                chkInherit.Checked = true;
            }
            ToggleGrid(!chkInherit.Checked);
        }

        private void ToggleGrid(bool enabled)
        {
            dgvConfig.Enabled = enabled;
            if (enabled)
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