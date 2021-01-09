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
using DBADashGUI.Performance;

namespace DBADashGUI.Drives
{
    public partial class DriveHistory : UserControl
    {
        public DriveHistory()
        {
            InitializeComponent();
        }

        string connectionString = Common.ConnectionString;

        public Int32 DriveID { get; set; }

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
            var columns = new Dictionary<string, columnMetaData>
            {
                {"SizeGB", new columnMetaData{Alias="Size (GB)",isVisible=true } },
                {"UsedGB", new columnMetaData{Alias="Used (GB)",isVisible=true } }
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
                Title = "GB",
                LabelFormatter = val => val.ToString("0.0 GB"),
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
                SqlCommand cmd = new SqlCommand("dbo.DriveSnapshot_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("FromDate", From);
                cmd.Parameters.AddWithValue("ToDate", To);
                cmd.Parameters.AddWithValue("DriveID", DriveID);
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
    }
}
