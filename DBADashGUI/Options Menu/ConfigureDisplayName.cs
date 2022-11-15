using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

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

        public int EditCount
        {
            get
            {
                return _editCount;
            }
        }
        private int _editCount = 0;

        private void ConfigureDisplayName_Load(object sender, EventArgs e)
        {
            SetCols();
            RefreshData();
        }

        private void SetCols()
        {
            dgv.AutoGenerateColumns = false;
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn() { Name = "colConnectionID", DataPropertyName = "ConnectionID", HeaderText = "ConnectionID", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle() { BackColor = DashColors.GrayLight } },
                new DataGridViewTextBoxColumn() { DataPropertyName = "Instance", HeaderText = "Instance", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle() { BackColor = DashColors.GrayLight } },
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
                    UpdateAlias(instanceID, ref alias);
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

        private static void UpdateAlias(int instanceID, ref string alias)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceAlias_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                cmd.Parameters.AddWithValue("Alias", alias);
                var pInstanceDisplayName = new SqlParameter("InstanceDisplayName", SqlDbType.NVarChar, 128) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pInstanceDisplayName);
                cmd.ExecuteNonQuery();
                alias = (string)pInstanceDisplayName.Value; // Returns the display name (set to ConnectionID if alias is NULL)
            }
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Highlight displayname column if it's been edited from the default value
            if (e.ColumnIndex == 2)
            {
                if (Convert.ToString(dgv.Rows[e.RowIndex].Cells[0].Value) == Convert.ToString(dgv.Rows[e.RowIndex].Cells[2].Value))
                {
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Regular);
                }
                else
                {
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                }
            }
        }

        private void ConfigureDisplayName_FormClosing(object sender, FormClosingEventArgs e)
        {
            dgv.EndEdit();
        }
    }
}
