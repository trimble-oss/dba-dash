using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class InstallService : Form
    {
        public InstallService()
        {
            InitializeComponent();
        }

        public string ServiceName { get; set; }

        private void installService()
        {
            Process p = new Process();
            var psi = new ProcessStartInfo()
            {
                FileName = "CMD.EXE"
            };
            string arg = "";
            switch (cboServiceCredentials.SelectedIndex)
            {
                case 0:
                    arg = "--localsystem";
                    break;
                case 1:
                    arg = "--localservice";
                    break;
                case 2:
                    arg = "--networkservice";
                    break;
                case 3:
                    arg = "--interactive";
                    break;

            }
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.Arguments = "/c DBADashService Install " + arg;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
           
            p.OutputDataReceived += (sender, args) => txtOutput.AppendText(args.Data + Environment.NewLine);
            p.StartInfo = psi;
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            System.Threading.Thread.Sleep(500);
        }

        private void bttnInstall_Click(object sender, EventArgs e)
        {
            txtOutput.BackColor = Color.Black;
            txtOutput.ForeColor = Color.White;

            installService();
            var svcCtrl = ServiceController.GetServices()
                            .FirstOrDefault(s => s.ServiceName == ServiceName);

            if (svcCtrl == null)
            {
                MessageBox.Show("Service did not install. Please review the log.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Service installation completed.  Please start the service to begin data collection.", "Service Install", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

        }

        private void InstallService_Load(object sender, EventArgs e)
        {
            cboServiceCredentials.SelectedIndex = 3;
        }
    }
}
