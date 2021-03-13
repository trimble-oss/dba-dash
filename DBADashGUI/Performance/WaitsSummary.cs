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
    public partial class WaitsSummary : UserControl
    {
        public WaitsSummary()
        {
            InitializeComponent();
        }

        public int InstanceID { get; set; }

        public void RefreshData()
        {
            var dt = GetWaitsSummaryDT();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;


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

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && e.ColumnIndex == colHelp.Index)
            {
                string wait = (string)dgv[colWaitType.Index, e.RowIndex].Value;
                System.Diagnostics.Process.Start("https://www.sqlskills.com/help/waits/" + wait.ToLower() + "/");
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            colHelp.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colHelp.Visible = true;
        }
    }
}
