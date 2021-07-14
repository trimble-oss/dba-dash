using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class ManageInstances : Form
    {
        public ManageInstances()
        {
            InitializeComponent();
        }

        public string Tags { get; set; } = null;
        private bool activeFlagChanged = false;
        public bool InstanceActiveFlagChanged{
            get{
                return activeFlagChanged;
            }
        }

        private void ManageInstances_Load(object sender, EventArgs e)
        {
            refreshData();
        }

        void refreshData()
        {
            var dt = CommonData.GetInstances(Tags, null);
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;

        }

        private void dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {

            for (Int32 idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var r = dgv.Rows[idx];
                var row = (DataRowView)r.DataBoundItem;
                if (row != null)
                {
                    bool isActive = (bool)row["IsActive"];
                    dgv.Rows[idx].Cells[colDeleteRestore.Index].Value = isActive ? "Mark Deleted" : "Restore";
                }
            }
            
        }

        void MarkInstanceDeleted(int InstanceID, bool IsActive)
        {
            using (SqlConnection cn = new SqlConnection(Common.ConnectionString))
            {
                using(SqlCommand cmd = new SqlCommand("dbo.Instance_Del", cn) { CommandType= CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    cmd.Parameters.AddWithValue("IsActive", IsActive);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex>=0 && e.ColumnIndex== colDeleteRestore.Index)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var InstanceID =(Int32)row["InstanceID"];
                var isActive = (bool)row["IsActive"];
                isActive = !isActive;
                try
                {
                    MarkInstanceDeleted(InstanceID, isActive);
                    dgv.Rows[e.RowIndex].Cells[colDeleteRestore.Index].Value = isActive ? "Mark Deleted" : "Restore";
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    refreshData();
                }
                activeFlagChanged = true;
                
            }
        }
    }
}
