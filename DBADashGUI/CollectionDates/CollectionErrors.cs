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

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionErrors : UserControl
    {
        public CollectionErrors()
        {
            InitializeComponent();
        }

        public Int32 InstanceID { get; set; }
        public string InstanceName { get; set; }
        private int _days;
        public Int32 Days
        {
            get
            {
                return _days;
            }
            set
            {
                _days = value;
                 foreach(ToolStripMenuItem itm in tsErrorDays.DropDownItems)
                {
                    itm.Checked = ((string)itm.Tag == _days.ToString());
                }
            }
        }

        public bool AckErrors
        {
            get
            {
                return tsAckErrors.Visible;
            }
            set
            {
                tsAckErrors.Visible = value;
            }
        }

        public void RefreshData()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    if (InstanceID > 0)
                    {
                        cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                    }
                    if (InstanceName != "")
                    {
                        cmd.Parameters.AddWithValue("Instance", InstanceName);
                    }
                    cmd.Parameters.AddWithValue("Days", Days);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgvDBADashErrors.AutoGenerateColumns = false;
                    dgvDBADashErrors.DataSource = dt;
                }
            }

        }

        private void tsErrorDays_Click(object sender, EventArgs e)
        {
            Days = int.Parse((string)((ToolStripMenuItem)sender).Tag);
            RefreshData();
        }

        private void tsCopyErrors_Click(object sender, EventArgs e)
        {
             Common.CopyDataGridViewToClipboard(dgvDBADashErrors);
        }

        private void tsRefreshErrors_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void tsAckErrors_Click(object sender, EventArgs e)
        {
            AcknowledgeErrors();
        }

        private void AcknowledgeErrors()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            {
                cn.Open();
                using (var cmd = new SqlCommand("dbo.AcknowledgeErrors", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Errors up to current date acknowledged and cleared from summary page.", "Errors acknowledged", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
