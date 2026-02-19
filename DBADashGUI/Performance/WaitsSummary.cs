using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class WaitsSummary : UserControl, ISetContext, IRefreshData
    {
        public WaitsSummary()
        {
            InitializeComponent();
            dgv.RegisterClearFilter(tsClearFilter);
            waits1.CloseVisible = true;
            splitGrid.Panel1Collapsed = true;
        }

        private int InstanceID { get; set; }
        private string selectedWaitType;

        private int dateGrouping = 1;
        private int mins;
        private double lineSmoothness = ChartConfiguration.DefaultLineSmoothness;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int DateGrouping
        {
            get => dateGrouping;
            set
            {
                dateGrouping = value;
                tsDateGroup.Text = DateHelper.DateGroupString(value);
            }
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
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
            dgv.DataSource = new DataView(dt);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCellsExceptHeader);
            RefreshChart();
        }

        public DataTable GetWaitsSummaryDT()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.WaitsSummary_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
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

        public DataTable GetWaitsDT(string waitType)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.Waits_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
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
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
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
            columns[((string)ts.Tag)!].IsVisible = ts.Checked;
            RefreshChart();
        }

        private void RefreshChart()
        {
            if (splitGrid.Panel1Collapsed && string.IsNullOrEmpty(selectedWaitType))
            {
                return;
            }
            splitChart.Panel1Collapsed = string.IsNullOrEmpty(selectedWaitType);

            if (!splitChart.Panel2Collapsed)
            {
                waits1.RefreshData(InstanceID);
            }
            if (!splitChart.Panel1Collapsed)
            {
                tsWaitType.Text = selectedWaitType;
                splitGrid.Panel1Collapsed = false;
                var dt = GetWaitsDT(selectedWaitType);

                if (dt == null || dt.Rows.Count == 0)
                {
                    WaitChart1.Series = Array.Empty<ISeries>();
                    return;
                }

                // Get visible columns
                var visibleColumns = columns.Where(c => c.Value.IsVisible).Select(c => c.Key).ToArray();

                if (visibleColumns.Length == 0)
                {
                    WaitChart1.Series = Array.Empty<ISeries>();
                    return;
                }

                // Create series names dictionary
                var seriesNames = columns.ToDictionary(c => c.Key, c => c.Value.Name);

                // Update chart using ChartHelper
                var config = new ChartConfiguration
                {
                    DateColumn = "time",
                    MetricColumns = visibleColumns,
                    ChartType = ChartTypes.Line,
                    ShowLegend = true,
                    LegendPosition = LegendPosition.Bottom,
                    LineSmoothness = lineSmoothness,
                    GeometrySize = geometrySize,
                    XAxisMin = DateRange.FromUTC.ToAppTimeZone(),
                    XAxisMax = DateRange.ToUTC.ToAppTimeZone(),
                    SeriesNames = seriesNames,
                    YAxisLabel = string.Empty,
                    YAxisFormat = "0.0",
                    YAxisMin = 0
                };

                ChartHelper.UpdateChart(WaitChart1, dt, config);
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            colHelp.Visible = false;
            dgv.CopyGrid();
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
            lineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
            RefreshChart();
        }

        private void PointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            geometrySize = Convert.ToInt32(ts.Tag);
            RefreshChart();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void ToggleBarChart_Click(object sender, EventArgs e)
        {
            splitChart.Panel2Collapsed = !splitChart.Panel2Collapsed;
            splitGrid.Panel1Collapsed = splitChart.Panel2Collapsed && string.IsNullOrEmpty(selectedWaitType);
            RefreshChart();
        }

        private void CloseLineChart_Click(object sender, EventArgs e)
        {
            selectedWaitType = string.Empty;
            splitGrid.Panel1Collapsed = splitChart.Panel2Collapsed;
            RefreshChart();
        }
    }
}