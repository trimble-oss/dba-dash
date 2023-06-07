using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}