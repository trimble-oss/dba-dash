using System;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class SaveViewPrompt : Form
    {
        public SaveViewPrompt()
        {
            InitializeComponent();
        }

        public string ViewName { get => txtName.Text; set => txtName.Text = value; }

        public bool IsGlobal { get => optEveryone.Checked; set => optEveryone.Checked = value; }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            chkDefault.Checked = txtName.Text == "Default";
        }

        private void ChkDefault_CheckedChanged(object sender, EventArgs e)
        {
            txtName.Text = "Default";
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void BttnSave_Click(object sender, EventArgs e)
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
