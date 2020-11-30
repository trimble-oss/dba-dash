using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using LiveCharts.Defaults;
using LiveCharts;
using LiveCharts.Wpf;
using DBAChecksGUI.Performance;

namespace DBAChecksGUI.DBFiles
{
    public partial class DBSpaceHistory : UserControl
    {
        public DBSpaceHistory()
        {
            InitializeComponent();
        }

        string connectionString = Common.ConnectionString;

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
                    populateFileGroupFilter();
                    populateFileFilesFilter();
                }
            }
        }

        public string Instance { get; set; }
        public string DBName { get; set; }
        private string _fileName;
        public string FileName { get { return _fileName; } set { _fileName = value; setFileChecked(); } }

        private Int32? _dataspaceid=null;
     
        public Int32? DataSpaceID
        {
            get
            {
                return _dataspaceid;
            }
            set
            {
                _dataspaceid = value;
                setFGChecked();
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

        Int32 pointSize
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
            var dt = DriveSnapshot();
            var cnt =dt.Rows.Count;
            if (dt.Rows.Count < 2)
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
            foreach (DataRow r in dt.Rows)
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
                    PointGeometrySize = pointSize, 
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

        public DataTable DriveSnapshot()
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DBFileSnapshot_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("FromDate", From);
                cmd.Parameters.AddWithValue("ToDate", To);
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                if (Instance!=null && Instance.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Instance", Instance);
                }
                if (DBName !=null && DBName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("DBName", DBName);
                }
                if (DataSpaceID !=null)
                {
                    cmd.Parameters.AddWithValue("DataSpaceID", DataSpaceID);
                }
                if(FileName!=null && FileName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("FileName", FileName);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void Days_Click(object sender, EventArgs e)
        {
            Days = Int32.Parse((string)((ToolStripMenuItem)sender).Tag);
            setTimeChecked();
            RefreshData();
        }

        private void setTimeChecked()
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
            var frm = new CustomTimePicker();
            frm.FromDate = From;
            frm.ToDate = To;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                customFrom = frm.FromDate;
                customTo = frm.ToDate;
                Days = -1;
                setTimeChecked();
                RefreshData();
            }

        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void smoothLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach(LineSeries s in chart1.Series)
            {
                s.LineSmoothness = SmoothLines ? 1 : 0;
            }
        }

        private void pointsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (LineSeries s in chart1.Series)
            {
                s.PointGeometrySize = pointSize;
            }
        }

        private void DBSpaceHistory_Load(object sender, EventArgs e)
        {

        }

        private void populateFileGroupFilter()
        {
            var dt = Common.GetFileGroups(DatabaseID);
            foreach(DataRow r in dt.Rows)
            {
                string fg = r["FileGroup"] == DBNull.Value ? "{NULL}" : (string)r["FileGroup"];
                Int32? dataspaceid = r["data_space_id"] == DBNull.Value ? null : (Int32?)r["data_space_id"];
                if (dataspaceid != null) {
                    var mnu = new ToolStripMenuItem(fg);
                    mnu.Tag = dataspaceid;
                    mnu.Checked = dataspaceid == DataSpaceID;
                    mnu.Click += Mnu_Click;
                    tsFileGroup.DropDownItems.Add(mnu);
                }
            }
            tsFileGroup.Visible = tsFileGroup.DropDownItems.Count>0;
        }

        private void populateFileFilesFilter()
        {
            var dt = Common.GetFiles(DatabaseID);
            foreach (DataRow r in dt.Rows)
            {
                string fileName = r["file_name"] == DBNull.Value ? "" : (string)r["file_name"];
                if (fileName.Length>0)
                {
                    var mnu = new ToolStripMenuItem(fileName);
                    mnu.Checked = fileName ==FileName;
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
            setFileChecked();
            RefreshData();
        }

        private void setFileChecked()
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
            setFGChecked();
            RefreshData();
        }

        private void setFGChecked()
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


    }
}
