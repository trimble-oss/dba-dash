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
using DBADashGUI.Changes;

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

        private DataTable getAzureDBPerformanceSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.AzureDBPerformanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                if (InstanceIDs.Count > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                }
                bool histogram = hasHistograms(ref dgv);
                cmd.Parameters.AddWithValue("CPUHist", histogram);
                cmd.Parameters.AddWithValue("DataHist", histogram);
                cmd.Parameters.AddWithValue("LogHist", histogram);
                cmd.Parameters.AddWithValue("DTUHist", histogram);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void refreshDB()
        {
            var dt = getAzureDBPerformanceSummary();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = new DataView(dt);
            generateHistogram(dgv);
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

        }

        private void refreshPool()
        {
            var dt = getAzureDBPoolSummary();
            dgvPool.AutoGenerateColumns = false;
            dgvPool.DataSource = new DataView(dt);
            generateHistogram(dgvPool);
            dgvPool.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);                    
        }

        private DataTable getAzureDBPoolSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.AzureDBPoolSummary_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                if (InstanceIDs.Count > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                }
                bool histogram = hasHistograms(ref dgvPool);
                cmd.Parameters.AddWithValue("CPUHist", histogram);
                cmd.Parameters.AddWithValue("DataHist", histogram);
                cmd.Parameters.AddWithValue("LogHist", histogram);
                cmd.Parameters.AddWithValue("DTUHist", histogram);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        readonly string[] histograms = new string[] { "DTU", "CPU", "Data", "Log" };

        private void generateHistogram(DataGridView gv)
        {
            string colPrefix = gv.Name + "_";
            foreach(string histogram in histograms)
            {
                string colName = colPrefix + histogram + "Histogram";
                if (gv.Rows.Count > 0 && gv.Rows[0].Cells[colName].Visible && gv.Rows[0].Cells[colName].Value == null)
                {
                    foreach (DataGridViewRow r in gv.Rows)
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


        private void AzureSummary_Load(object sender, EventArgs e)
        {
            addHistCols(dgv);
            addHistCols(dgvPool);
        }

        private void addHistCols(DataGridView gv)
        {
            foreach(string histogram in histograms)
            {

                for (int i = 10; i <= 100; i += 10)
                {
                    var col = new DataGridViewTextBoxColumn()
                    {
                        Name = gv.Name + "_" + histogram + "Histogram_" + i,
                        DataPropertyName = histogram + i.ToString(),
                        Visible = false,
                        HeaderText = histogram + " Histogram " + (i - 10).ToString() + " to " + i.ToString() + "%"
                    };
                    gv.Columns.Add(col);
                }
            }
        
        }


        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshDB();
        }


        private void tsCopy_Click(object sender, EventArgs e)
        {
            exportDGV(ExportTarget.Clipboard);
        }

        private void tsCopyPool_Click(object sender, EventArgs e)
        {
            exportDGVPool(ExportTarget.Clipboard);
        }

        private void tsRefreshPool_Click(object sender, EventArgs e)
        {
            refreshPool();
        }

        private void dgv_Sorted(object sender, EventArgs e)
        {
            generateHistogram(dgv);
        }

        private void dgvPool_Sorted(object sender, EventArgs e)
        {
            generateHistogram(dgvPool);
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
            else if(e.RowIndex>=0 && col == colServiceObjective)
            {
                DataRowView row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var frm = new ResourceGovernanceViewer() { InstanceID = (Int32)row["InstanceID"], DatabaseName= (string)row["DB"] };
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

        private void tsExcel_Click(object sender, EventArgs e)
        {
            exportDGV(ExportTarget.Excel);
        }

        private void tsExcelPool_Click(object sender, EventArgs e)
        {
            exportDGVPool(ExportTarget.Excel);
        }

        private enum ExportTarget
        {
            Clipboard,
            Excel
        }

        private void exportDGV(ExportTarget target)
        {
            var visibleCols = (new DataGridViewColumn[] { dgv_CPUHistogram, dgv_DataHistogram, dgv_DTUHistogram, dgv_LogHistogram }).Where(col => col.Visible == true).ToList();

            foreach (var c in visibleCols)
            {
                c.Visible = false;
            }
            if (target == ExportTarget.Clipboard)
            {
                Common.CopyDataGridViewToClipboard(dgv);
            }
            else if (target == ExportTarget.Excel)
            {
                Common.PromptSaveDataGridView(ref dgv);
            }
            foreach (var c in visibleCols)
            {
                c.Visible = true;
            }
        }

        private void exportDGVPool(ExportTarget target)
        {
            var visibleCols = (new DataGridViewColumn[] { dgvPool_CPUHistogram, dgvPool_DataHistogram, dgvPool_DTUHistogram, dgvPool_LogHistogram }).Where(col => col.Visible == true).ToList();

            foreach (var c in visibleCols)
            {
                c.Visible = false;
            }
            if (target == ExportTarget.Clipboard)
            {
                Common.CopyDataGridViewToClipboard(dgvPool);
            }
            else if (target == ExportTarget.Excel)
            {
                Common.PromptSaveDataGridView(ref dgvPool);
            }
            foreach (var c in visibleCols)
            {
                c.Visible = true;
            }

        }

        private Color getStatusColour(object value)
        {
            var status = (DBADashStatus.DBADashStatusEnum)Convert.ToInt32(value == DBNull.Value ? 3 : value);
            return DBADashStatus.GetStatusColour(status);
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;        

                dgv.Rows[idx].Cells["colAlocatedPctMax"].Style.BackColor = getStatusColour(row["PctMaxSizeStatus"]);
                dgv.Rows[idx].Cells["colFileSnapshotAge"].Style.BackColor = getStatusColour(row["FileSnapshotStatus"]);

            }
        }

        private bool hasHistograms(ref DataGridView gv)
        {
            return gv.Columns.Cast<DataGridViewColumn>().Where(col => col.Name.Contains("Histogram") && col.Visible).Any();
        }

        private void updateHistogramsIfNeeded(ref DataGridView gv)
        {
            DataView dv = (DataView)gv.DataSource;

            if (hasHistograms(ref gv))
            {
                foreach (string histogram in histograms)
                {
                    if (!dv.Table.Columns.Contains(histogram + "10"))
                    {
                        if(gv == dgvPool)
                        {
                            refreshPool();
                        }
                        else
                        {
                            refreshDB();
                        }
                                              
                        return;
                    }
                }
                generateHistogram(gv);
            }
        }

        private void promptColumnSelection(ref DataGridView gv)
        {
            using (var frm = new SelectColumns())
            {
                frm.Columns = gv.Columns;
                frm.ShowDialog(this);
                if (frm.DialogResult == DialogResult.OK)
                {
                    updateHistogramsIfNeeded(ref gv);
                    gv.AutoResizeColumns();
                    gv.AutoResizeRows();
                }
            }
        }

        private void tsCols_Click(object sender, EventArgs e)
        {          
             promptColumnSelection(ref dgv);                    
        }

        private void tsPoolCols_Click(object sender, EventArgs e)
        {
            promptColumnSelection(ref dgvPool);
        }
    }
}
