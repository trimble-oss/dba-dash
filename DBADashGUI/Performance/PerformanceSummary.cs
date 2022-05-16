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
using static DBADashGUI.Main;

namespace DBADashGUI.Performance
{
    public partial class PerformanceSummary : UserControl
    {

        public List<Int32> InstanceIDs;
        public string TagIDs;
        private List<KeyValuePair<string,PersistedColumnLayout>>standardLayout;

        public Dictionary<int,Counter> SelectedPerformanceCounters =  new Dictionary<int, Counter>();

        public PerformanceSummary()
        {
            InitializeComponent();
            standardLayout = getColumnLayout();
        }

   
        public void RefreshData()
        {
            dgv.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgv.DataSource = null;
            var dt = getPerformanceSummary();
            addPerformanceCounters(ref dt);
            dgv.AutoGenerateColumns = false;
            generateHistogram(ref dt);
            if (dgv.DataSource == null)
            {
                dgv.DataSource = new DataView(dt);
            }
            dgv.AutoResizeColumnHeadersHeight();
            dgv.Columns["colCPUHistogram"].Width = 200;
        }

        private void addPerformanceCounterColsToGrid()
        {
            List<string> pcColNames = new List<string>();
            foreach (var ctr in SelectedPerformanceCounters.Values)
            {
                foreach (string agg in ctr.GetAggColumns())
                {
                    string name = agg + "_" + ctr.CounterID;
                    pcColNames.Add(name);
                    if (!dgv.Columns.Contains(name))
                    {
                        dgv.Columns.Add(new DataGridViewTextBoxColumn() { Name = name, DataPropertyName = name, HeaderText = agg + " " + ctr.ToString().Replace("\\", " \\ "), Tag = "PC", Width = 70, DefaultCellStyle = Common.DataGridViewNumericCellStyle });
                    }
                }
            }
            var colsToRemove = dgv.Columns.Cast<DataGridViewColumn>().Where(col => (string)col.Tag == "PC" && !pcColNames.Contains(col.Name)).ToList();
            foreach (var col in colsToRemove)
            {
                dgv.Columns.Remove(col);
            }
        }

        private void addPerformanceCounterColsToTable(ref DataTable dt)
        {
            foreach (var ctr in SelectedPerformanceCounters.Values)
            {
                foreach (string agg in ctr.GetAggColumns())
                {
                    string name = agg + "_" + ctr.CounterID;
                    dt.Columns.Add(name, typeof(double));
                    dt.Columns.Add(name + "Status", typeof(int));        
                }
            }
        }

        void addPerformanceCounters(ref DataTable dt)
        {
            addPerformanceCounterColsToGrid();
            addPerformanceCounterColsToTable(ref dt);
            if (SelectedPerformanceCounters.Count > 0)
            {
                var pcDT = getPerformanceCounters();
                DataRow mainRow = null;
                int instanceIdMainRow = -1;
                foreach (DataRow r in pcDT.Rows)
                {
                    int instanceID = (int)r["InstanceID"];
                    int CounterID = (int)r["CounterID"];
                    var cntr = SelectedPerformanceCounters[CounterID];
                    if (instanceID != instanceIdMainRow)
                    {
                        mainRow = dt.Select("InstanceId=" + (int)r["InstanceID"]).FirstOrDefault();
                        instanceIdMainRow = instanceID;
                    }
                    if (mainRow != null)
                    {
                        if (cntr.Avg)
                        {
                            mainRow[("Avg_" + (int)r["CounterID"]).ToString()] = r["AvgValue"];
                            mainRow[("Avg_" + (int)r["CounterID"] + "Status").ToString()] = r["AvgValueStatus"];                            
                        }
                        if (cntr.Max)
                        {
                            mainRow[("Max_" + (int)r["CounterID"]).ToString()] = r["MaxValue"];
                            mainRow[("Max_" + (int)r["CounterID"] + "Status").ToString()] = r["MaxValueStatus"];                            
                        }
                        if (cntr.Total)
                        {
                            mainRow[("Total_" + (int)r["CounterID"]).ToString()] = r["TotalValue"];
                        }
                        if (cntr.Current)
                        {
                            mainRow[("Current_" + (int)r["CounterID"]).ToString()] = r["CurrentValue"];
                            mainRow[("Current_" + (int)r["CounterID"] + "Status").ToString()] = r["CurrentValueStatus"];
                        }
                        if (cntr.Min)
                        {
                            mainRow[("Min_" + (int)r["CounterID"]).ToString()] = r["MinValue"];
                            mainRow[("Min_" + (int)r["CounterID"] + "Status").ToString()] = r["MinValueStatus"];
                        }
                        if (cntr.SampleCount)
                        {
                            mainRow[("SampleCount_" + (int)r["CounterID"]).ToString()] = r["SampleCount"];
                        }
                    }
                }
            }
        }

        DataTable getPerformanceCounters()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                if (InstanceIDs.Count > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                }
                else
                {
                    cmd.Parameters.AddWithValue("TagIDs", TagIDs);
                }
                var counters = String.Join(",", SelectedPerformanceCounters.Values.Select(pc => pc.CounterID));
                cmd.Parameters.AddWithValue("Counters", counters);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }           
        }


        DataTable getPerformanceSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.PerformanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
            {
                cn.Open();
                if (InstanceIDs.Count > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                }
                else
                {
                    cmd.Parameters.AddWithValue("TagIDs", TagIDs);
                }
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                var pkCols = new DataColumn[1];
                pkCols[0] =  dt.Columns.Add("InstanceID", typeof(int));
                dt.PrimaryKey = pkCols;
                da.Fill(dt);
                return dt;
            }          
        }
    

        private void generateHistogram(ref DataTable dt)
        {
            if (dt.Rows.Count > 0 && dgv.Columns["colCPUHistogram"].Visible && (!dt.Columns.Contains("CPUHistogram")) )
            {
                dgv.DataSource = null;
                dt.Columns.Add("CPUHistogram", typeof(Bitmap));
                dt.Columns.Add("CPUHistogramTooltip", typeof(string));
                foreach (DataRow row in dt.Rows)
                {                    
                    var hist = new List<double>();
                    if (row["CPU10"] != DBNull.Value)
                    {
                        StringBuilder sbToolTip = new StringBuilder();
                        for (Int32 i = 10; i <= 100; i += 10)
                        {
                            var v = Convert.ToDouble(row["CPU" + i.ToString()]);
                            hist.Add(v);
                            sbToolTip.AppendLine((i-10).ToString() + " to " +  i.ToString() + "% | " + v.ToString("N0"));
                        }
                        row["CPUHistogram"] = Histogram.GetHistogram(hist, 200, 100, true);
                        row["CPUHistogramToolTip"] = sbToolTip.ToString();

                    }
                    else
                    {
                        row["CPUHistogram"] = new Bitmap(1, 1);
                    }
                    
                }
                dgv.DataSource = new DataView(dt);
            }
        }

        private void PerformanceSummary_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgv);
            addHistCols(dgv, "col");
            loadSavedView();
        }

        private void addHistCols(DataGridView dgv, string prefix)
        {
            string histogram = "CPU";        

            for (int i = 10; i <= 100; i += 10)
            {
                var col = new DataGridViewTextBoxColumn()
                {
                    Name = prefix + histogram + "Histogram_" + i,
                    DataPropertyName = histogram + i.ToString(),
                    Visible = false,
                    HeaderText = histogram + " Histogram " + (i - 10).ToString() + " to " + i.ToString() + "%"
                };
                dgv.Columns.Add(col);
            }
            

        }


        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }


        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            bool histogram = ((DataView)dgv.DataSource).Table.Columns.Contains("CPUHistogram");
            var pcCols = dgv.Columns.Cast<DataGridViewColumn>().Where(col => Convert.ToString(col.Tag) == "PC" 
                                                                && (col.DataPropertyName.StartsWith("Avg") || col.DataPropertyName.StartsWith("Max") || col.DataPropertyName.StartsWith("Min") || col.DataPropertyName.StartsWith("Current")) 
                                                                )
                                                                .Select(col => col.DataPropertyName)
                                                                .ToList();
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                var pAvgCPU = (CustomProgressControl.DataGridViewProgressBarCell)r.Cells["AvgCPU"];
                var pMaxCPU = (CustomProgressControl.DataGridViewProgressBarCell)r.Cells["MaxCPU"];
                var avgCPUstatus = (DBADashStatus.DBADashStatusEnum)row["AvgCPUStatus"];
                var maxCPUstatus = (DBADashStatus.DBADashStatusEnum)row["MaxCPUStatus"];

                DBADashStatus.SetProgressBarColor(avgCPUstatus,ref pAvgCPU);
                DBADashStatus.SetProgressBarColor(maxCPUstatus, ref pMaxCPU);
              
                foreach(var pcCol in pcCols)
                {
                    var status = row[pcCol + "Status"];
                    if (status != DBNull.Value)
                    {
                        r.Cells[pcCol].SetStatusColor((DBADashStatus.DBADashStatusEnum)Convert.ToInt32(status));
                    }
                    else
                    {
                        r.Cells[pcCol].SetStatusColor(Color.White);
                    }
                }

                r.Cells["ReadLatency"].SetStatusColor((DBADashStatus.DBADashStatusEnum)row["ReadLatencyStatus"]);
                r.Cells["WriteLatency"].SetStatusColor((DBADashStatus.DBADashStatusEnum)row["WriteLatencyStatus"]);
                r.Cells["CriticalWaitMsPerSec"].SetStatusColor((DBADashStatus.DBADashStatusEnum)row["CriticalWaitStatus"]);
                if (histogram)
                {
                     r.Height = 100;
                    r.Cells["colCPUHistogram"].ToolTipText = row["CPUHistogramTooltip"]==DBNull.Value ? "" : (string)row["CPUHistogramTooltip"];
                }
            }
             
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            var cpuHistVisible = colCPUHistogram.Visible;
            if (cpuHistVisible)
            {
                for (int i = 10; i <= 100; i += 10)
                {
                    dgv.Columns["colCPUHistogram_" + i.ToString()].Visible = true;
                }
            }
            colCPUHistogram.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colCPUHistogram.Visible = cpuHistVisible;
            if (cpuHistVisible)
            {
                for (int i = 10; i <= 100; i += 10)
                {
                    dgv.Columns["colCPUHistogram_" + i.ToString()].Visible = false ;
                }
            }
        }

        public event EventHandler<InstanceSelectedEventArgs> Instance_Selected;


        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                Instance_Selected(this, new InstanceSelectedEventArgs() { InstanceID = (Int32)row["InstanceID"], Tab="tabPerformance"});
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
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
                    generateHistogram(ref dt);
                }
            }
        }


        private void saveLayout() 
        {           
            string jsonPC = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedPerformanceCounters, Newtonsoft.Json.Formatting.Indented);
            Properties.Settings.Default.PerformanceSummaryPerformanceCounters = jsonPC;
   
            string jsonCols = Newtonsoft.Json.JsonConvert.SerializeObject(getColumnLayout(), Newtonsoft.Json.Formatting.Indented);
            Properties.Settings.Default.PerformanceSummaryCols = jsonCols;
            
            Properties.Settings.Default.Save();           
        }

        private List<KeyValuePair<string, PersistedColumnLayout>> getColumnLayout()
        {
           return dgv.Columns.Cast<DataGridViewColumn>()
          .Select(c => new KeyValuePair<string, PersistedColumnLayout>(c.Name, new PersistedColumnLayout() { Visible = c.Visible, Width = c.Width, DisplayIndex = c.DisplayIndex }))
          .ToList();
        }


        private void loadSavedView()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.PerformanceSummaryPerformanceCounters))
            {
                var json = Properties.Settings.Default.PerformanceSummaryPerformanceCounters;
                try
                {
                    SelectedPerformanceCounters = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Counter>>(json);
                    addPerformanceCounterColsToGrid();
                }
                catch (Exception ex)
                {
                    Properties.Settings.Default.PerformanceSummaryPerformanceCounters = string.Empty;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Error loading saved view.  The view has been reset: \n" + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.PerformanceSummaryCols))
            {
                var jsonCols = Properties.Settings.Default.PerformanceSummaryCols;
                try
                {
                    var savedCols = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KeyValuePair<string,PersistedColumnLayout>>>(jsonCols);
                    loadPersistedColumnLayout(savedCols);
                }
                catch (Exception ex)
                {
                    Properties.Settings.Default.PerformanceSummaryCols = string.Empty;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Error loading saved view.  The view has been reset: \n" + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void loadPersistedColumnLayout(List<KeyValuePair<string,PersistedColumnLayout>> savedCols)
        {
            foreach (var col in savedCols)
            {
                if (dgv.Columns.Contains(col.Key))
                {
                    dgv.Columns[col.Key].Visible = col.Value.Visible;
                    dgv.Columns[col.Key].Width = col.Value.Width;
                    if (col.Value.DisplayIndex >= 0)
                    {
                        dgv.Columns[col.Key].DisplayIndex = col.Value.DisplayIndex;
                    }
                }
            }
        }

        private void tsSaveLayout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Save layout?  Column selection, size and position will be saved.", "Save Layout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                saveLayout();
            }
        }

        private void resetLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset the column selection back to the defaults?", "Reset Layout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SelectedPerformanceCounters.Clear();
                addPerformanceCounterColsToGrid();
                loadPersistedColumnLayout(standardLayout);
                Properties.Settings.Default.PerformanceSummaryPerformanceCounters = string.Empty;
                Properties.Settings.Default.PerformanceSummaryCols = string.Empty;
                Properties.Settings.Default.Save();
            }
        }

        private void standardColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            promptColumnSelection(ref dgv);
        }

        private void performanceCounterColumnsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new SelectPerformanceCounters
            {
                SelectedCounters = SelectedPerformanceCounters
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                SelectedPerformanceCounters = frm.SelectedCounters;
                RefreshData();
            }
        }
    }
}
