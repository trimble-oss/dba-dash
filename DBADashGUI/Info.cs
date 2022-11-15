using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class Info : UserControl
    {
        public Info()
        {
            InitializeComponent();
        }

        public Int32 InstanceID;

        public void RefreshData()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceInfo_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                DataTable dt = new();
                da.Fill(dt);
                dgv.DataSource = dt;
            }
        }
    }
}

