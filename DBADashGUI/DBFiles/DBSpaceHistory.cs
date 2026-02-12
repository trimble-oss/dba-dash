using DBADashGUI.Charts;
using DBADashGUI.Theme;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.DBFiles
{
    public partial class DBSpaceHistory : UserControl
    {
        public DBSpaceHistory()
        {
            InitializeComponent();
            DateHelper.AddDateGroups(tsDateGroup, DateGroup_Click);
        }

        private void DateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGroupingMins = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = "Group:" + DateHelper.DateGroupString(DateGroupingMins);
            RefreshData();
        }

        private void AutoSetDateGroup()
        {
            DateGroupingMins = DateHelper.DateGrouping((int)tsDateRange.ActualTimeSpan.TotalMinutes, PointSize > 0 ? 100 : 365);
            tsDateGroup.Text = "Group:" + DateHelper.DateGroupString(DateGroupingMins);
        }

        private readonly string connectionString = Common.ConnectionString;

        private int _databaseID;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int DatabaseID
        {
            get => _databaseID;
            set
            {
                _databaseID = value;
                if (_databaseID > 0)
                {
                    PopulateFileGroupFilter();
                    PopulateFileFilesFilter();
                }
            }
        }

        private int DateGroupingMins;
        private double lineSmoothness = ChartConfiguration.DefaultLineSmoothness;
        private double geometrySize = ChartConfiguration.DefaultGeometrySize;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InstanceGroupName { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DBName { get; set; }

        private string _fileName;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; SetFileChecked(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string NumberFormat { get; set; } = "N1";

        private int? _dataSpaceId;
        private DataTable HistoryDT;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int? DataSpaceID
        {
            get => _dataSpaceId;
            set
            {
                _dataSpaceId = value;
                SetFGChecked();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SmoothLines
        {
            get => smoothLinesToolStripMenuItem.Checked;
            set
            {
                smoothLinesToolStripMenuItem.Checked = value;
                lineSmoothness = value ? 1 : 0;
            }
        }

        public string DateFormat => tsDateRange.ActualTimeSpan.DateFormatString();

        private DateTime From => tsDateRange.DateFromUtc;

        private DateTime To => tsDateRange.DateToUtc;

        private double PointSize
        {
            get => pointsToolStripMenuItem.Checked ? 10 : 0;
            set
            {
                geometrySize = value;
                pointsToolStripMenuItem.Checked = value > 0;
            }
        }

        private string _unit = "MB";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Unit
        {
            get => _unit;
            set
            {
                var checkedCount = 0;
                foreach (ToolStripMenuItem itm in tsUnits.DropDownItems)
                {
                    itm.Checked = value == Convert.ToString(itm.Tag);
                    if (itm.Checked)
                    {
                        checkedCount++;
                    }
                }
                if (checkedCount != 1)
                {
                    throw new Exception("Invalid Unit.  Select MB, GB, TB");
                }
                _unit = value;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.DataPropertyName.StartsWith("Size") || col.DataPropertyName.StartsWith("Used"))
                    {
                        col.Visible = col.DataPropertyName == "Size" + value || col.DataPropertyName == "Used" + value;
                    }
                }
            }
        }

        public void RefreshData()
        {
            HistoryDT = DBFileSnapshot();
            dgv.DataSource = HistoryDT;
            dgv.Sort(dgv.Columns[0], ListSortDirection.Descending);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            RefreshChart();
        }

        private void RefreshChart()
        {
            var cnt = HistoryDT.Rows.Count;
            if (cnt < 2)
            {
                chart1.Series = Array.Empty<ISeries>();
                return;
            }

            var columns = new Dictionary<string, ColumnMetaData>
            {
                {"Size" + Unit, new ColumnMetaData{Name="Size (" + Unit + ")",IsVisible=true, AxisName="Primary" } },
                {"Used" + Unit, new ColumnMetaData{Name="Used (" + Unit + ")",IsVisible=true, AxisName="Primary" } }
            };

            // Get visible columns
            var visibleColumns = columns.Where(c => c.Value.IsVisible).Select(c => c.Key).ToArray();

            if (visibleColumns.Length == 0)
            {
                chart1.Series = Array.Empty<ISeries>();
                return;
            }

            // Create series names dictionary
            var seriesNames = columns.ToDictionary(c => c.Key, c => c.Value.Name);

            // Get date range from data
            var dates = HistoryDT.Rows.Cast<DataRow>()
                .Select(r => ((DateTime)r["SnapshotDate"]).ToAppTimeZone())
                .ToList();
            var minDate = dates.Min();
            var maxDate = dates.Max();

            // Update chart using ChartHelper
            var config = new ChartConfiguration
            {
                DateColumn = "SnapshotDate",
                MetricColumns = visibleColumns,
                ChartType = ChartTypes.Line,
                ShowLegend = true,
                LegendPosition = LegendPosition.Bottom,
                LineSmoothness = lineSmoothness,
                GeometrySize = geometrySize,
                XAxisMin = minDate,
                XAxisMax = maxDate,
                SeriesNames = seriesNames,
                YAxisLabel = Unit,
                YAxisFormat = "#,##0.0",
                YAxisMin = 0
            };

            ChartHelper.UpdateChart(chart1, HistoryDT, config);
        }

        public DataTable DBFileSnapshot()
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.DBFileSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("FromDate", From);
            cmd.Parameters.AddWithValue("ToDate", To);
            cmd.Parameters.AddIfGreaterThanZero("DatabaseID", DatabaseID);
            cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", InstanceGroupName);
            cmd.Parameters.AddStringIfNotNullOrEmpty("DBName", DBName);
            cmd.Parameters.AddWithNullableValue("DataSpaceID", DataSpaceID);
            cmd.Parameters.AddWithValue("DateGroupingMins", DateGroupingMins);

            if (!string.IsNullOrEmpty(FileName))
            {
                cmd.Parameters.AddWithValue("FileName", FileName);
            }
            DataTable dt = new();
            da.Fill(dt);

            dt.Columns.Add("SizeGB", typeof(decimal));
            dt.Columns.Add("UsedGB", typeof(decimal));
            dt.Columns.Add("SizeTB", typeof(decimal));
            dt.Columns.Add("UsedTB", typeof(decimal));
            foreach (DataRow row in dt.Rows)
            {
                if (row["SizeMB"] != DBNull.Value)
                {
                    row["SizeGB"] = Convert.ToDecimal(row["SizeMB"]) / 1024;
                    row["SizeTB"] = Convert.ToDecimal(row["SizeGB"]) / 1024;
                }
                if (row["UsedMB"] != DBNull.Value)
                {
                    row["UsedGB"] = Convert.ToDecimal(row["UsedMB"]) / 1024;
                    row["UsedTB"] = Convert.ToDecimal(row["UsedGB"]) / 1024;
                }
            }

            return dt;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void SmoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lineSmoothness = SmoothLines ? 1 : 0;
            RefreshChart();
        }

        private void PointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            geometrySize = pointsToolStripMenuItem.Checked ? 10 : 0;
            RefreshChart();
        }

        private void DBSpaceHistory_Load(object sender, EventArgs e)
        {
            AddDgvColumns();
            AutoSetDateGroup();
        }

        private void AddDgvColumns()
        {
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(new DataGridViewTextBoxColumn() { HeaderText = "Snapshot Date", DataPropertyName = "SnapshotDate" },
                new DataGridViewTextBoxColumn() { HeaderText = "Size (MB)", DataPropertyName = "SizeMB", DefaultCellStyle = Common.DataGridViewCellStyle(NumberFormat), Visible = Unit == "MB" },
                new DataGridViewTextBoxColumn() { HeaderText = "Used (MB)", DataPropertyName = "UsedMB", DefaultCellStyle = Common.DataGridViewCellStyle(NumberFormat), Visible = Unit == "MB" },
                new DataGridViewTextBoxColumn() { HeaderText = "Size (GB)", DataPropertyName = "SizeGB", DefaultCellStyle = Common.DataGridViewCellStyle(NumberFormat), Visible = Unit == "GB" },
                new DataGridViewTextBoxColumn() { HeaderText = "Used (GB)", DataPropertyName = "UsedGB", DefaultCellStyle = Common.DataGridViewCellStyle(NumberFormat), Visible = Unit == "GB" },
                new DataGridViewTextBoxColumn() { HeaderText = "Size (TB)", DataPropertyName = "SizeTB", DefaultCellStyle = Common.DataGridViewCellStyle(NumberFormat), Visible = Unit == "TB" },
                new DataGridViewTextBoxColumn() { HeaderText = "Used (TB)", DataPropertyName = "UsedTB", DefaultCellStyle = Common.DataGridViewCellStyle(NumberFormat), Visible = Unit == "TB" });
        }

        private void PopulateFileGroupFilter()
        {
            var dt = CommonData.GetFileGroups(DatabaseID);
            foreach (DataRow r in dt.Rows)
            {
                var fg = r["FileGroup"] == DBNull.Value ? "{NULL}" : (string)r["FileGroup"];
                var dataSpaceId = r["data_space_id"] == DBNull.Value ? null : (int?)r["data_space_id"];
                if (dataSpaceId == null) continue;
                var mnu = new ToolStripMenuItem(fg)
                {
                    Tag = dataSpaceId,
                    Checked = dataSpaceId == DataSpaceID
                };
                mnu.Click += Mnu_Click;
                tsFileGroup.DropDownItems.Add(mnu);
            }
            tsFileGroup.Visible = tsFileGroup.DropDownItems.Count > 0;
        }

        private void PopulateFileFilesFilter()
        {
            var dt = CommonData.GetFiles(DatabaseID);
            foreach (DataRow r in dt.Rows)
            {
                var fileName = r["file_name"] == DBNull.Value ? "" : (string)r["file_name"];
                if (fileName.Length <= 0) continue;
                var mnu = new ToolStripMenuItem(fileName)
                {
                    Checked = fileName == FileName
                };
                mnu.Click += MnuFile_Click;
                tsFile.DropDownItems.Add(mnu);
            }
            tsFile.Visible = tsFile.DropDownItems.Count > 0;
        }

        private void MnuFile_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            FileName = mnu.Checked ? "" : mnu.Text;
            SetFileChecked();
            RefreshData();
        }

        private void SetFileChecked()
        {
            tsFile.Text = FileName is null or "" ? "{All Files}" : FileName;
            tsFile.Font = FileName is null or "" ? new Font(tsFile.Font, FontStyle.Regular) : new Font(tsFile.Font, FontStyle.Bold);
            foreach (ToolStripMenuItem mnu in tsFile.DropDownItems)
            {
                mnu.Checked = mnu.Text == FileName;
                if (mnu.Checked) { tsFile.Text = mnu.Text; }

                mnu.Font = mnu.Checked ? new Font(mnu.Font, FontStyle.Bold) : new Font(mnu.Font, FontStyle.Regular);
            }
        }

        private void Mnu_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            if (mnu.Checked)
            {
                DataSpaceID = null;
            }
            else
            {
                DataSpaceID = (int?)mnu.Tag;
            }
            SetFGChecked();
            RefreshData();
        }

        private void SetFGChecked()
        {
            tsFileGroup.Text = DataSpaceID == null ? "{All FileGroups}" : DataSpaceID.ToString();
            tsFileGroup.Font = DataSpaceID == null ? new Font(tsFileGroup.Font, FontStyle.Regular) : new Font(tsFileGroup.Font, FontStyle.Bold);
            foreach (ToolStripMenuItem mnu in tsFileGroup.DropDownItems)
            {
                mnu.Checked = (int?)mnu.Tag == DataSpaceID;
                if (mnu.Checked) { tsFileGroup.Text = mnu.Text; }

                mnu.Font = mnu.Checked ? new Font(mnu.Font, FontStyle.Bold) : new Font(mnu.Font, FontStyle.Regular);
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(dgv);
        }

        private void TsGrid_Click(object sender, EventArgs e)
        {
            splitContainer1.TogglePanels();
            tsGrid.Image = splitContainer1.Panel1Collapsed ? Properties.Resources.LineChart_16x : Properties.Resources.Table_16x;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            SetPanelSize();
        }

        private void SetPanelSize()
        {
            if (!splitContainer1.Panel2Collapsed)
            {
                var distance = Width - (ColumnTotalWidth() + 30);
                if (distance > 10)
                {
                    splitContainer1.SplitterDistance = Width - (ColumnTotalWidth() + 30);
                }
            }
        }

        private int ColumnTotalWidth()
        {
            return dgv.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).Select(x => x.Width).Sum();
        }

        private void DBSpaceHistory_Resize(object sender, EventArgs e)
        {
            SetPanelSize();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void SetUnit(object sender, EventArgs e)
        {
            var selectedItem = (ToolStripMenuItem)sender;
            Unit = Convert.ToString(selectedItem.Tag);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            SetPanelSize();
            RefreshChart();
        }

        private void DateRangeChanged(object sender, EventArgs e)
        {
            AutoSetDateGroup();
            RefreshData();
        }
    }
}