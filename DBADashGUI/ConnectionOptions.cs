using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADash;
namespace DBADashGUI
{
    public partial class ConnectionOptions : Form
    {
        public ConnectionOptions()
        {
            InitializeComponent();
        }

        public BasicConfig cfg;

        private void bttnOK_Click(object sender, EventArgs e)
        {
            if (optConfigure.Checked)
            {
                configureService();
            }
            else
            {
                connect();
            }
        }

        private void connect()
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
                    catch(Exception ex)
                    {
                        MessageBox.Show("Error validating connection to repository database:" + ex.Message,"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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


        private void configureService()
        {
            Process.Start(Properties.Resources.ServiceConfigToolName);
            Application.Exit();
        }
        

        private void ConnectionOptions_Load(object sender, EventArgs e)
        {
            if (!File.Exists(Properties.Resources.ServiceConfigToolName))
            {
                optConfigure.Enabled = false;
                optConnect.Checked = true;
                connect();
            }
        }
    }
}
