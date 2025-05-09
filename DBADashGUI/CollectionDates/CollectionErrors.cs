using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionErrors : UserControl, ISetContext
    {
        public CollectionErrors()
        {
            InitializeComponent();
            dgvDBADashErrors.ApplyTheme();
            txtContext.BackColor = DashColors.BluePale;
            txtMessage.BackColor = DashColors.BluePale;
            txtSource.BackColor = DashColors.BluePale;
            SetFilterHighlight(txtSource, sourceToolStripMenuItem);
            SetFilterHighlight(txtContext, contextToolStripMenuItem);
            SetFilterHighlight(txtMessage, messageToolStripMenuItem);
            SetFilterHighlight(txtInstance, instanceToolStripMenuItem);
        }

        private DBADashContext Context;

        private int InstanceID => Context.InstanceID;
        private string InstanceGroupName => Context.InstanceName;
        private int _days;

        public int Days
        {
            get => _days;
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
            get => tsAckErrors.Visible;
            set => tsAckErrors.Visible = value;
        }

        private static DataTable GetErrorLog(int instanceID, string instanceGroupName, string instanceDisplayName, string errorSource, string errorContext, string errorMessage, int Days, HashSet<int> instanceIds)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CollectionErrorLog_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddIfGreaterThanZero("InstanceID", instanceID);
            cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceGroupName", instanceGroupName);
            cmd.Parameters.AddStringIfNotNullOrEmpty("InstanceDisplayName", instanceDisplayName);
            cmd.Parameters.AddStringIfNotNullOrEmpty("ErrorSource", errorSource);
            cmd.Parameters.AddStringIfNotNullOrEmpty("ErrorContext", errorContext);
            cmd.Parameters.AddStringIfNotNullOrEmpty("ErrorMessage", errorMessage);
            cmd.Parameters.AddWithValue("InstanceIDs", instanceIds.AsDataTable());
            cmd.Parameters.AddWithValue("Days", Days);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        public void SetContext(DBADashContext _context)
        {
            Context = _context;
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
            DataTable dt = GetErrorLog(InstanceID, InstanceGroupName, txtInstance.Text.Trim(), txtSource.Text.Trim(), txtContext.Text.Trim(), txtMessage.Text.Trim(), Days, Context.TreeLevel == 1 && Context.Type == SQLTreeItem.TreeType.DBAChecks ? new HashSet<int>() : Context.InstanceIDs);
            dgvDBADashErrors.AutoGenerateColumns = false;
            dgvDBADashErrors.DataSource = new DataView(dt);
            dgvDBADashErrors.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private bool IsFiltered =>
            !string.IsNullOrWhiteSpace(txtSource.Text) ||
            !string.IsNullOrWhiteSpace(txtContext.Text) ||
            !string.IsNullOrWhiteSpace(txtMessage.Text) ||
            !string.IsNullOrWhiteSpace(txtInstance.Text);

        private void TsErrorDays_Click(object sender, EventArgs e)
        {
            Days = int.Parse(((string)((ToolStripMenuItem)sender).Tag)!);
            RefreshDataLocal();
        }

        private void TsCopyErrors_Click(object sender, EventArgs e)
        {
            dgvDBADashErrors.CopyGrid();
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
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.AcknowledgeErrors", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.ExecuteNonQuery();
            MessageBox.Show("Errors up to current date acknowledged and cleared from summary page.", "Errors acknowledged", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvDBADashErrors.ExportToExcel();
        }

        private void DgvDBADashErrors_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == ErrorMessage.Index)
            {
                Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value).OpenAsTextFile();
            }
            else if (e.ColumnIndex == ErrorSource.Index)
            {
                txtSource.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? string.Empty;
                RefreshDataLocal();
            }
            else if (e.ColumnIndex == ErrorContext.Index)
            {
                txtContext.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? string.Empty;
                RefreshDataLocal();
            }
            else if (e.ColumnIndex == Instance.Index)
            {
                txtInstance.Text = Convert.ToString(dgvDBADashErrors.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? string.Empty;
                RefreshDataLocal();
            }
        }

        private void ClearFilters()
        {
            txtSource.Text = string.Empty;
            txtMessage.Text = string.Empty;
            txtContext.Text = string.Empty;
            txtInstance.Text = string.Empty;
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
            lbl.Font = !string.IsNullOrEmpty(txt.Text.Trim()) ? new Font(lbl.Font, FontStyle.Bold) : new Font(lbl.Font, FontStyle.Regular);
        }
    }
}