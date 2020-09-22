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
    public partial class SQLPatching : UserControl
    {
        public SQLPatching()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;

        public void RefreshData()
        {
            refreshHistory();
            refreshVersion();
        }

        private void refreshVersion()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.InstanceVersionInfo_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgvVersion.AutoGenerateColumns = false;
                dgvVersion.DataSource = dt;
            }
        }

        private void refreshHistory()
        {
            SqlConnection cn = new SqlConnection(ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.SQLPatching_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;
            }
        }

        private void tsCopyVersion_Click(object sender, EventArgs e)
        {
            dgvVersion.SelectAll();
            DataObject dataObj = dgvVersion.GetClipboardContent();
            Clipboard.SetDataObject(dataObj, true);
        }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            dgv.SelectAll();
            DataObject dataObj = dgv.GetClipboardContent();
            Clipboard.SetDataObject(dataObj, true);
        }

        private void tsRefreshVersion_Click(object sender, EventArgs e)
        {
            refreshVersion();
        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }
    }
}
