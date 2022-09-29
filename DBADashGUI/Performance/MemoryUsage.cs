using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
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
        private List<int> MemoryCounters;
        private int dateGrouping = 0;
        private static readonly int MaxChartPoints = 1000;
        private string selectedClerk;
        private string selectedCounter;
        private string selectedCounterAlias;
        readonly ToolTip dgvToolTip = new() { AutomaticDelay = 100, AutoPopDelay = 60000, ReshowDelay = 100 };
        private int previousDurationMins = 0;

        private enum ChartViews
        {
            Pie,
            PerformanceCounter,
            MemoryClerk
        }

        private ChartViews ChartView
        {
            get {
                if (pieChart1.Visible)
                {
                    return ChartViews.Pie;
                }
                else if (performanceCounters1.Visible)
                {
                    return ChartViews.PerformanceCounter;
                }
                else
                {
                    return ChartViews.MemoryClerk;
                }
            }
            set
            {
                pieChart1.Visible = value == ChartViews.Pie;
                performanceCounters1.Visible = value == ChartViews.PerformanceCounter;
                chartClerk.Visible = value == ChartViews.MemoryClerk;
                tsDateGroup.Visible = value == ChartViews.MemoryClerk;
                tsAgg.Visible = value == ChartViews.MemoryClerk;
                tsPieChart.Enabled = value!= ChartViews.Pie;
            }
        }

        public void RefreshData()
        {
            isClerksRefreshed = false;
            isCountersRefreshed = false;
            isConfigRefreshed = false;
            ResetDateGroupingIfDurationChanged();
            RefreshCurrentTab();
            RefreshClerkLineChart();
            RefreshPerformanceChart();
        }

        private void ResetDateGroupingIfDurationChanged()
        {
            if (Math.Abs(DateRange.DurationMins - previousDurationMins) > 5)
            {
                dateGrouping = Common.DateGrouping(DateRange.DurationMins, MaxChartPoints);
                tsDateGroup.Text = Common.DateGroupString(dateGrouping);
                previousDurationMins = DateRange.DurationMins;
            }
        }

        private void RefreshCurrentTab()
        {
            if ((tab1.SelectedTab == tabClerks || ChartView== ChartViews.Pie) && !isClerksRefreshed)
            {
                RefreshClerks();
            }
            if (tab1.SelectedTab == tabConfig && !isConfigRefreshed)
            {
                RefreshConfig();
            }
            else if (tab1.SelectedTab == tabCounters && !isCountersRefreshed)
            {
                RefreshCounters();
            }
        }

        private void RefreshPerformanceChart()
        {
            if (ChartView== ChartViews.PerformanceCounter)
            {
                performanceCounters1.FromDate = DateRange.FromUTC;
                performanceCounters1.ToDate = DateRange.ToUTC;
                performanceCounters1.InstanceID = InstanceID;
                performanceCounters1.RefreshData();
            }
        }
        private void RefreshClerkLineChart()
        {
            if (ChartView == ChartViews.MemoryClerk)
            {
                ShowMemoryUsageForClerk();
            }
        }

        private void RefreshClerks()
        {
            var dt = GetMemoryUsage();
            dgv.AutoGenerateColumns = false;
            if (dgv.Columns.Count == 0)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn() {Name="colMemoryClerkType", HeaderText = "Memory Clerk Type", DataPropertyName = "MemoryClerkType" });
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colPages", HeaderText = "Pages KB", DataPropertyName = "pages_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor= DashColors.LinkColor});
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colVirtualMemoryCommitted", HeaderText = "Virtual Memory Committed KB", DataPropertyName = "virtual_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor});
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colAWEAllocated", HeaderText = "AWE Allocated KB", DataPropertyName = "awe_allocated_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor});
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colSharedMemoryReserved", HeaderText = "Shared Memory Reserved KB", DataPropertyName = "shared_memory_reserved_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits, LinkColor = DashColors.LinkColor});
                dgv.Columns.Add(new DataGridViewLinkColumn() { Name = "colSharedMemoryCommitted", HeaderText = "Shared Memory Committed KB", DataPropertyName = "shared_memory_committed_kb", DefaultCellStyle = Common.DataGridViewNumericCellStyleNoDigits,LinkColor = DashColors.LinkColor});
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = "colPagesPct", HeaderText = "Pages %", DataPropertyName = "Pct", DefaultCellStyle = new DataGridViewCellStyle() { Format = "P1" } });             
            }
            dgv.DataSource = dt;

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            if (ChartView == ChartViews.Pie)
            {
                ShowPie(ref dt);
            }
            isClerksRefreshed = true;
        }

        private void ShowPie(ref DataTable dt)
        {
            pieChart1.Series.Clear();

            static string labelPoint(ChartPoint chartPoint) =>
            string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation);
            SeriesCollection sc = new();
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

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                selectedClerk = (string)row["MemoryClerkType"];
                if (e.ColumnIndex == dgv.Columns["colPages"].Index)
                {
                    selectedCounter = "pages_kb";
                    selectedCounterAlias =selectedClerk +  " - Pages KB";
                }
                else if (e.ColumnIndex == dgv.Columns["colVirtualMemoryCommitted"].Index)
                {
                    selectedCounter = "virtual_memory_committed_kb";
                    selectedCounterAlias = selectedClerk +  " - Virtual Memory Committed KB";
                }
                else if (e.ColumnIndex == dgv.Columns["colAWEAllocated"].Index)
                {
                    selectedCounter = "awe_allocated_kb";
                    selectedCounterAlias = selectedClerk +  " - AWE Allocated KB";
                }
                else if(e.ColumnIndex== dgv.Columns["colSharedMemoryReserved"].Index)
                {
                    selectedCounter = "shared_memory_reserved_kb";
                    selectedCounterAlias=selectedClerk + " - Shared Memory Reserved KB";
                }
                else if (e.ColumnIndex == dgv.Columns["colSharedMemoryCommitted"].Index)
                {
                    selectedCounter = "shared_memory_committed_kb";
                    selectedCounterAlias =selectedClerk + " - Shared Memory Committed KB";
                }
                else
                {
                    return;
                }
                ShowMemoryUsageForClerk();
            }
        }

        private void ShowMemoryUsageForClerk(string format = "N0")
        {
            ChartView = ChartViews.MemoryClerk;    
            chartClerk.Series.Clear();
            var dt = GetMemoryClerkUsage(selectedClerk,dateGrouping,tsAgg.Text,selectedCounter);
            if (dt.Rows.Count > MaxChartPoints)
            {
                MessageBox.Show("Max Chart points exceeded.  Please select a narrower date range or increase the date grouping.","Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var columns = new Dictionary<string, columnMetaData>
            {
                {selectedCounter, new columnMetaData{Alias=selectedCounterAlias,isVisible=true } }
            };
            chartClerk.LegendLocation = LegendLocation.Top;
            chartClerk.AddDataTable(dt,columns,"SnapshotDate",false);
            chartClerk.AxisY.Clear();   
            chartClerk.AxisY.Add(new Axis() { 
                MinValue = 0,
                LabelFormatter = val => val.ToString(format)
            });
            tsAgg.Enabled = dateGrouping > 0;
        }

        private DataTable GetMemoryClerkUsage(string clerk,int? dateGrouping,string agg,string measure)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryClerkUsage_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("MemoryClerkType", clerk);
                cmd.Parameters.AddWithValue("Mins",dateGrouping);
                cmd.Parameters.AddWithValue("Agg", agg);
                cmd.Parameters.AddWithValue("Measure", measure);
                var dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshConfig()
        {
            var dt = GetMemoryConfig();
            dgvConfig.DataSource = dt;
            dgvConfig.Columns[0].Width = 400;
            dgvConfig.Columns[1].Width = 150;
            isConfigRefreshed = true;
        }

        private void RefreshCounters()
        {
            if (MemoryCounters == null || MemoryCounters.Count == 0) { 
                MemoryCounters = GetMemoryCounters();
            }
            performanceCounterSummaryGrid1.Counters = MemoryCounters;
            performanceCounterSummaryGrid1.InstanceID = InstanceID;
            performanceCounterSummaryGrid1.RefreshData();
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

        private List<int> GetMemoryCounters()
        {
            var Counters = new List<int>();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.MemoryCounters_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        Counters.Add(rdr.GetInt32(0));
                    }
                }
            }
            return Counters;
        }

        private void Tab1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshCurrentTab();
        }

     
        private void Dgv_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) {
                string toolTip = (string)((DataRowView)dgv.Rows[e.RowIndex].DataBoundItem)["MemoryClerkDescription"];
                dgvToolTip.SetToolTip(dgv, toolTip);
            }
        }

        private void MemoryUsage_Load(object sender, EventArgs e)
        {
            performanceCounterSummaryGrid1.ObjectLink = false;
            performanceCounterSummaryGrid1.InstanceLink = false;
            performanceCounterSummaryGrid1.CounterLink = false;
            performanceCounterSummaryGrid1.CounterSelected += PerformanceCounterSummaryGrid1_CounterSelected;
            Common.AddDateGroups(tsDateGroup,TsDateGroup_Click);
        }
        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = Common.DateGroupString(dateGrouping);
            previousDurationMins = DateRange.DurationMins;
            ShowMemoryUsageForClerk();
        }

        private void PerformanceCounterSummaryGrid1_CounterSelected(object sender, PerformanceCounterSummaryGrid.CounterSelectedEventArgs e)
        {
            performanceCounters1.CounterID = e.CounterID;
            performanceCounters1.CounterName = e.CounterName;
            ChartView = ChartViews.PerformanceCounter;
            RefreshPerformanceChart();
        }

        private void TsAGG_Click(object sender, EventArgs e)
        {
            avgToolStripMenuItem.Checked = avgToolStripMenuItem == sender;
            maxToolStripMenuItem.Checked = maxToolStripMenuItem == sender;
            minToolStripMenuItem.Checked = minToolStripMenuItem == sender;
            tsAgg.Text = ((ToolStripMenuItem)sender).Text;
            ShowMemoryUsageForClerk();
        }

        private void TsPieChart_Click(object sender, EventArgs e)
        {
            ChartView = ChartViews.Pie;
            RefreshCurrentTab();
        }
    }
}
