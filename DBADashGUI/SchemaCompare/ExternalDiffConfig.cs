using DBADashGUI.Theme;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace DBADashGUI.SchemaCompare
{
    public partial class ExternalDiffConfig : Form
    {
        private enum DiffTools
        {
            None,
            Custom,
            WinMerge,
            VSCode
        }

        public ExternalDiffConfig()
        {
            InitializeComponent();
            cboDiffTool.DataSource = Enum.GetValues(typeof(DiffTools));
            cboDiffTool.DropDownStyle = ComboBoxStyle.DropDownList;
            this.ApplyTheme();
        }

        private static string GetWinMergePath()
        {
            // Registry path for 64-bit installations
            const string keyPath64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinMergeU.exe";
            // Registry path for 32-bit installations on a 64-bit system
            const string keyPath32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\WinMergeU.exe";
            const string path64 = @"C:\Program Files\WinMerge\WinMergeU.exe";
            const string path32 = @"C:\Program Files (x86)\WinMerge\WinMergeU.exe";

            try
            {
                // Try to open the registry key (64-bit)
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath64))
                {
                    if (key != null)
                    {
                        // If the key exists, retrieve the default value which should be the installation path
                        return key.GetValue("") as string;
                    }
                }

                // If not found, try to open the registry key for 32-bit installations
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath32))
                {
                    if (key != null)
                    {
                        // If the key exists, retrieve the default value which should be the installation path
                        return key.GetValue("") as string;
                    }
                }
            }
            catch
            {
                // Shouldn't generate an error, but if it does, continue and check default paths
            }

            // Try default paths
            if (!File.Exists(path64) && File.Exists(path32))
            {
                return path32;
            }

            return path64;
        }

        private static string GetVSCodePath()
        {
            // Possible default paths for VSCode installations
            var userPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                           @"\Programs\Microsoft VS Code\code.exe";
            const string systemPath = @"C:\Program Files\Microsoft VS Code\code.exe";
            const string defaultPath = "code";

            // Check if VSCode is installed in User Mode
            if (File.Exists(userPath))
            {
                return userPath;
            }
            // Check if VSCode is installed in System Mode
            else if (File.Exists(systemPath))
            {
                return systemPath;
            }
            else
            {
                return defaultPath;
            }
        }

        private void ExternalDiffConfig_Load(object sender, EventArgs e)
        {
            cboDiffTool.SelectedItem = string.IsNullOrEmpty(Properties.Settings.Default.DiffToolBinaryPath) ? DiffTools.None : DiffTools.Custom;

            txtArgs.Text = Properties.Settings.Default.DiffToolArguments ?? string.Empty;
            txtPath.Text = Properties.Settings.Default.DiffToolBinaryPath ?? string.Empty;
        }

        private void bttnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DiffToolArguments = txtArgs.Text;
            Properties.Settings.Default.DiffToolBinaryPath = txtPath.Text;
            Properties.Settings.Default.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void SelectDiffTool(object sender, EventArgs e)
        {
            var tool = (DiffTools)(cboDiffTool.SelectedValue ?? DiffTools.None);
            txtArgs.Visible = tool != DiffTools.None;
            txtPath.Visible = tool != DiffTools.None;
            lblDiffToolArgs.Visible = tool != DiffTools.None;
            lblDiffToolPath.Visible = tool != DiffTools.None;
            bttnOpen.Visible = tool != DiffTools.None;
            switch (tool)
            {
                case DiffTools.None:
                    txtArgs.Text = string.Empty;
                    txtPath.Text = string.Empty;
                    break;

                case DiffTools.WinMerge:
                    txtPath.Text = GetWinMergePath();
                    txtArgs.Text = "/u /e /wl /wr /dl \"Old DDL\" /dr \"New DDL\" /fileext sql $OLD$ $NEW$";
                    break;

                case DiffTools.VSCode:
                    txtPath.Text = GetVSCodePath();
                    txtArgs.Text = "--diff $OLD$ $NEW$";
                    break;

                case DiffTools.Custom:
                default:
                    txtArgs.Text = string.Empty;
                    txtPath.Text = string.Empty;
                    break;
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void BttnOpen_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.FileName = txtPath.Text;
            if (ofd.ShowDialog() != DialogResult.OK) return;
            txtPath.Text = ofd.FileName;
        }
    }
}