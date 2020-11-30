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
using LiveCharts.Wpf;
using LiveCharts;
using LiveCharts.Defaults;
using static DBAChecksGUI.Performance.Performance;

namespace DBAChecksGUI.Performance
{
    public partial class IOPerformance : UserControl
    {
        public IOPerformance()
        {
            InitializeComponent();
        }

        Int32 mins;
        DateTime ioTime = DateTime.MinValue;
        DateTime from;
        DateTime to;
        string connectionString;
        Int32 instanceID;
        private Int32 dateGrouping;
        bool smoothLines = true;
        Int32 databaseid=0;
        string drive="";
        public Int32 PointSize;
        string filegroup="";
        public string FileGroup
        {
            get
            {
                return filegroup;
            }
            set
            {
                filegroup = value;
                tsFileGroup.Text = filegroup.Length > 0 ? filegroup : "Filegroup";
                var style = filegroup.Length > 0 ? FontStyle.Bold : FontStyle.Regular;
                tsFileGroup.Font = new Font(tsFileGroup.Font, style);
                foreach (ToolStripMenuItem itm in tsFileGroup.DropDownItems)
                {
                    style = itm.Text == filegroup ? FontStyle.Bold : FontStyle.Regular;
                    itm.Font = new Font(itm.Font, style);
                    itm.Checked = itm.Text == filegroup;                  
                }
                if (drive != "")
                {
                    Drive = "";
                }
            }
        }

        public string Drive
        {
            get
            {
                return drive;
            }
            set
            {
                tsDrives.Text = value;
                drive = value;
                if( filegroup != "")
                {
                    FileGroup = "";
                }
            }
        }

        public bool SmoothLines { 
            get {
                return smoothLines;
            }
            set {
                smoothLines = value;
                foreach(LineSeries s in chartIO.Series)
                {
                    s.LineSmoothness = smoothLines ? 1 : 0;
                }
            }
        }

        private void populateFileGroupFilter()
        {
            if (databaseid > 0)
            {
                tsFileGroup.DropDownItems.Clear();
                var dt = Common.GetFileGroups(databaseid);
                foreach (DataRow r in dt.Rows)
                {
                    string fg = r["FileGroup"] == DBNull.Value ? "{NULL}" : (string)r["FileGroup"];

                    var mnu = new ToolStripMenuItem(fg);
                    mnu.Checked = fg == filegroup;
                    mnu.CheckOnClick = true;
                    mnu.Click += filegroup_Click; ;
                    tsFileGroup.DropDownItems.Add(mnu);

                }
                tsFileGroup.Visible = tsFileGroup.DropDownItems.Count > 0;
            }
            else
            {
                tsFileGroup.Visible = false;
            }
        }

        private void filegroup_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            if (mnu.Checked)
            {   
                FileGroup = mnu.Text;
            }
            else
            {
                FileGroup = "";
            }
            refreshData();
        }

        private void getDrives()
        {
            tsDrives.DropDownItems.Clear();
            drive = "";
            tsDrives.Text = drive;
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("DriveLetters_Get", cn);
                cmd.Parameters.AddWithValue("@InstanceID", instanceID);
                cmd.CommandType = CommandType.StoredProcedure;
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    var name = (string)rdr["Name"];
                    var label = rdr["Label"] == DBNull.Value ? "" : (string)rdr["Label"];
                    var ddDrive = tsDrives.DropDownItems.Add(name + "     |     " + label);
                    ddDrive.Tag = name;
                    ddDrive.Click += DdDrive_Click;
                }
            }
            var ddAll = tsDrives.DropDownItems.Add("{All}");
            ddAll.Tag = "";
            ddAll.Click += DdDrive_Click;
        }

        private void DdDrive_Click(object sender, EventArgs e)
        {
            Drive =  (string)((ToolStripDropDownItem)sender).Tag;
            refreshData(false);    
        }

        private DataTable IOStats(Int32 instanceid, DateTime from, DateTime to, string connectionString,Int32 DatabaseID,string drive)
        {
            var dt = new DataTable();
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"IOStats_Get", cn);
                cmd.Parameters.AddWithValue("@InstanceID", instanceid);
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDate", to);
                if (filegroup.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@FileGroup", filegroup);
                }
                cmd.Parameters.AddWithValue("DateGroupingMin", dateGrouping);
                if (drive != "")
                {
                    cmd.Parameters.AddWithValue("Drive", drive);
                }
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("@DatabaseID", DatabaseID);
                }
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                var da = new SqlDataAdapter(cmd);
                da.Fill(dt);

            }
            return dt;
        }

   

        public void RefreshData(Int32 InstanceID, DateTime fromDate, DateTime toDate, string connectionString,Int32 databaseID)
        {
            
             disableEnableDropdowns();
            
            this.instanceID = InstanceID;
            mins = (Int32)toDate.Subtract(fromDate).TotalMinutes;

            if (this.from!=fromDate || this.to != toDate)
            {
                dateGrouping = Common.DateGrouping(mins, 200);
                tsDateGroup.Text = Common.DateGroupString(dateGrouping);
            }
            this.from = fromDate;
            this.to = toDate;
            this.connectionString = connectionString;
            this.databaseid  = databaseID;
            FileGroup = "";
            populateFileGroupFilter();     
            getDrives();
            refreshData();
            
        }

        private void disableEnableDropdowns()
        {
            foreach(ToolStripMenuItem ts in tsMeasures.DropDownItems)
            {
                ts.Visible= (!(dateGrouping<=1 && ts.Name.StartsWith("Max")));
                ts.Checked = ts.Enabled ? ts.Checked : false;
            }
        }

        public void RefreshData()
        {
           
            if (DateTime.UtcNow.Subtract(ioTime).TotalMinutes > 30 || dateGrouping !=  1)
            {
                from = DateTime.UtcNow.AddMinutes(-mins);
                to = DateTime.UtcNow.AddMinutes(1);
                refreshData(false);
            }
            else
            {
                from = ioTime.AddSeconds(1);
                to = DateTime.UtcNow.AddMinutes(1);
                refreshData(true);
            }
        }

        private void refreshData(bool update = false)
        {

            string DateFormat = "HH:mm";
            if (mins > 1440)
            {
                DateFormat = "yyyy-MM-dd HH:mm";
            }
            var dt = IOStats(instanceID, from, to, connectionString,databaseid,drive);
            var cnt = dt.Rows.Count;
            if(cnt==0 && update)
            {
                return;
            }

            var columns = new Dictionary<string, columnMetaData>
            {
                {"MBsec", new columnMetaData{Alias="MB/sec",isVisible=true } },
                {"ReadMBsec", new columnMetaData{Alias="Read MB/sec",isVisible=false } },          
                {"WriteMBsec", new columnMetaData{Alias="Write MB/sec",isVisible=false } },          
                {"IOPs", new columnMetaData{Alias="IOPs",isVisible=true,axis=1 } },          
                {"ReadIOPs", new columnMetaData{Alias="Read IOPs",isVisible=false,axis=1 } },
                {"WriteIOPs", new columnMetaData{Alias="Write IOPs",isVisible=false ,axis=1} },
                {"Latency", new columnMetaData{Alias="Latency",isVisible=true,axis=2 } },
                {"ReadLatency", new columnMetaData{Alias="Read Latency",isVisible=false,axis=2} },
                {"WriteLatency", new columnMetaData{Alias="Write Latency",isVisible=false,axis=2 } },
                {"MaxMBsec", new columnMetaData{Alias="Max MB/sec",isVisible=false } },
                {"MaxReadMBsec", new columnMetaData{Alias="Max Read MB/sec",isVisible=false } },
                {"MaxWriteMBsec", new columnMetaData{Alias="Max Write MB/sec",isVisible=false } },
                {"MaxIOPs", new columnMetaData{Alias="Max IOPs",isVisible=false,axis=1 } },
                {"MaxReadIOPs", new columnMetaData{Alias="Max Read IOPs",isVisible=false,axis=1 } },
                {"MaxWriteIOPs", new columnMetaData{Alias="Max Write IOPs",isVisible=false ,axis=1} },
                {"MaxLatency", new columnMetaData{Alias="Max Latency",isVisible=false,axis=2 } },
                {"MaxReadLatency", new columnMetaData{Alias="Max Read Latency",isVisible=false ,axis=2} },
                {"MaxWriteLatency", new columnMetaData{Alias="Max Write Latency",isVisible=false ,axis=2} }
            };

            foreach(ToolStripMenuItem ts in tsMeasures.DropDownItems)
            {
                columns[ts.Name].isVisible = ts.Checked;
            }
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
                    ioTime = (DateTime)r["SnapshotDate"];
                    columns[s].Points[i] = new DateTimePoint(ioTime.ToLocalTime(), v );
                }
                i++;
            }

            SeriesCollection sc;
            if (!update)
            {
                sc = new SeriesCollection();
                chartIO.Series = sc;
                foreach (string s in columns.Keys)
                {
                    sc.Add(new LineSeries
                    {
                        Title = columns[s].Alias,
                        Tag = s,
                        ScalesYAt = columns[s].axis,
                        PointGeometrySize = cnt <= 100 ? PointSize : 0,
                        LineSmoothness = SmoothLines ? 1 : 0
                    }
                    );
                }
                chartIO.AxisX.Clear();
                chartIO.AxisY.Clear();
                chartIO.AxisX.Add(new Axis
                {
                    Title = "Time",
                    LabelFormatter = val => new System.DateTime((long)val).ToString(DateFormat)

                });
                chartIO.AxisY.Add(new Axis
                {
                    Title = "MB/sec",
                    LabelFormatter = val => val.ToString("0.0 MB"),
                    MinValue=0
                });
                chartIO.AxisY.Add(new Axis
                {
                    Title = "IOPs",
                    LabelFormatter = val => val.ToString("0.0 IOPs"),
                    Position = AxisPosition.RightTop,
                    MinValue=0
                });
                chartIO.AxisY.Add(new Axis
                {
                    Title = "Latency",
                    LabelFormatter = val => val.ToString("0.0ms"),
                    Position = AxisPosition.RightTop,
                    MinValue=0,
                    MaxValue=200
                });
            }
            else
            {
                sc = chartIO.Series;
                foreach(LineSeries s in sc)
                {
                    if(s.Values.Count>0 && DateTime.Now.Subtract(((DateTimePoint)s.Values[0]).DateTime).TotalMinutes > mins)
                    {
                        s.Values.RemoveAt(0);
                    }
                }
            };

            bool addDropdowns = tsMeasures.DropDownItems.Count == 0;
        

            foreach (LineSeries s in chartIO.Series)
            {
                var c = columns[(string)s.Tag];
                if (s.Values == null)
                {
                    var v = new ChartValues<DateTimePoint>();
                    v.AddRange(c.Points);
                    s.Values = v;
                    
                }
                else
                {
                    s.Values.AddRange(c.Points);
                }
                s.Visibility = c.isVisible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                if (addDropdowns)
                {                  
                    var dd = new ToolStripMenuItem(c.Alias);
                    dd.Name = (string)s.Tag;
                    dd.CheckOnClick = true;
                    dd.Visible = (!(dd.Name.StartsWith("Max") && dateGrouping <=1));
                    dd.Checked = dd.Enabled ? c.isVisible : false;
                    dd.Click += measureDropDown_Click;
                    tsMeasures.DropDownItems.Add(dd);
                }
            }
            if (chartIO.Series[0].Values.Count == 1)
            {
                chartIO.Series.Clear(); // fix tends to zero error
            }
            lblIOPerformance.Text = databaseid > 0 ? "IO Performance: Database" : "IO Performance: Instance";
        }

        private void measureDropDown_Click(object sender, EventArgs e)
        {
            var dd = (ToolStripMenuItem)sender;
            foreach(LineSeries s in chartIO.Series)
            {
                if((string)s.Tag == dd.Name)
                {
                    s.Visibility = dd.Checked ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                }
            }
        }

        private void IOPerformance_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGroup, TsDateGroup_Click);

        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            dateGrouping = Convert.ToInt32(ts.Tag);
            tsDateGroup.Text = Common.DateGroupString(dateGrouping);
            refreshData(false);
        }
    }
}
