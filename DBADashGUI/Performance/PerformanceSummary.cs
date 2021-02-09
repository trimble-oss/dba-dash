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
        public string ConnectionString;
        public string TagIDs;

        public PerformanceSummary()
        {
            InitializeComponent();
        }

        Int32 mins=15;
        private DateTime _from=DateTime.MinValue;
        private DateTime _to = DateTime.MinValue;

        private DateTime fromDate
        {
            get
            {
                if (_from == DateTime.MinValue)
                {
                    DateTime now = DateTime.UtcNow;
                    if (mins >= 720)  // round to nearest hr
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, 0);
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

        public void RefreshData()
        {
            if (ConnectionString != null)
            {
                dgv.DataSource = null;
                SqlConnection cn = new SqlConnection(ConnectionString);
                using (cn)
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.PerformanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                        cn.Open();
                        if (InstanceIDs.Count > 0)
                        {
                            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("TagIDs", TagIDs);
                        }
                        cmd.Parameters.AddWithValue("FromDate", fromDate);
                        cmd.Parameters.AddWithValue("ToDate", toDate);
                        cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dgv.AutoGenerateColumns = false;
                        generateHistogram(ref dt);
                        if (dgv.DataSource == null)
                        {
                            dgv.DataSource = new DataView(dt);
                        }
                    }
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

        private void checkTime()
        {
            foreach (var ts in tsTime.DropDownItems)
            {
                if (ts.GetType() == typeof(ToolStripMenuItem))
                {
                    var tsmi = (ToolStripMenuItem)ts;
                    tsmi.Checked = Int32.Parse((string)tsmi.Tag)== mins;
                    if (tsmi.Checked)
                    {
                        tsTime.Text = tsmi.Text;
                    }
                }
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
            checkTime();
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

        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            mins = Int32.Parse((string)itm.Tag);
            _from = DateTime.MinValue;
            _to = DateTime.MinValue;
            RefreshData();
            checkTime();
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCustom_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker
            {
                FromDate = fromDate.ToLocalTime(),
                ToDate = toDate.ToLocalTime()
            };
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                _from = frm.FromDate.ToUniversalTime();
                _to = frm.ToDate.ToUniversalTime();
                tsTime.Text = "Custom";
                mins = 0;
                checkTime();
                RefreshData();
                tsCustom.Checked = true;
            }

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


    }
}
