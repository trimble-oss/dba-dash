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

namespace DBADashGUI.Performance
{
    public partial class ObjectExecutionSummary : UserControl
    {
        public ObjectExecutionSummary()
        {
            InitializeComponent();
        }

        public string Instance { get; set; }
        public Int32 InstanceID { get; set; }
        public Int32 DatabaseID { get; set; }
        public Int64 ObjectID { get; set; }

        public string Types { 
            get
            {
                string types = "";
                foreach(ToolStripMenuItem mnu in tsType.DropDownItems)
                {
                    if (mnu.Checked)
                    {
                        types += (types.Length>0 ? "," : "") + mnu.Tag;
                    }
                }
                return types;
            }
            set
            {
                var types = value.Split(',');
                foreach (ToolStripMenuItem mnu in tsType.DropDownItems)
                {
                   mnu.Checked = types.Contains(mnu.Tag);
                }
            }
        }

        Int32 mins = 15;
        private DateTime _from = DateTime.MinValue;
        private DateTime _to = DateTime.MinValue;

        private Int32 compareOffset = 0;

        private DateTime fromDate
        {
            get
            {
                if (_from == DateTime.MinValue)
                {
                    DateTime now = DateTime.UtcNow;
                    if (mins >= 720)  // round to nearest hr
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
                    }
                    else
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
                    }
                    return now.AddMinutes(-mins);
                }
                else
                {
                    return _from;
                }
            }
        }

        private DateTime toDate
        {
            get
            {
                if (_to == DateTime.MinValue)
                {
                    return DateTime.UtcNow;
                }
                else
                {
                    return _to;
                }

            }
        }

        private DateTime _compareTo=DateTime.MinValue;
        private DateTime _compareFrom=DateTime.MinValue;

        private DateTime compareTo
        {
            get {
                if (compareOffset > 0)
                {
                    return new DateTime(toDate.Year, toDate.Month, toDate.Day, toDate.Hour, toDate.Minute, 0, DateTimeKind.Utc).AddMinutes(-compareOffset);
                }
                else
                {
                    return _compareTo;
                }
            }
        }
        private DateTime compareFrom
        {
            get
            {
                if (compareOffset > 0)
                {
                    return fromDate.AddMinutes(-compareOffset);
                }
                else
                {
                    return _compareFrom;
                }
            }
        }

        readonly List<DataGridViewColumn> StandardCols = new List<DataGridViewColumn> {new DataGridViewTextBoxColumn() { Name = "DB", DataPropertyName = "DB",DisplayIndex=0 },
                                                                        new DataGridViewTextBoxColumn() { Name = "Schema", DataPropertyName = "SchemaName",DisplayIndex=1},
                                                                        new DataGridViewLinkColumn { Name = "Name", DataPropertyName = "ObjectName",DisplayIndex=2, SortMode = DataGridViewColumnSortMode.Automatic},
                                                                        new DataGridViewTextBoxColumn() { Name = "Type", DataPropertyName = "TypeDescription",DisplayIndex=3}

        };
        readonly List<DataGridViewColumn> Cols = new List<DataGridViewColumn> { new DataGridViewTextBoxColumn()  { Name= "Total Duration (sec)",Visible=false, DataPropertyName = "total_duration_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.000" } },
                                                                        new DataGridViewTextBoxColumn()  { Name= "Total Duration (ms/sec)", DataPropertyName = "duration_ms_per_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.####" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Duration (sec)", DataPropertyName = "avg_duration_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.000" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Executions", Visible=false, DataPropertyName = "execution_count" },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Executions/min", DataPropertyName = "execs_per_min", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###"} },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total CPU (sec)",Visible=false, DataPropertyName = "total_cpu_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total CPU (ms/sec)", DataPropertyName = "cpu_ms_per_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg CPU (sec)", DataPropertyName = "avg_cpu_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total Physical Reads",Visible=false, DataPropertyName = "total_physical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Physical Reads", DataPropertyName = "avg_physical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total Logical Reads", DataPropertyName = "total_logical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Logical Reads",Visible=false, DataPropertyName = "avg_logical_reads", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Total Writes", Visible=false,DataPropertyName = "total_writes", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Writes",Visible=false, DataPropertyName = "avg_writes", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Period Time (sec)",Visible=false, DataPropertyName = "period_time_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###" } },

        };

        public void addColumns()
        {
            tsColumns.DropDownItems.Clear();
            foreach(DataGridViewColumn col in Cols)
            {
                tsColumns.DropDownItems.Add(new ToolStripMenuItem(col.Name,null,tsColumn_Click ) { Checked = col.Visible, CheckOnClick=true });
            }
        }

        private void tsColumn_Click(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(ToolStripMenuItem)) {
                var mnu = (ToolStripMenuItem)sender;
                foreach (string name in new string[] { mnu.Text, "Compare " + mnu.Text })
                {
                    if (dgv.Columns.Contains(name))
                    {
                        dgv.Columns[name].Visible = mnu.Checked;
                    }
                }
            }
        }




        public List<DataGridViewColumn> Columns
        {
            get
            {
                var _cols = new List<DataGridViewColumn>();
                _cols.AddRange(StandardCols.ToArray());
                var displayIdx = _cols.Count;
                foreach (var col in Cols)
                {
                    col.DisplayIndex = displayIdx;
                    col.Visible = true;
                    _cols.Add(col);
                    displayIdx += 1;
                    if (compareFrom > DateTime.MinValue)
                    {
                        var compareCol = new DataGridViewTextBoxColumn() { Name = "Compare " + col.Name, DataPropertyName = "compare_" + col.DataPropertyName, DisplayIndex= displayIdx };
                        compareCol.DefaultCellStyle = col.DefaultCellStyle.Clone();
                        compareCol.DefaultCellStyle.BackColor = Color.BlanchedAlmond;         
                        _cols.Add(compareCol);
                        displayIdx += 1;
                        if (col.DataPropertyName== "avg_duration_sec" || col.DataPropertyName == "avg_cpu_sec")
                        {
                            var diffCol = new DataGridViewTextBoxColumn() { Name = "Diff " + col.Name.Replace("sec","%"), DataPropertyName = "diff_" + col.DataPropertyName.Replace("_sec","_pct"), DisplayIndex = displayIdx };
                            diffCol.DefaultCellStyle.Format = "P2";
                            diffCol.DefaultCellStyle.BackColor = Color.AliceBlue;
                            _cols.Add(diffCol);
                            displayIdx += 1;
                        }
                    }
                }

                return _cols;
            }
        }

        private void setColVisibility()
        {
            foreach (var ts in tsColumns.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var mnu = (ToolStripMenuItem)ts;
                    foreach (string name in new string[] { mnu.Text, "Compare " + mnu.Text })
                    {
                        if (dgv.Columns.Contains(name))
                        {
                            dgv.Columns[name].Visible = mnu.Checked;
                        }
                    }
                }
            }
        }



        public void RefreshData()
        {
            splitContainer1.Panel1Collapsed = true;
            refreshData();
        }

        private void refreshData()
        {
            dgv.DataSource = null;
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.ObjectExecutionStatsSummary_Get", cn);
                if (InstanceID > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                }
                else if (Instance != null && Instance.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Instance", Instance);
                }
                else
                {
                    throw new Exception("Instance not provided to Object Execution Summary");
                }
                if (ObjectID > 0)
                {
                    cmd.Parameters.AddWithValue("ObjectID", ObjectID);
                }
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
                }
                if (compareFrom != DateTime.MinValue && compareFrom != DateTime.MaxValue && compareTo != DateTime.MinValue && compareTo != DateTime.MaxValue)
                {
                    cmd.Parameters.AddWithValue("CompareFrom", compareFrom);
                    cmd.Parameters.AddWithValue("CompareTo", compareTo);
                }
                if (Types.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Types", Types);
                }

                cmd.Parameters.AddWithValue("FromDate", fromDate);
                cmd.Parameters.AddWithValue("ToDate", toDate);
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 1)
                {
                    refreshChart((Int64)dt.Rows[0]["ObjectID"], (string)dt.Rows[0]["ObjectName"]);
                }
                dgv.Columns.Clear();
                dgv.AutoGenerateColumns = false;


                dgv.Columns.AddRange(Columns.ToArray());
                setColVisibility();

                dgv.DataSource = new DataView(dt, null, "total_duration_sec DESC", DataViewRowState.CurrentRows);
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                if (splitContainer1.Panel1Collapsed == false)
                {
                    refreshChart();
                }

            }
        }


        private void checkTime()
        {
            foreach (var ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var tsmi = (ToolStripMenuItem)ts;
                    tsmi.Checked = Int32.Parse((string)tsmi.Tag) == mins;
                    if (tsmi.Checked)
                    {
                        tsTimeOffset.Text = "Previous " + tsmi.Text;
                    }
                }
            }
        }

        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            mins = Int32.Parse((string)itm.Tag);
            setMins();
            refreshData();
        }

        private void setMins()
        {
            _from = DateTime.MinValue;
            _to = DateTime.MinValue;
            tsTimeOffset.Tag = mins.ToString();
            if(compareFrom!=DateTime.MinValue && compareFrom != DateTime.MaxValue)
            {
                compareOffset = mins;
                checkOffset();
            }          
            checkTime();
        }

        private void tsCustom_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker
            {
                FromDate = fromDate.ToLocalTime(),
                ToDate = toDate.ToLocalTime()
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                _from = frm.FromDate.ToUniversalTime();
                _to = frm.ToDate.ToUniversalTime();
                mins = 0;
                checkTime();
                tsTimeOffset.Tag = _to.Subtract(_from).TotalMinutes.ToString();
                tsTimeOffset.Text = "Previous " + _to.Subtract(_from).ToString();
                refreshData();
                tsCustom.Checked = true;
            }          
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }


        private void tsSetOffset_Click(object sender, EventArgs e)
        {
            compareOffset = Int32.Parse((string)((ToolStripMenuItem)sender).Tag);
            checkOffset();
            refreshData();
        }

        private void checkOffset()
        {

            tsNoCompare.Checked = compareOffset <= 0;          
            foreach (ToolStripItem itm in tsCompare.DropDownItems)
            {
                if (itm.GetType() == typeof(ToolStripMenuItem))
                {
                    ((ToolStripMenuItem)itm).Checked = Int32.Parse((string)itm.Tag) == compareOffset;
                }
            }
        }

        private void ObjectExecutionSummary_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = true;
            addColumns();
            setMins();
        }

        private void tsCustomCompare_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker
            {
                FromDate = compareFrom > DateTime.MinValue && compareFrom<DateTime.MaxValue ? compareFrom.ToLocalTime() : fromDate.ToLocalTime(),
                ToDate = compareTo > DateTime.MinValue && compareTo<DateTime.MaxValue ? compareTo.ToLocalTime() : toDate.ToLocalTime()
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                _compareFrom = frm.FromDate.ToUniversalTime();
                _compareTo = frm.ToDate.ToUniversalTime();
                compareOffset = 0;
                checkOffset();
                refreshData();
                tsCustomCompare.Checked = true;
            }
        }

        private void tsType_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {

            if (dgv.Columns.Contains("Diff Avg Duration (%)")){
                for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
                {
                    var r = dgv.Rows[idx];
                    var row = (DataRowView)r.DataBoundItem;
                    double diffAvgDurationPct = row["diff_avg_duration_pct"] == DBNull.Value ? 0d : Convert.ToDouble(row["diff_avg_duration_pct"]);
                    double diffAvgCPUPct = row["diff_avg_cpu_pct"] == DBNull.Value ? 0d : Convert.ToDouble(row["diff_avg_cpu_pct"]);
                    r.Cells["Diff Avg Duration (%)"].Style.BackColor = diffColourFromDouble(diffAvgDurationPct);
                    r.Cells["Diff Avg CPU (%)"].Style.BackColor = diffColourFromDouble(diffAvgCPUPct);
                }
            }
        }

        private Color diffColourFromDouble(Double value)
        {
            if (value > 0.5)
            {
                return Color.Red;
            }
            else if (value>0.3){
                return Color.FromArgb(242, 215, 213);
            }
            else if (value < -0.5)
            {
                return Color.Green;
            }
            else if (value<-0.3)
            {
                return Color.FromArgb(171, 235, 198);
            }
            else
            {
                return Color.AliceBlue;
            }
        }

        private void refreshChart()
        {
            objectExecutionLineChart1.FromDate = compareFrom> DateTime.MinValue && compareFrom<fromDate? compareFrom : fromDate;
            objectExecutionLineChart1.ToDate = compareTo>=toDate && compareTo < DateTime.MaxValue ? compareTo : toDate;
            objectExecutionLineChart1.RefreshData();
        }

        private void refreshChart(Int64 objectID,string title)
        {
            splitContainer1.Panel1Collapsed = false;
            objectExecutionLineChart1.InstanceID = InstanceID;
            objectExecutionLineChart1.Instance = Instance;
            objectExecutionLineChart1.ObjectID = objectID;
            objectExecutionLineChart1.Title = title;
            refreshChart();
        }


        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if(dgv.Columns[e.ColumnIndex].Name == "Name")
                {                 
                    var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                    refreshChart((Int64)row["ObjectID"], (string)row["ObjectName"]);               
                }
            }
        }
    }
}
