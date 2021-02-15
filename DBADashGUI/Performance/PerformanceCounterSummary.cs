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
    public partial class PerformanceCounterSummary : UserControl
    {
        public PerformanceCounterSummary()
        {
            InitializeComponent();
        }

        public Int32 InstanceID { get; set; }

        Int32 mins = 60;
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
            refreshSummary();
            refreshChart();
        }

        private void refreshSummary()
        {
            var cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.PerformanceCounterSummary_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                    cn.Open();
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    cmd.Parameters.AddWithValue("FromDate", fromDate);
                    cmd.Parameters.AddWithValue("ToDate", toDate);
                    if (txtSearch.Text.Length > 0)
                    {
                        cmd.Parameters.AddWithValue("Search", "%" + txtSearch.Text + "%");
                    }                   
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();                   
                    da.Fill(dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = dt;
                }
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
                performanceCounters1.FromDate = fromDate;
                performanceCounters1.ToDate = toDate;
                performanceCounters1.InstanceID = InstanceID;
                performanceCounters1.RefreshData();
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
                        tsTime.Text = tsmi.Text;
                    }
                }
            }
        }


        private void tsTime_Click(object sender, EventArgs e)
        {
            var itm = (ToolStripMenuItem)sender;
            mins = Int32.Parse((string)itm.Tag);
            _from = DateTime.MinValue;
            _to = DateTime.MinValue;
            refreshSummary();
            refreshChart();
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
                tsTime.Text = "Custom";
                checkTime();
                RefreshData();
                tsCustom.Checked = true;
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
            splitContainer1.Panel1Collapsed = true;
            checkTime();
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
    }
}
