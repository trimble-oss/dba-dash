using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionErrors : UserControl, ISetContext
    {
        public CollectionErrors()
        {
            InitializeComponent();
            Common.StyleGrid(ref dgvDBADashErrors);
            txtContext.BackColor = DashColors.BluePale;
            txtMessage.BackColor = DashColors.BluePale;
            txtSource.BackColor = DashColors.BluePale;
            SetFilterHighlight(txtSource, sourceToolStripMenuItem);
            SetFilterHighlight(txtContext, contextToolStripMenuItem);
            SetFilterHighlight(txtMessage, messageToolStripMenuItem);
            SetFilterHighlight(txtInstance, instanceToolStripMenuItem);
        }

        private Int32 InstanceID { get; set; }
        private string InstanceGroupName { get; set; }
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
                foreach (ToolStripMenuItem itm in tsErrorDays.DropDownItems)
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

        private static DataTable GetErrorLog(int instanceID, string instanceGroupName, string instanceDisplayName, string errorSource, string errorContext, string errorMessage, int Days)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddIfGreaterThanZero("InstanceID", instanceID);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", instanceGroupName);
                cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceDisplayName", instanceDisplayName);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ErrorSource", errorSource);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ErrorContext", errorContext);
                cmd.Parameters.AddStringIfNotNullOrEmpty("ErrorMessage", errorMessage);
                cmd.Parameters.AddWithValue("Days", Days);
                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        public void SetContext(DBADashContext context)
        {
            InstanceID = context.InstanceID;
            InstanceGroupName = context.InstanceName;
            Days = 1;
            RefreshData();
        }

        public void RefreshData()
        {
            ClearFilters();
            RefreshDataLocal();
        }

        private void RefreshDataLocal()
        {
            tsFilter.Font = IsFiltered ? new Font(tsFilter.Font, FontStyle.Bold) : new Font(tsFilter.Font, FontStyle.Regular);
            DataTable dt = GetErrorLog(InstanceID, InstanceGroupName, txtInstance.Text.Trim(), txtSource.Text.Trim(), txtContext.Text.Trim(), txtMessage.Text.Trim(), Days);
            dgvDBADashErrors.AutoGenerateColumns = false;
            dgvDBADashErrors.DataSource = dt;
            dgvDBADashErrors.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private bool IsFiltered
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

        private void TsErrorDays_Click(object sender, EventArgs e)
        {
            Days = int.Parse((string)((ToolStripMenuItem)sender).Tag);
            RefreshDataLocal();
        }

        private void TsCopyErrors_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvDBADashErrors);
        }

        private void TsRefreshErrors_Click(object sender, EventArgs e)
        {
            RefreshDataLocal();
        }

        private void TsAckErrors_Click(object sender, EventArgs e)
        {
            AcknowledgeErrors();
        }

        private static void AcknowledgeErrors()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.AcknowledgeErrors", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Errors up to current date acknowledged and cleared from summary page.", "Errors acknowledged", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvDBADashErrors);
        }

        private void DgvDBADashErrors_CellContentClick(object sender, DataGridViewCellEventArgs e)
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
                    RefreshDataLocal();
                }
                else if (e.ColumnIndex == ErrorContext.Index)
                {
                    txtContext.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    RefreshDataLocal();
                }
                else if (e.ColumnIndex == Instance.Index)
                {
                    txtInstance.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    RefreshDataLocal();
                }
            }
        }

        private void ClearFilters()
        {
            txtSource.Text = String.Empty;
            txtMessage.Text = String.Empty;
            txtContext.Text = String.Empty;
            txtInstance.Text = String.Empty;
        }

        private void TsClearFilters_Click(object sender, EventArgs e)
        {
            ClearFilters();
            RefreshDataLocal();
        }

        private void TxtSource_TextChanged(object sender, EventArgs e)
        {
            SetFilterHighlight(txtSource, sourceToolStripMenuItem);
        }

        private void TxtContext_TextChanged(object sender, EventArgs e)
        {
            SetFilterHighlight(txtContext, contextToolStripMenuItem);
        }

        private void TxtMessage_TextChanged(object sender, EventArgs e)
        {
            SetFilterHighlight(txtMessage, messageToolStripMenuItem);
        }

        private void TxtInstance_TextChanged(object sender, EventArgs e)
        {
            SetFilterHighlight(txtInstance, instanceToolStripMenuItem);
        }

        private void Filter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                RefreshDataLocal();
            }
        }

        private static void SetFilterHighlight(ToolStripTextBox txt, ToolStripMenuItem lbl)
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