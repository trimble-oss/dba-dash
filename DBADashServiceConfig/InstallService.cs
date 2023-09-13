using Meziantou.Framework.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashServiceConfig
{
    public partial class InstallService : Form
    {
        public InstallService()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public string ServiceName { get; set; }

        private bool InstallDBADashService()
        {
            Process p = new();
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
                    // Note: --interactive doesn't work on .NET 6 without a target OS.  Prompt user and pass as commandline arguments.
                    var creds = CredentialManager.PromptForCredentials(
                    captionText: ServiceName,
                    messageText: "Please enter the credentials to run the DBA Dash service.\nNote: Check the security requirements for the service account in the applicaton documentation.\nEnter username in domain\\username format",
                    saveCredential: CredentialSaveOption.Hidden
                    );
                    if (creds == null)
                    {
                        return false;
                    }
                    string domain = creds.Domain;

                    if (String.IsNullOrEmpty(domain) && !creds.UserName.StartsWith(".\\"))
                    {
                        var input = MessageBox.Show(String.Format("Warning domain hasn't been specified.  Is this a local user account?\n\nSelect Yes to use {0} (local) \nSelect No to use {1} (domain)", Environment.MachineName, Environment.UserDomainName), "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                        if (input == DialogResult.Yes)
                        {
                            domain = Environment.MachineName;
                        }
                        else if (input == DialogResult.No)
                        {
                            domain = Environment.UserDomainName;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    string username = domain + "\\" + creds.UserName;
                    if (String.IsNullOrEmpty(domain)) // UserName specified as .\User
                    {
                        username = creds.UserName;
                    }

                    arg = "-username \"" + username + "\" -password \"" + creds.Password.Replace("\"", "\"\"") + "\"";

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
            return true;
        }

        private void BttnInstall_Click(object sender, EventArgs e)
        {
            txtOutput.BackColor = Color.Black;
            txtOutput.ForeColor = Color.White;

            if (InstallDBADashService())
            {
                CheckServiceInstalledAndClose();
            }
            else
            {
                txtOutput.BackColor = Color.White;
                txtOutput.ForeColor = Color.Black;
            }
        }

        private void CheckServiceInstalledAndClose()
        {
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

        private void LnkPermissions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DBADashSharedGUI.CommonShared.OpenURL("https://dbadash.com/docs/help/security/");
        }
    }
}