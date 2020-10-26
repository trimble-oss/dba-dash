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

namespace DBAChecksGUI.Changes
{
    public partial class AzureServiceObjectivesHistory : UserControl
    {
        public AzureServiceObjectivesHistory()
        {
            InitializeComponent();
        }

        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                SqlCommand cmd = new SqlCommand("dbo.AzureServiceObjectivesHistory_Get", cn);
                cmd.Parameters.AddWithValue("InstanceIDs" ,string.Join(",", InstanceIDs));
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
            }
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }
    }
}
