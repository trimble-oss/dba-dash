using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            dtCols = columnsToDataTable(Columns);
            dgvCols.DataSource = dtCols;
        }

        private static DataTable columnsToDataTable(DataGridViewColumnCollection Columns) 
        {
            var dtCols = new DataTable();
            dtCols.Columns.Add( "ColumnName", typeof(string) );
            dtCols.Columns.Add("IsVisible",typeof(bool) );
            dtCols.Columns.Add("Index",typeof(int) );   
            foreach (DataGridViewColumn col in Columns)
            {
                dtCols.Rows.Add(new object[] { col.HeaderText, col.Visible,col.Index });
            }
            return dtCols;
         
        }

        private void toggleIsVisible(bool isVisible)
        {
            foreach(DataRow row in dtCols.Rows)
            {
                row["IsVisible"] = isVisible;
            }
        }

        private void lnkAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            toggleIsVisible(Visible);
        }

        private void lnkNone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            toggleIsVisible(false);
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            foreach(DataRow r in dtCols.Rows)
            {
                Columns[(int)r["Index"]].Visible = (bool)r["IsVisible"];
            }
            this.DialogResult = DialogResult.OK;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult= DialogResult.Cancel;
        }

        private void toggleSelected()
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

        private void lnkSelected_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            toggleSelected();
        }

        private void dgvCols_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar==' ')
            {
                toggleSelected();
            }
        }
    }
}
