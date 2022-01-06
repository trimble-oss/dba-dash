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
using System.Diagnostics;

namespace DBADashGUI.Performance
{
    public partial class WaitsSummary : UserControl
    {
        public WaitsSummary()
        {
            InitializeComponent();
        }

        public int InstanceID { get; set; }
        string selectedWaitType;

        private int dateGrouping = 1;
        
        public int DateGrouping
        {
            get
            {
                return dateGrouping;
            }
            set
            {
                dateGrouping = value;
                tsDateGroup.Text = Common.DateGroupString(value);
            }
        }



        public void RefreshData()
        {
            DateGrouping= Common.DateGrouping(DateRange.DurationMins, 300);
            refreshData();
        }

        private void refreshData()
        {
            var dt = GetWaitsSummaryDT();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            refreshChart();
        }

        public DataTable GetWaitsSummaryDT()
        {
            using (SqlConnection cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(new SqlCommand("dbo.WaitsSummary_Get", cn) { CommandType = CommandType.StoredProcedure }))
                {
                    da.SelectCommand.Parameters.AddWithValue("InstanceID", InstanceID);
                    da.SelectCommand.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    da.SelectCommand.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }


        public DataTable GetWaitsDT(string waitType)
        {
            using (SqlConnection cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(new SqlCommand("dbo.Waits_Get", cn) { CommandType = CommandType.StoredProcedure }))
                {
                    da.SelectCommand.Parameters.AddWithValue("InstanceID", InstanceID);
                    da.SelectCommand.Parameters.AddWithValue("FromDate", DateRange.FromUTC);
                    da.SelectCommand.Parameters.AddWithValue("ToDate", DateRange.ToUTC);
                    da.SelectCommand.Parameters.AddWithValue("WaitType", waitType);
                    da.SelectCommand.Parameters.AddWithValue("DateGroupingMin", DateGrouping);
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && e.ColumnIndex == colHelp.Index)
            {
                string wait = (string)dgv[colWaitType.Index, e.RowIndex].Value;
                var psi = new ProcessStartInfo("https://www.sqlskills.com/help/waits/" + wait.ToLower() + "/") { UseShellExecute = true };
                Process.Start(psi);
            }
            else if(e.RowIndex>=0 && e.ColumnIndex == colWaitType.Index)
            {
                selectedWaitType = (string)dgv[colWaitType.Index, e.RowIndex].Value;
                refreshChart();

            }
        }

        readonly Dictionary<string, columnMetaData> columns = new Dictionary<string, columnMetaData>
            {
                {"WaitTimeMsPerSec", new columnMetaData{Alias="Wait Time (ms/sec)",isVisible=true } }
            };

        private void refreshChart()
        {
            if (selectedWaitType == null || selectedWaitType == String.Empty)
            {
                splitContainer1.Panel1Collapsed = true;
            }
            else
            {
                tsWaitType.Text = selectedWaitType;
                splitContainer1.Panel1Collapsed = false;
                var dt = GetWaitsDT(selectedWaitType);
                WaitChart1.Series.Clear();
                WaitChart1.AddDataTable(dt, columns, "time", true);
                WaitChart1.AxisX[0].MinValue = DateRange.FromUTC.ToLocalTime().Ticks;
                WaitChart1.AxisX[0].MaxValue = DateRange.ToUTC.ToLocalTime().Ticks;
                WaitChart1.AxisY[0].MinValue = 0;
            }

        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            colHelp.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colHelp.Visible = true;
        }

        private void WaitsSummary_Load(object sender, EventArgs e)
        {
            Common.AddDateGroups(tsDateGroup, tsDateGroups_Click);
            addColumnsToMenu();
        }

        private void addColumnsToMenu()
        {
            foreach(DataGridViewColumn col in dgv.Columns)
            {
                var mnuCol = new ToolStripMenuItem { Text = col.HeaderText, Tag = col.Name,Checked =col.Visible, CheckOnClick=true };
                mnuCol.Click += MnuCol_Click;
                tsColumns.DropDownItems.Add(mnuCol);
            }
        }

        private void MnuCol_Click(object sender, EventArgs e)
        {
            var col = (ToolStripMenuItem)sender;
            dgv.Columns[(string)col.Tag].Visible = col.Checked;
        }

        private void tsDateGroups_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = (int)ts.Tag;
            refreshChart();
        }

        private void tsSmooth_Click(object sender, EventArgs e)
        {
            WaitChart1.DefaultLineSmoothness = Convert.ToDouble(((ToolStripMenuItem)sender).Tag);
        }

        private void PointSize_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            WaitChart1.SetPointSize(Convert.ToInt32(ts.Tag));
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }

   
    }
}
