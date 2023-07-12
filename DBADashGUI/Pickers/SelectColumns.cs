using DBADashGUI.Pickers;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
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

        public List<ISelectable> Items;

        private DataTable dtItems;

        private void SelectColumns_Load(object sender, EventArgs e)
        {
            dgvCols.AutoGenerateColumns = false;
            dtItems = ItemsToDataTable(Items);
            dgvCols.DataSource = dtItems;
        }

        private static DataTable ItemsToDataTable(List<ISelectable> items)
        {
            var dtItems = new DataTable();
            dtItems.Columns.Add("Name", typeof(string));
            dtItems.Columns.Add("IsVisible", typeof(bool));

            foreach (ISelectable item in items)
            {
                dtItems.Rows.Add(new object[] { item.Name, item.IsVisible });
            }
            return dtItems;
        }

        private void ToggleIsVisible(bool isVisible)
        {
            foreach (DataRow row in dtItems.Rows)
            {
                row["IsVisible"] = isVisible;
            }
        }

        private void LnkAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ToggleIsVisible(true);
        }

        private void LnkNone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ToggleIsVisible(false);
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dtItems.Rows.Count; i++)
            {
                Items[i].IsVisible = (bool)dtItems.Rows[i]["IsVisible"];
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

        protected void DgvCols_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                ToggleSelected();
            }
        }
    }

}
