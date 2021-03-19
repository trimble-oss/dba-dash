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
using static DBADashGUI.Main;

namespace DBADashGUI.Performance
{
    public partial class PerformanceSummary : UserControl
    {

        public List<Int32> InstanceIDs;
        public string TagIDs;

        public Dictionary<int,Counter> SelectedPerformanceCounters =  new Dictionary<int, Counter>();

        public PerformanceSummary()
        {
            InitializeComponent();
        }

   
        public void RefreshData()
        {
            dgv.DataSource = null;
            var dt = getPerformanceSummary();
            addPerformanceCounters(ref dt);
            dgv.AutoGenerateColumns = false;
            generateHistogram(ref dt);
            if (dgv.DataSource == null)
            {
                dgv.DataSource = new DataView(dt);
            }
            
        }

        void addPerformanceCounters(ref DataTable dt)
        {
            List<string> colsToRemove = new List<string>();

            foreach(DataGridViewColumn col in dgv.Columns)
            {
                if ((string)col.Tag == "PC") {
                    colsToRemove.Add(col.Name);
              }
            }
            foreach(var col in colsToRemove)
            {
                dgv.Columns.Remove(col);
            }          
            foreach(var ctr in SelectedPerformanceCounters.Values)
            {
                foreach(string agg in ctr.GetAggColumns())
                {
                    dt.Columns.Add(agg + "_" + ctr.CounterID, typeof(double));
                    dgv.Columns.Add(new DataGridViewTextBoxColumn() { DataPropertyName = agg + "_" + ctr.CounterID, HeaderText = agg + " " + ctr.ToString(), Tag = "PC" });
                }
            }
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
                        }
                        if (cntr.Max)
                        {
                            mainRow[("Max_" + (int)r["CounterID"]).ToString()] = r["MaxValue"];
                        }
                        if (cntr.Total)
                        {
                            mainRow[("Total_" + (int)r["CounterID"]).ToString()] = r["TotalValue"];
                        }
                        if (cntr.Current)
                        {
                            mainRow[("Current_" + (int)r["CounterID"]).ToString()] = r["CurrentValue"];
                        }
                        if (cntr.Min)
                        {
                            mainRow[("Min_" + (int)r["CounterID"]).ToString()] = r["MinValue"];
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
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
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
                    var counters =String.Join(",", SelectedPerformanceCounters.Values.Select(pc => pc.CounterID));
                    cmd.Parameters.AddWithValue("Counters", counters);
                    cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }


        DataTable getPerformanceSummary()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.PerformanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
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
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    var pkCols = new DataColumn[1];
                    pkCols[0] =  dt.Columns.Add("InstanceID", typeof(int));
                    dt.PrimaryKey = pkCols;
                    da.Fill(dt);
                    return dt;
                }
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


        private void addColumnsMenu()
        {
            foreach(DataGridViewColumn col in dgv.Columns)
            {
                ToolStripMenuItem mnu = new ToolStripMenuItem(col.HeaderText)
                {
                    Name = col.Name,
                };
                mnu.Click += ColumnMenu_Click;
                mnu.Checked = col.Visible;
                mnu.CheckOnClick = true;
                tsColumns.DropDownItems.Add(mnu);
            }
            tsColumns.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem mnuCheckAll = new ToolStripMenuItem("Check All");
            mnuCheckAll.Click += MnuCheckAll_Click;
            tsColumns.DropDownItems.Add(mnuCheckAll);
            ToolStripMenuItem mnuUnCheckAll = new ToolStripMenuItem("Uncheck All");
            mnuUnCheckAll.Click += MnuUnCheckAll_Click;
            tsColumns.DropDownItems.Add(mnuUnCheckAll);
        }

        private void MnuUnCheckAll_Click(object sender, EventArgs e)
        {
            checkAll(false);
        }

        private void checkAll(bool isChecked)
        {
            foreach (ToolStripItem itm in tsColumns.DropDownItems)
            {
                if (itm.GetType() == typeof(ToolStripMenuItem))
                {
                    var mnu = (ToolStripMenuItem)itm;
                    if (mnu.CheckOnClick)
                    {
                        mnu.Checked = isChecked;
                        dgv.Columns[mnu.Name].Visible = isChecked;
                    }
                }
            }
        }

        private void MnuCheckAll_Click(object sender, EventArgs e)
        {
            checkAll(true);
            var dt = ((DataView)dgv.DataSource).Table;
            generateHistogram(ref dt);
        }

        private void ColumnMenu_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            dgv.Columns[mnu.Name].Visible = mnu.Checked;
            if (mnu.Name == "colCPUHistogram" && mnu.Checked)
            {
                var dt = ((DataView)dgv.DataSource).Table;
                generateHistogram(ref dt);
            }
        }

        private void PerformanceSummary_Load(object sender, EventArgs e)
        {
            addColumnsMenu();
            addHistCols(dgv, "col");
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

                r.Cells["ReadLatency"].Style.BackColor = DBADashStatus.GetStatusColour((DBADashStatus.DBADashStatusEnum)row["ReadLatencyStatus"]);
                r.Cells["WriteLatency"].Style.BackColor = DBADashStatus.GetStatusColour((DBADashStatus.DBADashStatusEnum)row["WriteLatencyStatus"]);
                r.Cells["CriticalWaitMsPerSec"].Style.BackColor = DBADashStatus.GetStatusColour((DBADashStatus.DBADashStatusEnum)row["CriticalWaitStatus"]);
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

        private void tsPerformanceCounters_Click(object sender, EventArgs e)
        {
            var frm = new SelectPerformanceCounters();
            frm.SelectedCounters = SelectedPerformanceCounters;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                SelectedPerformanceCounters = frm.SelectedCounters;
                RefreshData();
            }
        }
    }
}
