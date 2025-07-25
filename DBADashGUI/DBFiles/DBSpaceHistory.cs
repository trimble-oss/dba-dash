using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
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

        public string InstanceGroupName { get; set; }
        public string DBName { get; set; }
        private string _fileName;

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; SetFileChecked(); }
        }

        public string NumberFormat { get; set; } = "N1";

        private int? _dataSpaceId;
        private DataTable HistoryDT;

        public int? DataSpaceID
        {
            get => _dataSpaceId;
            set
            {
                _dataSpaceId = value;
                SetFGChecked();
            }
        }

        public bool SmoothLines
        {
            get => smoothLinesToolStripMenuItem.Checked; set => smoothLinesToolStripMenuItem.Checked = value;
        }

        public string DateFormat => tsDateRange.ActualTimeSpan.DateFormatString();

        private DateTime From => tsDateRange.DateFromUtc;

        private DateTime To => tsDateRange.DateToUtc;

        private int PointSize => pointsToolStripMenuItem.Checked ? 10 : 0;

        private string _unit = "MB";

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
                chart1.Series = null;
                return;
            }
            var columns = new Dictionary<string, ColumnMetaData>
            {
                {"Size" + Unit, new ColumnMetaData{Name="Size (" + Unit + ")",IsVisible=true } },
                {"Used" + Unit, new ColumnMetaData{Name="Used (" + Unit + ")",IsVisible=true } }
            };

            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            var i = 0;
            foreach (DataRow r in HistoryDT.Rows)
            {
                foreach (var s in columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : (double)(decimal)r[s];
                    var ssDate = (DateTime)r["SnapshotDate"];
                    columns[s].Points[i] = new DateTimePoint(ssDate.ToAppTimeZone(), v);
                }
                i++;
            }

            var sc = new SeriesCollection();
            chart1.Series = sc;
            foreach (var s in columns.Keys)
            {
                var v = new ChartValues<DateTimePoint>();
                v.AddRange(columns[s].Points);
                sc.Add(new LineSeries
                {
                    Title = columns[s].Name,
                    Tag = s,
                    ScalesYAt = columns[s].axis,
                    LineSmoothness = SmoothLines ? 1 : 0,
                    PointGeometrySize = PointSize,
                    Values = v
                }
                );
            }
            chart1.AxisX.Clear();
            chart1.AxisY.Clear();
            chart1.AxisX.Add(new Axis
            {
                Title = "Time",
                LabelFormatter = val => new DateTime((long)val).ToString(DateFormat)
            });
            chart1.AxisY.Add(new Axis
            {
                Title = Unit,
                LabelFormatter = val => val.ToString("#,##0.0 " + Unit),
                MinValue = 0
            });
            chart1.LegendLocation = LegendLocation.Bottom;
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
            foreach (var s in chart1.Series.Cast<LineSeries>())
            {
                s.LineSmoothness = SmoothLines ? 1 : 0;
            }
        }

        private void PointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var s in chart1.Series.Cast<LineSeries>())
            {
                s.PointGeometrySize = PointSize;
            }
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