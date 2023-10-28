using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    public partial class WaitsSummary : UserControl, ISetContext, IRefreshData
    {
        public WaitsSummary()
        {
            InitializeComponent();
        }

        private int InstanceID { get; set; }
        private string selectedWaitType;

        private int dateGrouping = 1;
        private int mins;

        public int DateGrouping
        {
            get => dateGrouping;
            set
            {
                dateGrouping = value;
                tsDateGroup.Text = DateHelper.DateGroupString(value);
            }
        }

        public void SetContext(DBADashContext context)
        {
            InstanceID = context.InstanceID;
            RefreshData();
        }

        public void RefreshData()
        {
            if (mins != DateRange.DurationMins)
            {
                DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 300);
                mins = DateRange.DurationMins;
            }
            var dt = GetWaitsSummaryDT();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
            RefreshChart();
        }

        public DataTable GetWaitsSummaryDT()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.WaitsSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("UTCOffset", DateHelper.UtcOffset);

                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetWaitsDT(string waitType)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.Waits_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.Parameters.AddWithValue("WaitType", waitType);
                cmd.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
                if (DateRange.HasTimeOfDayFilter)
                {
                    cmd.Parameters.AddWithValue("Hours", DateRange.TimeOfDay.AsDataTable());
                }
                if (DateRange.HasDayOfWeekFilter)
                {
                    cmd.Parameters.AddWithValue("DaysOfWeek", DateRange.DayOfWeek.AsDataTable());
                }
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == colHelp.Index)
            {
                string wait = (string)dgv[colWaitType.Index, e.RowIndex].Value;
                var psi = new ProcessStartInfo("https://www.sqlskills.com/help/waits/" + wait.ToLower() + "/") { UseShellExecute = true };
                Process.Start(psi);
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == colWaitType.Index)
            {
                selectedWaitType = (string)dgv[colWaitType.Index, e.RowIndex].Value;
                RefreshChart();
            }
        }

        private readonly Dictionary<string, ColumnMetaData> columns = new()
        {
                {"AvgWaitTimeMs", new ColumnMetaData{Name="Avg Wait Time (ms)",IsVisible=false } },
                {"SampleDurationSec", new ColumnMetaData{Name="Sample Duration (sec)",IsVisible=false } },
                {"SignalWaitPct", new ColumnMetaData{Name="Signal Wait %",IsVisible=false } },
                {"SignalWaitMsPerSec", new ColumnMetaData{Name="Signal Wait (ms/sec)",IsVisible=true } },
                {"SignalWaitMsPerCorePerSec", new ColumnMetaData{Name="Signal Wait Time (ms/sec/core)",IsVisible=false } },
                {"SignalWaitSec", new ColumnMetaData{Name="Signal Wait (sec)",IsVisible=false } },
                {"TotalWaitSec", new ColumnMetaData{Name="Total Wait (sec)",IsVisible=false } },
                {"WaitTimeMsPerSec", new ColumnMetaData{Name="Wait Time (ms/sec)",IsVisible=true } },
                {"WaitTimeMsPerCorePerSec", new ColumnMetaData{Name="Wait Time (ms/sec/core)",IsVisible=false } },
                {"WaitingTasksCount", new ColumnMetaData{Name="Waiting Tasks Count",IsVisible=false } }
            };

        private void PopulateMetricsMenu()
        {
            foreach (var itm in columns)
            {
                tsMetrics.DropDownItems.Add(new ToolStripMenuItem(itm.Value.Name, null, TsMetrics_Click) { Tag = itm.Key, Checked = itm.Value.IsVisible, CheckOnClick = true });
            }
        }

        private void TsMetrics_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            columns[(string)ts.Tag].IsVisible = ts.Checked;
            WaitChart1.UpdateColumnVisibility(columns);
        }

        private void RefreshChart()
        {
            if (selectedWaitType == null || selectedWaitType == string.Empty)
            {
                splitContainer1.Panel1Collapsed = true;
            }
            else
            {
                tsWaitType.Text = selectedWaitType;
                splitContainer1.Panel1Collapsed = false;
                var dt = GetWaitsDT(selectedWaitType);
                WaitChart1.LegendLocation = LiveCharts.LegendLocation.Bottom;
                WaitChart1.Series.Clear();
                WaitChart1.AddDataTable(dt, columns, "time", true);
                WaitChart1.AxisX[0].MinValue = DateRange.FromUTC.ToAppTimeZone().Ticks;
                WaitChart1.AxisX[0].MaxValue = DateRange.ToUTC.ToAppTimeZone().Ticks;
                if (WaitChart1.AxisY.Count == 1)
                {
                    WaitChart1.AxisY[0].MinValue = 0;
                }
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            colHelp.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colHelp.Visible = true;
        }

        private void WaitsSummary_Load(object sender, EventArgs e)
        {
            dgv.ApplyTheme();
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroups_Click);
            PopulateColumnsMenu();
            PopulateMetricsMenu();
        }

        private void PopulateColumnsMenu()
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                var mnuCol = new ToolStripMenuItem { Text = col.HeaderText, Tag = col.Name, Checked = col.Visible, CheckOnClick = true };
                mnuCol.Click += MnuCol_Click;
                tsColumns.DropDownItems.Add(mnuCol);
            }
        }

        private void MnuCol_Click(object sender, EventArgs e)
        {
            var col = (ToolStripMenuItem)sender;
            dgv.Columns[(string)col.Tag].Visible = col.Checked;
        }

        private void TsDateGroups_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = (int)ts.Tag;
            RefreshChart();
        }

        private void TsSmooth_Click(object sender, EventArgs e)
        {
            WaitChart1.DefaultLineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
        }

        private void PointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            WaitChart1.SetPointSize(Convert.ToInt32(ts.Tag));
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }
}