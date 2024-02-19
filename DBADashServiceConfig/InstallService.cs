using Meziantou.Framework.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;
using DBADash;
using DBADashGUI.Theme;
using Microsoft.SqlServer.TransactSql.ScriptDom;

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
            string username = null;
            string password = null;
            switch (cboServiceCredentials.SelectedIndex)
            {
                case 0: // LocalSystem (default)
                    break;

                case 1:
                    username = "NT AUTHORITY\\LocalService";
                    break;

                case 2:
                    username = "NT AUTHORITY\\NetworkService";
                    break;

                case 3:
                    // Note: --interactive doesn't work on .NET 6 without a target OS.  Prompt user and pass as commandline arguments.
                    var creds = CredentialManager.PromptForCredentials(
                    captionText: ServiceName,
                    messageText: "Please enter the credentials to run the DBA Dash service.\nNote: Check the security requirements for the service account in the application documentation.\nEnter username in domain\\username format",
                    saveCredential: CredentialSaveOption.Hidden
                    );
                    if (creds == null)
                    {
                        return false;
                    }
                    var domain = creds.Domain;

                    if (string.IsNullOrEmpty(domain) && !creds.UserName.StartsWith(".\\"))
                    {
                        var input = MessageBox.Show(
                            $"Warning domain hasn't been specified.  Is this a local user account?\n\nSelect Yes to use {Environment.MachineName} (local) \nSelect No to use {Environment.UserDomainName} (domain)", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                        switch (input)
                        {
                            case DialogResult.Yes:
                                domain = Environment.MachineName;
                                break;

                            case DialogResult.No:
                                domain = Environment.UserDomainName;
                                break;

                            default:
                                return false;
                        }
                    }
                    username = domain + "\\" + creds.UserName;
                    if (string.IsNullOrEmpty(domain)) // UserName specified as .\User
                    {
                        username = creds.UserName;
                    }
                    password = creds.Password;

                    break;
            }

            try
            {
                var result = ServiceTools.InstallService(ServiceName, username, password,
                    ServiceTools.StartMode.AutomaticDelayedStart);
                txtOutput.AppendText(result.Output + Environment.NewLine);
            }
            catch (Exception ex)
            {
                txtOutput.AppendText(ex.Message + Environment.NewLine);
                return false;
            }

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
            var args = ServiceTools.GetServiceInstallArgs(ServiceName, "YourDomain\\YourUser", "YourPassword",
                ServiceTools.StartMode.AutomaticDelayedStart);
            txtOutput.Text = txtOutput.Text.Replace("{CommandLine}", $"sc.exe {args}");
            cboServiceCredentials.SelectedIndex = 3;
        }

        private void LnkPermissions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DBADashSharedGUI.CommonShared.OpenURL("https://dbadash.com/docs/help/security/");
        }
    }
}