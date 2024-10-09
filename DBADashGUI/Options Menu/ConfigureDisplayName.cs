using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DBADash;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class ConfigureDisplayName : Form
    {
        public ConfigureDisplayName()
        {
            InitializeComponent();
        }

        public string TagIDs { get; set; }
        public string SearchString { get; set; }

        public int EditCount => _editCount;
        private int _editCount;

        private void ConfigureDisplayName_Load(object sender, EventArgs e)
        {
            this.ApplyTheme();
            SetCols();
            RefreshData();
        }

        private void SetCols()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { Name = "colConnectionID", DataPropertyName = "ConnectionID", HeaderText = "ConnectionID", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle() { BackColor = DashColors.GrayLight, ForeColor = Color.Black } },
                new DataGridViewTextBoxColumn() { DataPropertyName = "Instance", HeaderText = "Instance", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle() { BackColor = DashColors.GrayLight, ForeColor = Color.Black } },
                new DataGridViewTextBoxColumn() { Name = "colDisplayName", DataPropertyName = "InstanceDisplayName", HeaderText = "Display Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
            );
        }

        private void RefreshData()
        {
            DataTable dt = CommonData.GetInstances(TagIDs, true, false, SearchString);
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["colDisplayName"].Index)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                int instanceID = (int)row["InstanceID"];
                var alias = Convert.ToString(dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                try
                {
                    SharedData.UpdateAlias(instanceID, ref alias,Common.ConnectionString);
                    row["InstanceDisplayName"] = alias;
                    row.EndEdit();
                    _editCount++; // Keep track of edits made so we can decide to refresh the tree
                }
                catch (SqlException ex) when (ex.Number == 2601)
                {
                    MessageBox.Show("A server with the specified alias already exists.  Please enter a unique alias.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    row.CancelEdit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating alias: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    row.CancelEdit();
                }
            }
        }


        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Highlight display name column if it's been edited from the default value
            if (e.ColumnIndex == 2)
            {
                e.CellStyle.Font = Convert.ToString(dgv.Rows[e.RowIndex].Cells[0].Value) == Convert.ToString(dgv.Rows[e.RowIndex].Cells[2].Value) ? new Font(e.CellStyle.Font, FontStyle.Regular) : new Font(e.CellStyle.Font, FontStyle.Bold);
            }
        }

        private void ConfigureDisplayName_FormClosing(object sender, FormClosingEventArgs e)
        {
            dgv.EndEdit();
        }
    }
}