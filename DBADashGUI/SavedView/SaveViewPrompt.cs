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
    public partial class SaveViewPrompt : Form
    {
        public SaveViewPrompt()
        {
            InitializeComponent();
        }

        public string ViewName { get => txtName.Text;set=>txtName.Text = value;  }

        public bool IsGlobal { get=>optEveryone.Checked; set => optEveryone.Checked = value; }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            chkDefault.Checked = txtName.Text == "Default";           
        }

        private void chkDefault_CheckedChanged(object sender, EventArgs e)
        {
            txtName.Text = "Default";
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void bttnSave_Click(object sender, EventArgs e)
        {
            if (txtName.Text.Length == 0)
            {
                MessageBox.Show("Invalid Name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            this.DialogResult = DialogResult.OK;
        }

        private void SaveViewPrompt_Load(object sender, EventArgs e)
        {
            optEveryone.Enabled = DBADashUser.HasManageGlobalViews;
        }
    }
}
