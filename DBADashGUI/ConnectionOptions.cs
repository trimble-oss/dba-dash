using System;
using System.Windows.Forms;
namespace DBADashGUI
{
    public partial class ConnectionOptions : Form
    {
        public ConnectionOptions()
        {
            InitializeComponent();
        }

        public bool RequestConfigureService => optConfigure.Checked;

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult= DialogResult.OK;
        }

        private void ConnectionOptions_Load(object sender, EventArgs e)
        {
       
        }
    }
}
