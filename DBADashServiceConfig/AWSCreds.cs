using System;
using System.Windows.Forms;
using DBADashGUI.Theme;
using Quartz.Util;

namespace DBADashServiceConfig
{
    public partial class AWSCreds : Form
    {
        public string AWSProfile
        {
            get => txtAWSProfile.Text.TrimEmptyToNull();
            set => txtAWSProfile.Text = value;
        }

        public string AWSAccessKey
        {
            get => txtAccessKey.Text.TrimEmptyToNull();
            set => txtAccessKey.Text = value;
        }

        public string AWSSecretKet
        {
            get => txtSecretKey.Text.TrimEmptyToNull();
            set => txtSecretKey.Text = value;
        }

        public AWSCreds()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}