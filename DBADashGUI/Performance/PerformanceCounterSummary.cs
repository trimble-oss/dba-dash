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
    public partial class PerformanceCounterSummary : UserControl
    {
        public PerformanceCounterSummary()
        {
            InitializeComponent();
        }

        public Int32 InstanceID { get; set; }

        public void RefreshData()
        {
            refreshSummary();
            refreshChart();
        }

        private void refreshSummary()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure }) 
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                cmd.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                if (txtSearch.Text.Length > 0)
                {
                    cmd.Parameters.AddWithValue("Search", "%" + txtSearch.Text + "%");
                }                   
                DataTable dt = new DataTable();                   
                da.Fill(dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }         
        }


        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshSummary();
            refreshChart();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void refreshChart()
        {
            if (!splitContainer1.Panel1Collapsed)
            {
                performanceCounters1.FromDate = DateRange.FromUTC;
                performanceCounters1.ToDate = DateRange.ToUTC;
                performanceCounters1.InstanceID = InstanceID;
                performanceCounters1.RefreshData();
            }
        }


        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var objectName = (string)row["object_name"];
                var counterName = (string)row["counter_name"];
                var instanceName = (string)row["instance_name"];

                if (e.ColumnIndex == colView.Index) {
                    performanceCounters1.CounterID = (Int32)row["CounterID"];
                    performanceCounters1.CounterName = objectName + "\\" + counterName + (instanceName == "" ? "" : "\\" + instanceName);
                    splitContainer1.Panel1Collapsed = false;
                    refreshChart();
                }
                if (e.ColumnIndex == colCounter.Index)
                {
                    txtSearch.Text = counterName;
                    refreshSummary();
                }
                if (e.ColumnIndex == colInstance.Index)
                {
                    txtSearch.Text = instanceName;
                    refreshSummary();
                }
                if (e.ColumnIndex == colObject.Index)
                {
                    txtSearch.Text = objectName;
                    refreshSummary();
                }
            }
        }

      
        private void PerformanceCounterSummary_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgv);
            splitContainer1.Panel1Collapsed = true;
        }

        private void tsClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            refreshSummary();
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)13)
            {
                refreshSummary();
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }
}
