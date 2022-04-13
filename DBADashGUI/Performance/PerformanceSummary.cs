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
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            dgv.Columns["colCPUHistogram"].Width = 200;
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

        private void tsPerformanceCounters_Click(object sender, EventArgs e)
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

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
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
                    generateHistogram(ref dt);
                    gv.AutoResizeColumns();
                    gv.AutoResizeRows();
                }
            }
        }
    }
}
