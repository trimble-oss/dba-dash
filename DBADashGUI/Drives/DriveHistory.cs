using DBADashGUI.Performance;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
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
            SetTimeChecked();
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
        }

        private readonly string connectionString = Common.ConnectionString;

        public Int32 DriveID { get; set; }

        public bool SmoothLines
        {
            get => smoothLinesToolStripMenuItem.Checked; set => smoothLinesToolStripMenuItem.Checked = value;
        }

        public string DateFormat => DateGroupingMins < 1440 ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd";

        private Int32 Days = 7;
        private DateTime customFrom;
        private DateTime customTo;

        private DateTime From => Days > 0
                    ? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddDays(-Days)
                    : customFrom;

        private DateTime To => Days > 0
                    ? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddHours(1)
                    : customTo;

        private Int32 PointSize => pointsToolStripMenuItem.Checked ? 10 : 0;

        private Int32 DateGroupingMins
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
            dgv.DataSource = driveSnapshotDT;
            var cnt = driveSnapshotDT.Rows.Count;
            if (cnt < 2)
            {
                DisplayInsufficientData();
                return;
            }
            var columns = new Dictionary<string, ColumnMetaData>
            {
                {"SizeGB", new ColumnMetaData{Alias="Size (GB)",isVisible=true } },
                {"UsedGB", new ColumnMetaData{Alias="Used (GB)",isVisible=true } }
            };

            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            Int32 i = 0;
            foreach (DataRow r in driveSnapshotDT.Rows)
            {
                foreach (string s in columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : (double)(decimal)r[s];
                    var ssDate = (DateTime)r["SnapshotDate"];
                    columns[s].Points[i] = new DateTimePoint(ssDate, v);
                }
                i++;
            }

            var sc = new SeriesCollection();
            chart1.Series = sc;
            foreach (string s in columns.Keys)
            {
                var v = new ChartValues<DateTimePoint>();
                v.AddRange(columns[s].Points);
                sc.Add(new LineSeries
                {
                    Title = columns[s].Alias,
                    Tag = s,
                    ScalesYAt = columns[s].axis,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    PointGeometrySize = PointSize,
                    Values = v
                }
                ); ;
            }
            chart1.AxisX.Clear();
            chart1.AxisY.Clear();
            chart1.AxisX.Add(new Axis
            {
                Title = "Time",
                LabelFormatter = val => new System.DateTime((long)val).ToString(DateFormat)
            });
            chart1.AxisY.Add(new Axis
            {
                Title = "GB",
                LabelFormatter = val => val.ToString("0.0 GB"),
                MinValue = 0
            });
            chart1.LegendLocation = LegendLocation.Bottom;
        }

        public DataTable DriveSnapshot()
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("dbo.DriveSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
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
        }

        private void Days_Click(object sender, EventArgs e)
        {
            Days = Int32.Parse((string)((ToolStripMenuItem)sender).Tag);
            SetTimeChecked();
            RefreshData();
        }

        private void SetTimeChecked()
        {
            foreach (ToolStripItem ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var itm = (ToolStripMenuItem)ts;
                    itm.Checked = (string)itm.Tag == Days.ToString();
                    if (itm.Checked)
                    {
                        tsTime.Text = itm.Text;
                    }
                }
            }
        }

        private void Custom_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker
            {
                FromDate = From,
                ToDate = To
            };
            frm.ShowDialog(this);
            if (frm.DialogResult == DialogResult.OK)
            {
                customFrom = frm.FromDate;
                customTo = frm.ToDate;
                Days = -1;
                SetTimeChecked();
                RefreshData();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void SmoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (LineSeries s in chart1.Series.Cast<LineSeries>())
            {
                s.LineSmoothness = SmoothLines ? 1 : 0;
            }
        }

        private void PointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (LineSeries s in chart1.Series.Cast<LineSeries>())
            {
                s.PointGeometrySize = PointSize;
            }
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
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }
    }
}