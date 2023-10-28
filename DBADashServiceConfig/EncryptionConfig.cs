using DBADash;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashServiceConfig
{
    public partial class EncryptionConfig : Form
    {
        public EncryptionConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public BasicConfig.EncryptionOptions EncryptionOption
        {
            get => chkEncrypt.Checked ? BasicConfig.EncryptionOptions.Encrypt : BasicConfig.EncryptionOptions.Basic;
            set => chkEncrypt.Checked = value == BasicConfig.EncryptionOptions.Encrypt;
        }

        public string EncryptionPassword
        {
            get => txtPassword.Text.Trim();
            set => txtPassword.Text = value;
        }

        private bool PasswordIsVisible
        {
            get => txtPassword.PasswordChar == '\0';
            set
            {
                txtPassword.PasswordChar = value ? '\0' : '*';
                lnkShowHide.Text = value ? "Hide" : "Show";
            }
        }

        private void LnkGenerate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtPassword.Text = PasswordGenerator.Generate(20, true, true, true);
            PasswordIsVisible = true;
        }

        private void LnkShowHide_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PasswordIsVisible = !PasswordIsVisible;
        }

        private void ChkEncrypt_CheckedChanged(object sender, EventArgs e)
        {
            grpEncryption.Enabled = chkEncrypt.Checked;
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            if (chkEncrypt.Checked && string.IsNullOrEmpty(EncryptionPassword))
            {
                MessageBox.Show("Password is required", "Password", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            else
            {
                DialogResult = DialogResult.OK;
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}