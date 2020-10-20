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

namespace DBAChecksGUI.Performance
{
    public partial class AzureSummary : UserControl
    {
        public AzureSummary()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;



        Int32 mins = 15;
        private DateTime _from = DateTime.MinValue;
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
            if (Common.ConnectionString != null)
            {
                refreshDB();
                refreshPool();
            }
        }

        private void refreshDB()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.AzureDBPerformanceSummary_Get", cn);
                if (InstanceIDs.Count > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                }
                cmd.Parameters.AddWithValue("CPUHist", colCPUHistogram.Visible);
                cmd.Parameters.AddWithValue("DataHist", colDataHistogram.Visible);
                cmd.Parameters.AddWithValue("LogHist", colLogHistogram.Visible);
                cmd.Parameters.AddWithValue("DTUHist", colDTUHistogram.Visible);
                cmd.Parameters.AddWithValue("FromDate", fromDate);
                cmd.Parameters.AddWithValue("ToDate", toDate);
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = new DataView(dt);
                generateHistogram(dgv);
            }
        }

        private void refreshPool()
        {

            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.AzureDBPoolSummary_Get", cn);
                if (InstanceIDs.Count > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                }
                cmd.Parameters.AddWithValue("CPUHist", colPoolCPUHistogram.Visible);
                cmd.Parameters.AddWithValue("DataHist", colPoolDataHistogram.Visible);
                cmd.Parameters.AddWithValue("LogHist", colPoolLogHistogram.Visible);
                cmd.Parameters.AddWithValue("DTUHist", colPoolDTUHistogram.Visible);
                cmd.Parameters.AddWithValue("FromDate", fromDate);
                cmd.Parameters.AddWithValue("ToDate", toDate);
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvPool.AutoGenerateColumns = false;
                dgvPool.DataSource = new DataView(dt);
                generateHistogram(dgvPool, "colPool");
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
                }
            }
        }

        string[] histograms = new string[] { "DTU", "CPU", "Data", "Log" };

        private void generateHistogram(DataGridView dgv,string colPrefix="col" )
        {
      
            foreach(string histogram in histograms)
            {
                string colName = colPrefix + histogram + "Histogram";
                if (dgv.Rows.Count > 0 && dgv.Rows[0].Cells[colName].Visible && dgv.Rows[0].Cells[colName].Value == null)
                {
                    foreach (DataGridViewRow r in dgv.Rows)
                    {
                        DataRowView row = (DataRowView)r.DataBoundItem;
                        StringBuilder sbToolTip = new StringBuilder();
                        var hist = new List<double>();
                        double total = 0;

                        for (Int32 i = 10; i <= 100; i += 10)
                        {
                            var v = Convert.ToDouble(row[histogram + i.ToString()]);
                            hist.Add(v);
                            total += v;
                        }
                        
                        if (total == 0)
                        {
                            r.Cells[colName].Value = new Bitmap(1, 1);
                        }
                        else
                        {
                            for (Int32 i = 10; i <= 100; i += 10)
                            {
                                var v = Convert.ToDouble(row[histogram + i.ToString()]);
                                sbToolTip.AppendLine((i - 10).ToString() + " to " + i.ToString() + "% | " + v.ToString("N0") + " (" + (v/total).ToString("P2") + ")")  ;
                            }
                            r.Cells[colName].Value = Histogram.GetHistogram(hist, 200, 100, true);
                            r.Height = 100;
                            r.Cells[colName].ToolTipText = sbToolTip.ToString();
                        }

                    }
                }
            }       
        }

        private void addColumnsMenu()
        {
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                ToolStripMenuItem mnu = new ToolStripMenuItem(col.HeaderText);
                mnu.Name = col.Name;
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

        private void addPoolColumnsMenu()
        {
            foreach (DataGridViewColumn col in dgvPool.Columns)
            {
                ToolStripMenuItem mnu = new ToolStripMenuItem(col.HeaderText);
                mnu.Name = col.Name;
                mnu.Click += PoolColumnMenu_Click;
                mnu.Checked = col.Visible;
                mnu.CheckOnClick = true;
                tsPoolColumns.DropDownItems.Add(mnu);
            }
            tsPoolColumns.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem mnuCheckAll = new ToolStripMenuItem("Check All");
            mnuCheckAll.Click += MnuPoolCheckAll_Click;
            tsPoolColumns.DropDownItems.Add(mnuCheckAll);
            ToolStripMenuItem mnuUnCheckAll = new ToolStripMenuItem("Uncheck All");
            mnuUnCheckAll.Click += MnuPoolUnCheckAll_Click;
            tsPoolColumns.DropDownItems.Add(mnuUnCheckAll);
        }



        private void MnuPoolUnCheckAll_Click(object sender, EventArgs e)
        {
            checkAll(false, ref tsPoolColumns, ref dgvPool);
        }

        private void MnuPoolCheckAll_Click(object sender, EventArgs e)
        {
            checkAll(true, ref tsPoolColumns, ref dgvPool);
            DataView dv = (DataView)dgvPool.DataSource;
            foreach (string histogram in histograms)
            {
                if (!dv.Table.Columns.Contains(histogram + "10"))
                {
                    refreshPool();
                    return;
                }
            }
            generateHistogram(dgvPool, "colPool");
        }

        private void MnuUnCheckAll_Click(object sender, EventArgs e)
        {
            checkAll(false,ref tsColumns,ref dgv);
        }

        private void checkAll(bool isChecked,ref ToolStripDropDownButton tsColumns, ref DataGridView dgv)
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
            checkAll(true,ref tsColumns,ref dgv);
            DataView dv = (DataView)dgv.DataSource;

            foreach (string histogram in histograms)
            {
                if (!dv.Table.Columns.Contains(histogram + "10"))
                {
                    refreshDB() ;
                    return;
                }                           
            }
            generateHistogram(dgv, "col");

        }

        private void PoolColumnMenu_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            dgvPool.Columns[mnu.Name].Visible = mnu.Checked;
            if (mnu.Checked && mnu.Text.Contains("Histogram"))
            {
                string histogram = mnu.Name.Replace("colPool", "").Replace("Histogram", "");
                DataView dv = (DataView)dgvPool.DataSource;
                if (!dv.Table.Columns.Contains(histogram + "10"))
                {
                    refreshPool();
                }
                else
                {
                    generateHistogram(dgvPool, "colPool");
                }
            }
        }

        private void ColumnMenu_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            dgv.Columns[mnu.Name].Visible = mnu.Checked;
            if(mnu.Checked && mnu.Text.Contains("Histogram"))
            {
                string histogram = mnu.Name.Replace("col", "").Replace("Histogram", "");
                DataView dv = (DataView)dgv.DataSource;
                if (!dv.Table.Columns.Contains(histogram + "10"))
                {
                    refreshDB();
                }
                else
                {
                    generateHistogram(dgv, "col");
                }
            }
        }

        private void AzureSummary_Load(object sender, EventArgs e)
        {
            checkTime();
            addColumnsMenu();
            addPoolColumnsMenu();
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
            refreshDB();
        }

        private void tsCustom_Click(object sender, EventArgs e)
        {
            var frm = new CustomTimePicker();
            frm.FromDate = fromDate;
            frm.ToDate = toDate;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                _from = frm.FromDate;
                _to = frm.ToDate;
                mins = 0;
                checkTime();
            }
            RefreshData();
            tsCustom.Checked = true;
        }


        private void tsCopy_Click(object sender, EventArgs e)
        {
            var visibleCols = (new DataGridViewColumn[] { colCPUHistogram, colDataHistogram, colDTUHistogram, colLogHistogram }).Where(col => col.Visible == true).ToList();
            
            foreach(var c in visibleCols)
            {
                c.Visible = false;
            }
            Common.CopyDataGridViewToClipboard(dgv);
            foreach (var c in visibleCols)
            {
                c.Visible = true;
            }

        }

        private void tsCopyPool_Click(object sender, EventArgs e)
        {
            var visibleCols = (new DataGridViewColumn[] { colPoolCPUHistogram, colPoolDataHistogram, colPoolDTUHistogram, colPoolLogHistogram }).Where(col => col.Visible == true).ToList();

            foreach (var c in visibleCols)
            {
                c.Visible = false;
            }
            Common.CopyDataGridViewToClipboard(dgvPool);
            foreach (var c in visibleCols)
            {
                c.Visible = true;
            }

        }

        private void tsRefreshPool_Click(object sender, EventArgs e)
        {
            refreshPool();
        }

        private void dgv_Sorted(object sender, EventArgs e)
        {
            generateHistogram(dgv, "col");
        }

        private void dgvPool_Sorted(object sender, EventArgs e)
        {
            generateHistogram(dgvPool, "colPool");
        }
    }
}
