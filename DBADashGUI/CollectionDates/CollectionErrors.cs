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

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionErrors : UserControl
    {
        public CollectionErrors()
        {
            InitializeComponent();
            Common.StyleGrid(ref dgvDBADashErrors);
            txtContext.BackColor = DashColors.BluePale;
            txtMessage.BackColor = DashColors.BluePale;
            txtSource.BackColor = DashColors.BluePale;
            setFilterHighlight(txtSource, sourceToolStripMenuItem);
            setFilterHighlight(txtContext, contextToolStripMenuItem);
            setFilterHighlight(txtMessage, messageToolStripMenuItem);
            setFilterHighlight(txtInstance, instanceToolStripMenuItem);
        }

        public Int32 InstanceID { get; set; }
        public string InstanceGroupName { get; set; }
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

        private static DataTable getErrorLog(int instanceID,string instanceGroupName,string instanceDisplayName,string errorSource,string errorContext,string errorMessage,int Days)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                if (instanceID > 0)
                {
                    cmd.Parameters.AddWithValue("InstanceID", instanceID);
                }
                if (!String.IsNullOrEmpty(instanceGroupName))
                {
                    cmd.Parameters.AddWithValue("InstanceGroupName", instanceGroupName);
                }
                if (!String.IsNullOrEmpty(instanceDisplayName))
                {
                    cmd.Parameters.AddWithValue("InstanceDisplayName", instanceDisplayName);
                }
                if (!String.IsNullOrEmpty(errorSource))
                {
                    cmd.Parameters.AddWithValue("ErrorSource", errorSource);
                }
                if (!String.IsNullOrEmpty(errorContext))
                {
                    cmd.Parameters.AddWithValue("ErrorContext", errorContext);
                }
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    cmd.Parameters.AddWithValue("ErrorMessage", errorMessage);
                }
                cmd.Parameters.AddWithValue("Days", Days);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        public void RefreshData()
        {
            clearFilters();
            refreshData();           
        }

        private void refreshData()
        {
            tsFilter.Font = isFiltered ? new Font(tsFilter.Font, FontStyle.Bold) : new Font(tsFilter.Font, FontStyle.Regular);
            DataTable dt = getErrorLog(InstanceID, InstanceGroupName, txtInstance.Text.Trim(), txtSource.Text.Trim(), txtContext.Text.Trim(), txtMessage.Text.Trim(), Days);
            dgvDBADashErrors.AutoGenerateColumns = false;
            dgvDBADashErrors.DataSource = dt;
            dgvDBADashErrors.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private bool isFiltered
        {
            get
            {
                if (!String.IsNullOrEmpty(txtSource.Text.Trim()))
                {
                    return true;
                }
                if (!String.IsNullOrEmpty(txtContext.Text.Trim()))
                {
                    return true;
                }
                if (!String.IsNullOrEmpty(txtMessage.Text.Trim()))
                {
                    return true;
                }
                if (!String.IsNullOrEmpty(txtInstance.Text.Trim()))
                {
                    return true;
                }
                return false;
            }
        }

        private void tsErrorDays_Click(object sender, EventArgs e)
        {
            Days = int.Parse((string)((ToolStripMenuItem)sender).Tag);
            refreshData();
        }

        private void tsCopyErrors_Click(object sender, EventArgs e)
        {
             Common.CopyDataGridViewToClipboard(dgvDBADashErrors);
        }

        private void tsRefreshErrors_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void tsAckErrors_Click(object sender, EventArgs e)
        {
            AcknowledgeErrors();
        }

        private void AcknowledgeErrors()
        {
            using(var cn = new SqlConnection(Common.ConnectionString))
            using(var cmd = new SqlCommand("dbo.AcknowledgeErrors", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();                          
                cmd.ExecuteNonQuery();
                MessageBox.Show("Errors up to current date acknowledged and cleared from summary page.", "Errors acknowledged", MessageBoxButtons.OK, MessageBoxIcon.Information);               
            }
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvDBADashErrors);
        }

        private void dgvDBADashErrors_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == ErrorMessage.Index)
                {
                    string message = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    NotepadHelper.ShowMessage(message, "DBA Dash Error");
                }
                else if (e.ColumnIndex == ErrorSource.Index)
                {
                    txtSource.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    refreshData();
                }
                else if (e.ColumnIndex== ErrorContext.Index)
                {
                    txtContext.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    refreshData();
                }
                else if (e.ColumnIndex== Instance.Index)
                {
                    txtInstance.Text= Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    refreshData();
                }
            }
        }

        private void clearFilters()
        {
            txtSource.Text = String.Empty;
            txtMessage.Text = String.Empty;
            txtContext.Text = String.Empty;
            txtInstance.Text=String.Empty;
        }

        private void tsClearFilters_Click(object sender, EventArgs e)
        {
            clearFilters();
            refreshData();
        }

        private void txtSource_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtSource, sourceToolStripMenuItem);
        }

        private void txtContext_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtContext, contextToolStripMenuItem);
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtMessage, messageToolStripMenuItem);
        }
        private void txtInstance_TextChanged(object sender, EventArgs e)
        {
            setFilterHighlight(txtInstance, instanceToolStripMenuItem);
        }

        private void Filter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                refreshData();
            }
        }

        private void setFilterHighlight(ToolStripTextBox txt, ToolStripMenuItem lbl)
        {
            if (!String.IsNullOrEmpty(txt.Text.Trim()))
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            }
            else
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Regular);
            }
        }
    }
}
