using DBADashGUI.Charts;
using DBADashGUI.Performance;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Drives
{
    public partial class DriveHistory : UserControl
    {
        public DriveHistory()
        {
            InitializeComponent();
            lblInsufficientData.BackColor = DashColors.Warning;
            lblInsufficientData.ForeColor = Color.White;
            dgv.Columns.Clear();
            var cols = new DataGridViewColumn[] {
                    new DataGridViewTextBoxColumn { HeaderText = "Date", DataPropertyName = "SnapshotDate" },
                    new DataGridViewTextBoxColumn { HeaderText = "Size (GB)", DataPropertyName = "SizeGB", DefaultCellStyle = Common.DataGridViewNumericCellStyle  },
                    new DataGridViewTextBoxColumn { HeaderText = "Used (GB)", DataPropertyName = "UsedGB", DefaultCellStyle = Common.DataGridViewNumericCellStyle  },
                    new DataGridViewTextBoxColumn { HeaderText = "Free (GB)", DataPropertyName = "FreeGB", DefaultCellStyle = Common.DataGridViewNumericCellStyle  },
                    new DataGridViewTextBoxColumn { HeaderText = "Free (%)", DataPropertyName = "FreePct", DefaultCellStyle = Common.DataGridViewPercentCellStyle }
            };
            dgv.Columns.AddRange(cols);
            dgv.AutoGenerateColumns = false;
            dgv.RegisterClearFilter(tsClearFilter);
        }

        private readonly string connectionString = Common.ConnectionString;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int DriveID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool SmoothLines
        {
            get => smoothLinesToolStripMenuItem.Checked; set => smoothLinesToolStripMenuItem.Checked = value;
        }

        public string DateFormat => DateGroupingMins < 1440 ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd";

        private DateTime From => tsDateRange.DateFromUtc;

        private DateTime To => tsDateRange.DateToUtc;

        private double PointSize => pointsToolStripMenuItem.Checked ? ChartConfiguration.DefaultGeometrySize : 0;

        private int DateGroupingMins
        {
            get
            {
                var mins = Convert.ToInt32(To.Subtract(From).TotalMinutes);
                return DateHelper.DateGrouping(mins, 400);
            }
        }

        private DataTable driveSnapshotDT;

        private void DisplayInsufficientData()
        {
            lblInsufficientData.Visible = true;
            chart1.Visible = false;
            dgv.Visible = false;
        }

        public void RefreshData()
        {
            ToggleGrid(false);
            driveSnapshotDT = DriveSnapshot();
            dgv.DataSource = new DataView(driveSnapshotDT);
            var cnt = driveSnapshotDT.Rows.Count;
            if (cnt < 2)
            {
                DisplayInsufficientData();
                return;
            }

            // Configure chart using ChartHelper with MetricColumns
            // Order matters: Size first (larger values), then Used (smaller values) to minimize visual overlap
            var config = new ChartConfiguration
            {
                DateColumn = "SnapshotDate",
                MetricColumns = new[] { "SizeGB", "UsedGB" }, // Size first to draw behind
                YAxisLabel = "GB",
                YAxisFormat = "0.0",
                ChartType = ChartTypes.Line,
                ShowLegend = true,
                LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom,
                XAxisMin = From,
                XAxisMax = To
            };

            ChartHelper.UpdateChart(chart1, driveSnapshotDT, config);

            // Apply customizations after ChartHelper creates the chart
            ApplyChartCustomizations();
        }

        /// <summary>
        /// Apply customizations to the chart after ChartHelper has configured it
        /// </summary>
        private void ApplyChartCustomizations()
        {
            if (chart1.Series == null) return;

            foreach (var series in chart1.Series.OfType<LineSeries<DateTimePoint>>())
            {
                series.LineSmoothness = SmoothLines ? ChartConfiguration.DefaultLineSmoothness : 0;
                series.GeometrySize = PointSize;
            }
        }

        public DataTable DriveSnapshot()
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DriveSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("FromDate", From);
            cmd.Parameters.AddWithValue("ToDate", To);
            cmd.Parameters.AddWithValue("DriveID", DriveID);
            cmd.Parameters.AddWithValue("DateGroupingMins", DateGroupingMins);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void SmoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ApplyChartCustomizations();
        }

        private void PointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ApplyChartCustomizations();
        }

        private void TsChart_Click(object sender, EventArgs e)
        {
            ToggleGrid(false);
        }

        private void ToggleGrid(bool gridVisible)
        {
            lblInsufficientData.Visible = false;
            chart1.Visible = !gridVisible;
            dgv.Visible = gridVisible;
            tsChart.Visible = gridVisible;
            tsGrid.Visible = !gridVisible;
        }

        private void TsGrid_Click(object sender, EventArgs e)
        {
            ToggleGrid(true);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }

        private void DateRangeChanged(object sender, EventArgs e)
        {
            RefreshData();
        }
    }
}