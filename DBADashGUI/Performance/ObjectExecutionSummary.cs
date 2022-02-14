using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

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

        private Int32 compareOffset = 0;
        private DateTime _compareTo=DateTime.MinValue;
        private DateTime _compareFrom=DateTime.MinValue;
        private DataTable dt;

        private DateTime compareTo
        {
            get {
                var toDate = DateRange.ToUTC;
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
                    return DateRange.FromUTC.AddMinutes(-compareOffset);
                }
                else
                {
                    return _compareFrom;
                }
            }
        }

        readonly List<DataGridViewColumn> StandardCols = new List<DataGridViewColumn> {new DataGridViewTextBoxColumn() { Name = "DB", DataPropertyName = "DB",DisplayIndex=0 },
                                                                        new DataGridViewTextBoxColumn() { Name = "Schema", DataPropertyName = "SchemaName",DisplayIndex=1},
                                                                        new DataGridViewLinkColumn { Name = "Name", DataPropertyName = "ObjectName",DisplayIndex=2, SortMode = DataGridViewColumnSortMode.Automatic, LinkColor = DashColors.LinkColor},
                                                                        new DataGridViewTextBoxColumn() { Name = "Type", DataPropertyName = "TypeDescription",DisplayIndex=3}

        };
        readonly List<DataGridViewColumn> Cols = new List<DataGridViewColumn> { new DataGridViewTextBoxColumn()  { Name= "Total Duration (sec)",Visible=false, DataPropertyName = "total_duration_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.000" } },
                                                                        new DataGridViewTextBoxColumn()  { Name= "Total Duration (ms/sec)", DataPropertyName = "duration_ms_per_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.####" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Avg Duration (sec)", DataPropertyName = "avg_duration_sec", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.000" } },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Executions", Visible=false, DataPropertyName = "execution_count" },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Executions/min", DataPropertyName = "execs_per_min", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###"} },
                                                                        new DataGridViewTextBoxColumn()  { Name = "Max Executions/min", DataPropertyName = "max_execs_per_min", DefaultCellStyle = new DataGridViewCellStyle() { Format = "#,##0.###"} },
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

        public void RefreshData()
        {
            splitContainer1.Panel1Collapsed = true;
            refreshData();
        }

        private void refreshData()
        {
            dgv.DataSource = null;

            var dt = getObjectExecutionStatsSummary();
            if (dt.Rows.Count == 1)
            {
                refreshChart((Int64)dt.Rows[0]["ObjectID"], (string)dt.Rows[0]["ObjectName"]);
            }
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;

            setDataSourceWithFilter();
            dgv.Columns.AddRange(Columns.ToArray());
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            if (splitContainer1.Panel1Collapsed == false)
            {
                refreshChart();
            }
           
        }

        private DataTable getObjectExecutionStatsSummary()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd = new SqlCommand("dbo.ObjectExecutionStatsSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
            using(var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
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

                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);

                dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void setDataSourceWithFilter()
        {
            if (string.IsNullOrEmpty(txtSearch.Text.Trim()))
            {
                dgv.DataSource = new DataView(dt, null, "total_duration_sec DESC", DataViewRowState.CurrentRows);
                lblSearch.Font= new Font(lblSearch.Font, FontStyle.Regular);
            }
            else
            {
                lblSearch.Font = new Font(lblSearch.Font, FontStyle.Bold);
                try
                {
                    dgv.DataSource = new DataView(dt, string.Format("SchemaName LIKE '%{0}%' OR ObjectName LIKE '%{0}%'", txtSearch.Text.Replace("'", "''")), "total_duration_sec DESC", DataViewRowState.CurrentRows);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Filter Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgv.DataSource = new DataView(dt, null, "total_duration_sec DESC", DataViewRowState.CurrentRows);
                }
            }
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
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
            tsNoCompare.Checked = compareOffset == 0;          
            foreach (ToolStripItem itm in tsCompare.DropDownItems)
            {
                if (itm.GetType() == typeof(ToolStripMenuItem))
                {
                    var ts = (ToolStripMenuItem)itm;
                    ts.Checked = Int32.Parse((string)itm.Tag) == compareOffset;
                    if (ts.Checked)
                    {
                        tsCompare.Text = "Compare To: " + ts.Text;
                    }
                }
            }
        }

        private void ObjectExecutionSummary_Load(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = true;
        }

        private void tsCustomCompare_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker
            {
                FromDate = compareFrom > DateTime.MinValue && compareFrom<DateTime.MaxValue ? compareFrom.ToLocalTime() : DateRange.FromUTC.ToLocalTime(),
                ToDate = compareTo > DateTime.MinValue && compareTo<DateTime.MaxValue ? compareTo.ToLocalTime() : DateRange.ToUTC.ToLocalTime()
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                _compareFrom = frm.FromDate.ToUniversalTime();
                _compareTo = frm.ToDate.ToUniversalTime();
                compareOffset = -1;
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
                    r.Cells["Diff Avg Duration (%)"].SetStatusColor(diffColourFromDouble(diffAvgDurationPct));
                    r.Cells["Diff Avg CPU (%)"].SetStatusColor(diffColourFromDouble(diffAvgCPUPct));
                }
            }
        }

        private Color diffColourFromDouble(Double value)
        {
            if (value > 0.5)
            {
                return DashColors.Red;
            }
            else if (value>0.3){
                return DashColors.RedLight;
            }
            else if (value < -0.5)
            {
                return DashColors.Success;
            }
            else if (value<-0.3)
            {
                return DashColors.GreenLight;
            }
            else
            {
                return DashColors.BlueLight;
            }
        }

        private void refreshChart()
        {
            if (compareFrom > DateTime.MinValue)
            {
                splitChart.Panel2Collapsed = false;
                compareObjectExecutionLineChart.FromDate = compareFrom;
                compareObjectExecutionLineChart.ToDate = compareTo;
                compareObjectExecutionLineChart.RefreshData();

            }
            else
            {
                splitChart.Panel2Collapsed = true;
            }
            objectExecutionLineChart1.FromDate = DateRange.FromUTC;
            objectExecutionLineChart1.ToDate = DateRange.ToUTC;
            objectExecutionLineChart1.RefreshData();
        }

        private void refreshChart(Int64 objectID,string title)
        {

            splitContainer1.Panel1Collapsed = false;

            compareObjectExecutionLineChart.InstanceID = InstanceID;
            compareObjectExecutionLineChart.Instance = Instance;
            compareObjectExecutionLineChart.ObjectID = objectID;
            compareObjectExecutionLineChart.Title = title + " (Compare)";
       
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

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

  
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            tmrSearch.Enabled = true;
            tmrSearch.Stop();
            tmrSearch.Start();
        }

        private void tmrSearch_Tick(object sender, EventArgs e)
        {
            setDataSourceWithFilter();
            tmrSearch.Enabled = false;
        }

        private void tsCols_Click(object sender, EventArgs e)
        {
            promptColumnSelection(ref dgv);
        }

        private void promptColumnSelection(ref DataGridView gv)
        {
            using (var frm = new SelectColumns())
            {
                frm.Columns = gv.Columns;
                frm.ShowDialog(this);
                if (frm.DialogResult == DialogResult.OK)
                {
                    var dt = ((DataView)dgv.DataSource).Table;
                    gv.AutoResizeColumns();
                    gv.AutoResizeRows();
                }
            }
        }
    }
}
