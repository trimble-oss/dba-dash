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

namespace DBADashGUI
{
    public partial class ConfigurationHistory : UserControl
    {
        public ConfigurationHistory()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            configuration1.ConnectionString = this.ConnectionString;
            configuration1.InstanceIDs = this.InstanceIDs;
            configuration1.RefreshData();
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.SysConfigHistory_Get", cn){CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();

                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = dt;
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
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

        private void configuration1_Load(object sender, EventArgs e)
        {

        }
    }
}
