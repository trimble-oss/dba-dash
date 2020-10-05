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
                SqlConnection cn = new SqlConnection(ConnectionString);
                using (cn)
                {
                    cn.Open();
                    SqlCommand cmd = new SqlCommand("dbo.PerformanceSummary_Get", cn);
                    if (InstanceIDs.Count > 0)
                    {
                        cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("TagIDs", TagIDs);
                    }
                    cmd.Parameters.AddWithValue("FromDate", fromDate);
                    cmd.Parameters.AddWithValue("ToDate",toDate);
                    cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = new DataView(dt);
                }
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
                }
            }
        }

        private void addColumnsMenu()
        {
            foreach(DataGridViewColumn col in dgv.Columns)
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
        }

        private void ColumnMenu_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            dgv.Columns[mnu.Name].Visible = mnu.Checked;
        }

        private void PerformanceSummary_Load(object sender, EventArgs e)
        {
            checkTime();
            addColumnsMenu();
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
            var frm = new CustomTimePicker();
            frm.FromDate = fromDate;
            frm.ToDate = toDate;
            frm.ShowDialog();
            if(frm.DialogResult == DialogResult.OK)
            {
                _from = frm.FromDate;
                _to = frm.ToDate;
                mins = 0;
                checkTime();
            }
            RefreshData();
            tsCustom.Checked = true;
        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var row = (DataRowView)dgv.Rows[idx].DataBoundItem;
               

                dgv.Rows[idx].Cells["AvgCPU"].Style.BackColor = DBAChecksStatus.GetStatusColour((DBAChecksStatus.DBAChecksStatusEnum)row["AvgCPUStatus"]);
                dgv.Rows[idx].Cells["ReadLatency"].Style.BackColor = DBAChecksStatus.GetStatusColour((DBAChecksStatus.DBAChecksStatusEnum)row["ReadLatencyStatus"]);
                dgv.Rows[idx].Cells["WriteLatency"].Style.BackColor = DBAChecksStatus.GetStatusColour((DBAChecksStatus.DBAChecksStatusEnum)row["WriteLatencyStatus"]);
                dgv.Rows[idx].Cells["CriticalWaitMsPerSec"].Style.BackColor = DBAChecksStatus.GetStatusColour((DBAChecksStatus.DBAChecksStatusEnum)row["CriticalWaitStatus"]);
            }
             
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }
    }
}
