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

namespace DBAChecksGUI
{
    public partial class HardwareChanges : UserControl
    {
        public HardwareChanges()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.HostUpgradeHistory_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",",InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
            }
        }
    }
}
