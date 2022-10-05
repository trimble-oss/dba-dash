﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using LiveCharts.Defaults;
using LiveCharts;
using LiveCharts.Wpf;
using DBADashGUI.Performance;

namespace DBADashGUI.DBFiles
{
    public partial class DBSpaceHistory : UserControl
    {
        public DBSpaceHistory()
        {
            InitializeComponent();
        }

        readonly string connectionString = Common.ConnectionString;

        private Int32 _databaseID;
        public Int32 DatabaseID
        {
            get
            {
                return _databaseID;
            }
            set
            {
                _databaseID = value;
                if (_databaseID >0)
                {
                    PopulateFileGroupFilter();
                    PopulateFileFilesFilter();
                }
            }
        }

        public string InstanceGroupName { get; set; }
        public string DBName { get; set; }
        private string _fileName;
        public string FileName { get { return _fileName; } set { _fileName = value; SetFileChecked(); } }

        private Int32? _dataspaceid=null;
        DataTable HistoryDT;
     
        public Int32? DataSpaceID
        {
            get
            {
                return _dataspaceid;
            }
            set
            {
                _dataspaceid = value;
                SetFGChecked();
            }
        }

        public bool SmoothLines {
            get {
                return smoothLinesToolStripMenuItem.Checked;
            }
            set
            {
                smoothLinesToolStripMenuItem.Checked = value;
            }
        }

        public string DateFormat = "yyyy-MM-dd";

        Int32 Days = 90;

        DateTime customFrom;
        DateTime customTo;

        DateTime From
        {
            get
            {
                if (Days > 0)
                {
                    return DateTime.UtcNow.Date.AddDays(-Days);
                }
                else
                {
                    return customFrom;
                }
            }
        }

        DateTime To
        {
            get
            {
                if (Days > 0)
                {
                    return DateTime.UtcNow.Date.AddDays(1);
                }
                else
                {
                    return customTo;
                }
            }
        }

        Int32 PointSize
        {
            get
            {
                if (pointsToolStripMenuItem.Checked)
                {
                    return 10;
                }
                else
                {
                    return 0;
                }
            }
        }

        public void RefreshData()
        {
            HistoryDT = DBFileSnapshot();
            var cnt =HistoryDT.Rows.Count;
            dgv.DataSource = HistoryDT;
            dgv.Sort(dgv.Columns[0], ListSortDirection.Descending);

            if (HistoryDT.Rows.Count < 2)
            {
                return;
            }
            var columns = new Dictionary<string, columnMetaData>
            {
                {"SizeMB", new columnMetaData{Alias="Size (MB)",isVisible=true } },
                {"UsedMB", new columnMetaData{Alias="Used (MB)",isVisible=true } }
            };


            foreach (var s in columns.Keys)
            {
                columns[s].Points = new DateTimePoint[cnt];
            }

            Int32 i = 0;
            foreach (DataRow r in HistoryDT.Rows)
            {
                foreach (string s in columns.Keys)
                {
                    var v = r[s] == DBNull.Value ? 0 : (double)(decimal)r[s];
                    var ssDate = (DateTime)r["SnapshotDate"];
                    columns[s].Points[i] = new DateTimePoint(ssDate.ToLocalTime(), v);
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
                    Values=v
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
                Title = "MB",
                LabelFormatter = val => val.ToString("#,##0.0 MB"),
                MinValue = 0
            });
            chart1.LegendLocation = LegendLocation.Bottom;

        }

        public DataTable DBFileSnapshot()
        {
            using (var cn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("dbo.DBFileSnapshot_Get", cn) { CommandType = CommandType.StoredProcedure })
            using(var da = new SqlDataAdapter(cmd)){
                cn.Open();
                cmd.Parameters.AddWithValue("FromDate", From);
                cmd.Parameters.AddWithValue("ToDate", To);
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                if (InstanceGroupName != null && InstanceGroupName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceGroupName", InstanceGroupName);
                }
                if (DBName != null && DBName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DBName", DBName);
                }
                if (DataSpaceID != null)
                {
                    cmd.Parameters.AddWithValue("DataSpaceID", DataSpaceID);
                }
                if (FileName != null && FileName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("FileName", FileName);
                }
                DataTable dt = new();
                da.Fill(dt);
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
                if(ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var itm = (ToolStripMenuItem)ts;
                    itm.Checked = (string)itm.Tag == Days.ToString();
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
            frm.ShowDialog();
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

        private void DBSpaceHistory_Load(object sender, EventArgs e)
        {
            AddDgvColumns();
        }

        private void AddDgvColumns()
        {
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn(){ HeaderText="Snapshot Date", DataPropertyName="SnapshotDate"},
                new DataGridViewTextBoxColumn(){ HeaderText="Size (MB)", DataPropertyName="SizeMB", DefaultCellStyle= Common.DataGridViewNumericCellStyleNoDigits},
                new DataGridViewTextBoxColumn(){ HeaderText="Used (MB)", DataPropertyName="UsedMB", DefaultCellStyle= Common.DataGridViewNumericCellStyleNoDigits}
            });
        }

        private void PopulateFileGroupFilter()
        {
            var dt = CommonData.GetFileGroups(DatabaseID);
            foreach(DataRow r in dt.Rows)
            {
                string fg = r["FileGroup"] == DBNull.Value ? "{NULL}" : (string)r["FileGroup"];
                Int32? dataspaceid = r["data_space_id"] == DBNull.Value ? null : (Int32?)r["data_space_id"];
                if (dataspaceid != null) {
                    var mnu = new ToolStripMenuItem(fg)
                    {
                        Tag = dataspaceid,
                        Checked = dataspaceid == DataSpaceID
                    };
                    mnu.Click += Mnu_Click;
                    tsFileGroup.DropDownItems.Add(mnu);
                }
            }
            tsFileGroup.Visible = tsFileGroup.DropDownItems.Count>0;
        }

        private void PopulateFileFilesFilter()
        {
            var dt = CommonData.GetFiles(DatabaseID);
            foreach (DataRow r in dt.Rows)
            {
                string fileName = r["file_name"] == DBNull.Value ? "" : (string)r["file_name"];
                if (fileName.Length>0)
                {
                    var mnu = new ToolStripMenuItem(fileName)
                    {
                        Checked = fileName == FileName
                    };
                    mnu.Click += MnuFile_Click;
                    tsFile.DropDownItems.Add(mnu);
                }
            }
            tsFile.Visible = tsFile.DropDownItems.Count > 0;
        }

        private void MnuFile_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            if (mnu.Checked)
            {
                FileName = "";
            }
            else
            {
                FileName = mnu.Text;
            }
            SetFileChecked();
            RefreshData();
        }

        private void SetFileChecked()
        {
            tsFile.Text = FileName ==null || FileName == "" ? "{All Files}" : FileName;
            tsFile.Font = FileName == null || FileName == "" ? new Font(tsFile.Font, FontStyle.Regular) : new Font(tsFile.Font, FontStyle.Bold);
            foreach (ToolStripMenuItem mnu in tsFile.DropDownItems)
            {
                mnu.Checked = mnu.Text== FileName;
                if (mnu.Checked) { tsFile.Text = mnu.Text; };
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
                DataSpaceID = (Int32?)mnu.Tag;
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
                mnu.Checked = (Int32?)mnu.Tag == DataSpaceID;
                if (mnu.Checked) { tsFileGroup.Text = mnu.Text; };
                mnu.Font = mnu.Checked ? new Font(mnu.Font, FontStyle.Bold) : new Font(mnu.Font, FontStyle.Regular);
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(dgv);
        }

        private void TsGrid_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
            SetPanelSize();
        }

        private void SetPanelSize()
        {
            if (!splitContainer1.Panel2Collapsed)
            {
                int distance = this.Width - (ColumnTotalWidth() + 30);
                if (distance > 10)
                {
                    splitContainer1.SplitterDistance = this.Width - (ColumnTotalWidth() + 30);
                }
            }
        }

        private int ColumnTotalWidth()
        {
            return dgv.Columns.Cast<DataGridViewColumn>().Select(x => x.Width).Sum();
        }

        private void DBSpaceHistory_Resize(object sender, EventArgs e)
        {
            SetPanelSize();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }
    }
}
