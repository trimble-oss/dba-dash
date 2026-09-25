using DBADashGUI.Pickers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DBADashGUI.Theme;
using Control = System.Windows.Forms.Control;

namespace DBADashGUI
{
    public partial class SelectColumns : Form, IThemedControl
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

            foreach (var item in items)
            {
                dtItems.Rows.Add(item.Name.Replace("\n", " "), item.IsVisible);
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
            DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
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

        public void ApplyTheme(BaseTheme theme)
        {
            foreach (Control control in Controls)
            {
                control.ApplyTheme(theme);
            }
            ForeColor = theme.ForegroundColor;
            BackColor = theme.BackgroundColor;
        }
    }
}