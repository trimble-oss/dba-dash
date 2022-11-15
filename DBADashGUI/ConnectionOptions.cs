using DBADash;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
namespace DBADashGUI
{
    public partial class ConnectionOptions : Form
    {
        public ConnectionOptions()
        {
            InitializeComponent();
        }

        public BasicConfig cfg;

        private void BttnOK_Click(object sender, EventArgs e)
        {
            if (optConfigure.Checked)
            {
                ConfigureService();
            }
            else
            {
                Connect();
            }
        }

        private void Connect()
        {
            using (var frm = new DBConnection())
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {

                    try
                    {
                        DBValidations.GetDBVersion(frm.ConnectionString);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error validating connection to repository database:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    cfg = new BasicConfig
                    {
                        Destination = frm.ConnectionString
                    };

                    if (File.Exists(Common.JsonConfigPath))
                    {
                        MessageBox.Show("Config file already exists:" + Common.JsonConfigPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        System.IO.File.WriteAllText(Common.JsonConfigPath, cfg.Serialize());
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
        }


        private static void ConfigureService()
        {
            var psi = new ProcessStartInfo(Properties.Resources.ServiceConfigToolName) { UseShellExecute = true };
            Process.Start(psi);
            Application.Exit();
        }


        private void ConnectionOptions_Load(object sender, EventArgs e)
        {
            if (!File.Exists(Properties.Resources.ServiceConfigToolName))
            {
                optConfigure.Enabled = false;
                optConnect.Checked = true;
                Connect();
            }
        }
    }
}
