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
    public partial class AzureSummary : UserControl
    {
        public AzureSummary()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;


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
                using (SqlCommand cmd = new SqlCommand("dbo.AzureDBPerformanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                    cn.Open();

                    if (InstanceIDs.Count > 0)
                    {
                        cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    }
                    cmd.Parameters.AddWithValue("CPUHist", colCPUHistogram.Visible);
                    cmd.Parameters.AddWithValue("DataHist", colDataHistogram.Visible);
                    cmd.Parameters.AddWithValue("LogHist", colLogHistogram.Visible);
                    cmd.Parameters.AddWithValue("DTUHist", colDTUHistogram.Visible);
                    cmd.Parameters.AddWithValue("FromDate",  DateRange.FromUTC);
                    cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = new DataView(dt);
                    generateHistogram(dgv);
                }
            }
        }

        private void refreshPool()
        {

            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.AzureDBPoolSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();

                    if (InstanceIDs.Count > 0)
                    {
                        cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    }
                    cmd.Parameters.AddWithValue("CPUHist", colPoolCPUHistogram.Visible);
                    cmd.Parameters.AddWithValue("DataHist", colPoolDataHistogram.Visible);
                    cmd.Parameters.AddWithValue("LogHist", colPoolLogHistogram.Visible);
                    cmd.Parameters.AddWithValue("DTUHist", colPoolDTUHistogram.Visible);
                    cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvPool.AutoGenerateColumns = false;
                    dgvPool.DataSource = new DataView(dt);
                    generateHistogram(dgvPool, "colPool");
                }
            }

        }

        readonly string[] histograms = new string[] { "DTU", "CPU", "Data", "Log" };

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
                ToolStripMenuItem mnu = new ToolStripMenuItem(col.HeaderText)
                {
                    Name = col.Name,
                    CheckOnClick=true,
                    Checked = col.Visible,                 
                };
                mnu.Click += ColumnMenu_Click;
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
                ToolStripMenuItem mnu = new ToolStripMenuItem(col.HeaderText)
                {
                    Name = col.Name,
                    Checked = col.Visible,
                    CheckOnClick = true
                };
                mnu.Click += PoolColumnMenu_Click;             
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
            addColumnsMenu();
            addPoolColumnsMenu();
            addHistCols(dgv,"col");
            addHistCols(dgvPool, "colPool");

        }

        private void addHistCols(DataGridView dgv,string prefix)
        {
            foreach(string histogram in histograms)
            {

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
        
        }



        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshDB();
        }



        private void tsCopy_Click(object sender, EventArgs e)
        {
            var visibleCols = (new DataGridViewColumn[] { colCPUHistogram, colDataHistogram, colDTUHistogram, colLogHistogram }).Where(col => col.Visible == true).ToList();
            
            foreach(var c in visibleCols)
            {
                c.Visible = false;
                for(int i = 10; i <= 100; i += 10)
                {
                    dgv.Columns[c.Name + "_" + i.ToString()].Visible = true;
                }
            }
            Common.CopyDataGridViewToClipboard(dgv);
            foreach (var c in visibleCols)
            {
                c.Visible = true;
                for (int i = 10; i <= 100; i += 10)
                {
                    dgv.Columns[c.Name + "_" + i.ToString()].Visible = false;
                }
            }

        }

        private void tsCopyPool_Click(object sender, EventArgs e)
        {
            var visibleCols = (new DataGridViewColumn[] { colPoolCPUHistogram, colPoolDataHistogram, colPoolDTUHistogram, colPoolLogHistogram }).Where(col => col.Visible == true).ToList();

            foreach (var c in visibleCols)
            {
                c.Visible = false;
                for (int i = 10; i <= 100; i += 10)
                {
                    dgvPool.Columns[c.Name + "_" + i.ToString()].Visible = true;
                }
            }
            Common.CopyDataGridViewToClipboard(dgvPool);
            foreach (var c in visibleCols)
            {
                c.Visible = true;
                for (int i = 10; i <= 100; i += 10)
                {
                    dgvPool.Columns[c.Name + "_" + i.ToString()].Visible = false;
                }
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

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var col = dgv.Columns[e.ColumnIndex];
            if (e.RowIndex>=0 && (col == colDB || col==colElasticPool))
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                string instance =(string)row["Instance"];
                string db= (string)row["DB"];

                var frm = new AzureDBResourceStatsView
                {
                    FromDate = DateRange.FromUTC,
                    ToDate = DateRange.ToUTC,
                    InstanceID = (Int32)row["InstanceID"]
                };
                if (col == colElasticPool)
                {
                    string pool = (string)row["elastic_pool_name"];
                    frm.ElasticPoolName = pool;
                    frm.Text = instance + " | " + pool;
                }
                else
                {
                    frm.Text = instance + " | " + db;
                }
     
                frm.Show();
            }
            
        }

        private void dgvPool_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvPool.Columns[e.ColumnIndex] == colPoolName)
            {
                DataRowView row = (DataRowView)dgvPool.Rows[e.RowIndex].DataBoundItem;
                string instance = (string)row["Instance"];
                string pool = (string)row["elastic_pool_name"];
                var frm = new AzureDBResourceStatsView
                {
                    FromDate = DateRange.FromUTC,
                    ToDate = DateRange.ToUTC,
                    InstanceID = (Int32)row["InstanceID"],
                    ElasticPoolName = pool,
                    Text = instance + " | " + pool
                };
                frm.Show();
            }
        }

        private void dgvPol_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgvPool.Rows[idx].DataBoundItem;
                var poolStorageStatus = (DBADashStatus.DBADashStatusEnum)row["ElasticPoolStorageStatus"];
                dgvPool.Rows[idx].Cells[colAllocatedStoragePct.Index].Style.BackColor = DBADashStatus.GetStatusColour(poolStorageStatus);
            }
        }
    }
}
