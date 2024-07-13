using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class Info : UserControl, ISetContext
    {
        public Info()
        {
            InitializeComponent();
        }

        private int InstanceID;

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
            RefreshData();
        }

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

