using LiveCharts;
using LiveCharts.Wpf;
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

namespace DBADashGUI.Performance
{
    public partial class MemoryUsage : UserControl
    {
        public MemoryUsage()
        {
            InitializeComponent();
        }

        public int InstanceID;

        private bool isClerksRefreshed = false;
        private bool isConfigRefreshed = false;
        private bool isCountersRefreshed = false;

        public void RefreshData()
        {
            isClerksRefreshed = false;
            isCountersRefreshed = false;
            isConfigRefreshed = false;
            refreshCurrentTab();
        }

        private void refreshCurrentTab()
        {
            if (tab1.SelectedTab == tabClerks && !isClerksRefreshed)
            {
                refreshClerks();
            }
            else if (tab1.SelectedTab == tabConfig && !isConfigRefreshed)
            {
                refreshConfig();
            }
            else if (tab1.SelectedTab == tabCounters && !isCountersRefreshed)
            {
                refreshCounters();
            }
        }

        private void refreshClerks()
        {
            var dt = GetMemoryUsage();
            chartHistory.Visible = false;
            pieChart1.Visible = true;
            performanceCounters1.Visible = false;
            dgv.AutoGenerateColumns = false;
            if (dgv.Columns.Count == 0)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn() {Name="colMemoryClerkType", HeaderText = "Memory Clerk Type", DataPropertyName = "MemoryClerkType" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colPages", HeaderText = "Pages KB", DataPropertyName = "pages_kb", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colVirtualMemoryCommitted", HeaderText = "Virtual Memory Committed KB", DataPropertyName = "virtual_memory_committed_kb", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colAWEAllocated", HeaderText = "AWE Allocated KB", DataPropertyName = "awe_allocated_kb", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colSharedMemoryReserved", HeaderText = "Shared Memory Reserved KB", DataPropertyName = "shared_memory_reserved_kb", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colSharedMemoryCommitted", HeaderText = "Shared Memory Committed KB", DataPropertyName = "shared_memory_committed_kb", DefaultCellStyle = new DataGridViewCellStyle() { Format = "N0" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colPagesPct", HeaderText = "Pages %", DataPropertyName = "Pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });             
            }
            dgv.DataSource = dt;

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            showPie(ref dt);
            isClerksRefreshed = true;
        }

        private void showPie(ref DataTable dt)
        {
            pieChart1.Series.Clear();
            string labelPoint(ChartPoint chartPoint) =>
           string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation);
            SeriesCollection sc = new SeriesCollection();
            Double other = 0;
            Double otherPct=0;
            bool dataLabels;
            foreach (DataRow r in dt.Rows)
            {
                var pages = Convert.ToDouble(r["pages_kb"]);
                var pct = Convert.ToDouble(r["Pct"]);
                dataLabels = pct > 0.05;
                if (pct > 0.02)
                {
                    var s = new PieSeries() { Title = (string)r["MemoryClerkType"], Values = new ChartValues<double> { pages }, LabelPoint = labelPoint, DataLabels = dataLabels, ToolTip = true};
                    sc.Add(s);
                }
                else
                {
                    other += pages;
                    otherPct += pct;
                }
            }
            if (other > 0)
            {
                dataLabels = otherPct > 0.05;
                var s = new PieSeries() { Title = "{Other}", Values = new ChartValues<double> { other }, LabelPoint = labelPoint, DataLabels = true, ToolTip = true};
                sc.Add(s);
            }
           
            pieChart1.Series = sc;
            pieChart1.LegendLocation = LegendLocation.Bottom;
        }

        public DataTable GetMemoryUsage()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd = new SqlCommand("dbo.MemoryUsage_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                var dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                if (e.ColumnIndex == dgv.Columns["colPages"].Index)
                {
                    showMemoryUsageForClerk((string)row["MemoryClerkType"],"pages_kb","Pages KB");
                }
                else if (e.ColumnIndex == dgv.Columns["colVirtualMemoryCommitted"].Index)
                {
                    showMemoryUsageForClerk((string)row["MemoryClerkType"], "virtual_memory_committed_kb", "Virtual Memory Committed KB");
                }
                else if (e.ColumnIndex == dgv.Columns["colAWEAllocated"].Index)
                {
                    showMemoryUsageForClerk((string)row["MemoryClerkType"], "awe_allocated_kb", "AWE Allocated KB");
                }
                else if(e.ColumnIndex== dgv.Columns["colSharedMemoryReserved"].Index)
                {
                    showMemoryUsageForClerk((string)row["MemoryClerkType"], "shared_memory_reserved_kb", "Shared Memory Reserved KB");
                }
                else if (e.ColumnIndex == dgv.Columns["colSharedMemoryCommitted"].Index)
                {
                    showMemoryUsageForClerk((string)row["MemoryClerkType"], "shared_memory_committed_kb", "Shared Memory Committed KB");
                }
            }
        }

        private void showMemoryUsageForClerk(string clerk,string col,string alias,string format="N0")
        {
            if (DateRange.DurationMins > 3000)
            {
                MessageBox.Show("Please select a narrower date range", "Date Range", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            chartHistory.Series.Clear();
            var dt = GetMemoryClerkUsage(clerk);
            var columns = new Dictionary<string, columnMetaData>
            {
                {col, new columnMetaData{Alias=alias,isVisible=true } }
            };
            chartHistory.AddDataTable(dt,columns,"SnapshotDate",false);
            chartHistory.AxisX[0].MinValue = DateRange.FromUTC.ToLocalTime().Ticks;
            chartHistory.AxisX[0].MaxValue = DateRange.ToUTC.ToLocalTime().Ticks;
            chartHistory.AxisY.Clear();   
            chartHistory.AxisY.Add(new Axis() { 
                MinValue = 0,
                LabelFormatter = val => val.ToString(format)
            });
            chartHistory.Visible = true;
            pieChart1.Visible = false;
            performanceCounters1.Visible = false;

        }

        private DataTable GetMemoryClerkUsage(string clerk)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryClerkUsage_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("MemoryClerkType", clerk);
                var dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void refreshConfig()
        {
            var dt = GetMemoryConfig();
            dgvConfig.DataSource = dt;
            dgvConfig.Columns[0].Width = 400;
            dgvConfig.Columns[1].Width = 150;
            isConfigRefreshed = true;
        }

        private void refreshCounters()
        {
            var dt = GetMemoryCounters();
            dgvCounters.AutoGenerateColumns = false;
            if (dgvCounters.Columns.Count == 0)
            {
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colObject", DataPropertyName = "object_name", HeaderText = "Object"});
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colCounter", DataPropertyName = "counter_name", HeaderText = "Counter" });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colInstance", DataPropertyName = "instane_name", HeaderText = "Instance" });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colMaxValue", DataPropertyName = "MaxValue", HeaderText = "Max Value", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.########" } });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colMinValue", DataPropertyName = "MinValue", HeaderText = "Min Value", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.########" } });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colAvgValue", DataPropertyName = "AvgValue", HeaderText = "Avg Value", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.########" } });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colTotal", DataPropertyName = "Total", HeaderText = "Total", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.########" } });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colSampleCount", DataPropertyName = "SampleCount", HeaderText = "Sample Count", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.########" } });
                dgvCounters.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colCurrentValue", DataPropertyName = "CurrentValue", HeaderText = "Current Value", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.########" } });
                dgvCounters.Columns.Add(new DataGridViewLinkColumn() { Name = "colView", HeaderText = "View", UseColumnTextForLinkValue = true, Text = "View", });
            }
            dgvCounters.DataSource = dt;
            dgvCounters.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            if (performanceCounters1.Visible)
            {
                performanceCounters1.InstanceID = InstanceID;
                performanceCounters1.FromDate = DateRange.FromUTC;
                performanceCounters1.ToDate = DateRange.ToUTC;
                performanceCounters1.RefreshData();
            }
            isCountersRefreshed = true;
        }

        private DataTable GetMemoryConfig()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryConfig_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private DataTable GetMemoryCounters()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryCounters_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void tab1_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshCurrentTab();
        }

        private void dgvCounters_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgvCounters.Rows[e.RowIndex].DataBoundItem;
                if(e.ColumnIndex== dgvCounters.Columns["colView"].Index)
                {
                    var objectName = (string)row["object_name"];
                    var counterName = (string)row["counter_name"];
                    var instanceName = (string)row["instance_name"];
                    performanceCounters1.CounterID = (int)row["CounterID"];
                    performanceCounters1.FromDate = DateRange.FromUTC;
                    performanceCounters1.ToDate = DateRange.ToUTC;
                    performanceCounters1.InstanceID = InstanceID;
                    performanceCounters1.Visible = true;
                    performanceCounters1.CounterName = objectName + "\\" + counterName + (instanceName == "" ? "" : "\\" + instanceName);
                    pieChart1.Visible = false;
                    chartHistory.Visible = false;

                    performanceCounters1.RefreshData();
                }
            }
        }

        ToolTip dgvToolTip = new ToolTip() { AutomaticDelay = 100, AutoPopDelay =60000, ReshowDelay = 100 };
        private void dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) {
                string toolTip = (string)((DataRowView)dgv.Rows[e.RowIndex].DataBoundItem)["MemoryClerkDescription"];
                dgvToolTip.SetToolTip(dgv, toolTip);
            }
        }
    }
}
