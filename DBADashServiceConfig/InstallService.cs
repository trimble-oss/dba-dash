using DBADash;
using DBADashGUI.Theme;
using Meziantou.Framework.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Windows.Forms;
using DBADashGUI;
using DBADashGUI.SchemaCompare;

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
                    username = "NT AUTHORITY\\System";
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
                        if (Environment.UserDomainName == Environment.MachineName)
                        {
                            domain = Environment.MachineName;
                        }
                        else
                        {
                            var localAccount =
                                new TaskDialogRadioButton($"Local account {Environment.MachineName}\\{creds.UserName}");
                            var domainAccount = new TaskDialogRadioButton($"Domain account {Environment.UserDomainName}\\{creds.UserName}") { Checked = true };
                            var page = new TaskDialogPage
                            {
                                Heading =
                                    $"A domain name hasn't been specified.  Is this a local or a domain user account?",
                                Caption = "Select account type",
                                Icon = TaskDialogIcon.None,
                                Buttons = new TaskDialogButtonCollection()
                                    { TaskDialogButton.OK, TaskDialogButton.Cancel },
                                SizeToContent = true,
                                RadioButtons = new TaskDialogRadioButtonCollection() { domainAccount, localAccount },
                            };
                            if (TaskDialog.ShowDialog(page) != TaskDialogButton.OK) return false;

                            domain = localAccount.Checked ? Environment.MachineName : Environment.UserDomainName;
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
                var result = ServiceTools.InstallService(ServiceName, username, password);
                txtOutput.AppendText(result.Output + Environment.NewLine);
            }
            catch (Exception ex)
            {
                txtOutput.AppendText(ex.Message + Environment.NewLine);
                return false;
            }

            try
            {
                GrantFullControlToUser(AppContext.BaseDirectory, username);
                txtOutput.AppendText($"Granted {username} access to {AppContext.BaseDirectory}");
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Error granting {username} access to {AppContext.BaseDirectory}\n{ex}");
            }

            return true;
        }

        private static void GrantFullControlToUser(string folderPath, string userName)
        {
            // Get the directory's access control list
            var dirInfo = new DirectoryInfo(folderPath);
            var dirSecurity = dirInfo.GetAccessControl();

            // Create a new access rule for the specified user
            var accessRule = new FileSystemAccessRule(
                userName,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            // Add the new rule to the directory's access control list
            dirSecurity.AddAccessRule(accessRule);

            // Apply the changes to the directory
            dirInfo.SetAccessControl(dirSecurity);
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
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void InstallService_Load(object sender, EventArgs e)
        {
            var args = ServiceTools.GetServiceInstallArgs(ServiceName, "YourDomain\\YourUser", "YourPassword");
            txtOutput.Text = txtOutput.Text.Replace("{CommandLine}", $"sc.exe {args}");
            cboServiceCredentials.SelectedIndex = 3;
        }

        private void CreateServiceAccountScript_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var script = ReadResourceString("CreateMSA.ps1");
            var frm = new CodeViewer() { Language = CodeEditor.CodeEditorModes.PowerShell, Code = script };
            frm.Show();
        }

        public string ReadResourceString(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}