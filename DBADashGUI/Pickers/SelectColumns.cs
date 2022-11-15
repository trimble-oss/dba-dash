using System;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class SelectColumns : Form
    {
        public SelectColumns()
        {
            InitializeComponent();
        }

        public DataGridViewColumnCollection Columns;

        private DataTable dtCols;

        private void SelectColumns_Load(object sender, EventArgs e)
        {
            dgvCols.AutoGenerateColumns = false;
            dtCols = ColumnsToDataTable(Columns);
            dgvCols.DataSource = dtCols;
        }

        private static DataTable ColumnsToDataTable(DataGridViewColumnCollection Columns)
        {
            var dtCols = new DataTable();
            dtCols.Columns.Add("ColumnName", typeof(string));
            dtCols.Columns.Add("IsVisible", typeof(bool));
            dtCols.Columns.Add("Index", typeof(int));
            foreach (DataGridViewColumn col in Columns)
            {
                dtCols.Rows.Add(new object[] { col.HeaderText, col.Visible, col.Index });
            }
            return dtCols;

        }

        private void ToggleIsVisible(bool isVisible)
        {
            foreach (DataRow row in dtCols.Rows)
            {
                row["IsVisible"] = isVisible;
            }
        }

        private void LnkAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ToggleIsVisible(Visible);
        }

        private void LnkNone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ToggleIsVisible(false);
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            foreach (DataRow r in dtCols.Rows)
            {
                Columns[(int)r["Index"]].Visible = (bool)r["IsVisible"];
            }
            this.DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void ToggleSelected()
        {
            if (dgvCols.SelectedRows.Count > 0)
            {
                bool isVisible = !((bool)((DataRowView)dgvCols.SelectedRows[0].DataBoundItem)["IsVisible"]);
                foreach (DataGridViewRow row in dgvCols.SelectedRows)
                {
                    var r = (DataRowView)row.DataBoundItem;
                    r["IsVisible"] = isVisible;
                }
                dgvCols.Refresh();
            }
        }

        private void LnkSelected_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ToggleSelected();
        }

        private void DgvCols_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                ToggleSelected();
            }
        }
    }
}
