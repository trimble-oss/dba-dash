using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashServiceConfig
{
    public partial class ServiceLog : Form
    {
        private readonly string logsFolder = System.IO.Path.Combine(Application.StartupPath, "Logs");

        public ServiceLog()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void ServiceLog_Load(object sender, EventArgs e)
        {
            RefreshLogs();
            Activated += new System.EventHandler(this.ServiceLog_Activated); // Scroll to end of log when form is activated (doesn't work in form load event)
        }

        private void ServiceLog_Activated(object sender, EventArgs e)
        {
            // Scroll to end of log when form is activated (doesn't work in form load event)
            Activated -= new System.EventHandler(this.ServiceLog_Activated);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void RefreshLogs()
        {
            if (Directory.Exists(logsFolder))
            {
                cboLogs.Items.Clear();
                var directory = new DirectoryInfo(logsFolder);
                foreach (var f in directory.GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime))
                {
                    cboLogs.Items.Add(f.Name);
                }
                if (cboLogs.Items.Count > 0)
                {
                    cboLogs.SelectedIndex = 0;
                }
                else
                {
                    txtLog.Text = "Log file not found. This is created when the service is started.";
                }
            }
            else
            {
                txtLog.Text = string.Format("Log folder does not exist '{0}'. This is created when the service is started.", logsFolder);
            }
        }

        private void LoadLog(string fileName)
        {
            var sb = new StringBuilder();
            string filter = txtLogFilter.Text.ToLower().Trim();
            string filePath = Path.Combine(logsFolder, fileName);
            if (File.Exists(filePath))
            {
                using (FileStream fs = new(filePath,
                                       FileMode.Open,
                                       FileAccess.Read,
                                       FileShare.ReadWrite))
                {
                    using (StreamReader sr = new(fs))
                    {
                        while (sr.Peek() >= 0) // reading the old data
                        {
                            string line = sr.ReadLine();
                            if (filter == string.Empty)
                            {
                                sb.AppendLine(line);
                            }
                            else if (line.ToLower().Contains(filter))
                            {
                                sb.AppendLine(line);
                            }
                        }
                    }
                }
                txtLog.Text = sb.ToString();
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
            else
            {
                txtLog.Text = String.Format("File not found '{0}'", filePath);
            }
        }

        private void BttnRefreshLog_Click(object sender, EventArgs e)
        {
            RefreshLogs();
        }

        private void CboLogs_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLog(cboLogs.Text);
        }

        private void TxtLogFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadLog(cboLogs.Text);
            }
        }

        private void BttnNotepad_Click(object sender, EventArgs e)
        {
            string filePath = Path.Combine(logsFolder, cboLogs.Text);
            System.Diagnostics.Process.Start("Notepad.exe", filePath);
        }
    }
}