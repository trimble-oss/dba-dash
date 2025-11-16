using DBADashGUI.Theme;
using Quartz.Util;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class AWSCreds : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string AWSProfile
        {
            get => txtAWSProfile.Text.TrimEmptyToNull();
            set => txtAWSProfile.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string AWSAccessKey
        {
            get => txtAccessKey.Text.TrimEmptyToNull();
            set => txtAccessKey.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string AWSSecretKey
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