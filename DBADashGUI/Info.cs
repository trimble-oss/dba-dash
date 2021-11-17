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

namespace DBADashGUI
{
    public partial class Info : UserControl
    {
        public Info()
        {
            InitializeComponent();
        }

        public Int32 InstanceID;
        public string ConnectionString;

        public void RefreshData()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InstanceInfo_Get", cn) {CommandType = CommandType.StoredProcedure}) {
                    cn.Open();
                  
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgv.DataSource = dt;
                }
            }
        }
    }
}
