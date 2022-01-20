using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class ServiceLog : Form
    {
        readonly string logsFolder = System.IO.Path.Combine(Application.StartupPath, "Logs");

        public ServiceLog()
        {
            InitializeComponent();
        }

        private void ServiceLog_Load(object sender, EventArgs e)
        {
            refreshLogs();
            Activated += new System.EventHandler(this.ServiceLog_Activated); // Scroll to end of log when form is activated (doesn't work in form load event)
        }

        private void ServiceLog_Activated(object sender, EventArgs e)
        {
            // Scroll to end of log when form is activated (doesn't work in form load event)
            Activated -= new System.EventHandler(this.ServiceLog_Activated);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void refreshLogs()
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
                txtLog.Text = string.Format("Log folder does not exist '{0}'. This is created when the service is started.",logsFolder);
            }
        }

        private void loadLog(string fileName)
        {
            var sb = new StringBuilder();
            string filter = txtLogFilter.Text.ToLower().Trim();
            string filePath = Path.Combine(logsFolder, fileName);
            if (File.Exists(filePath))
            {
                using (FileStream fs = new FileStream(filePath,
                                       FileMode.Open,
                                       FileAccess.Read,
                                       FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs))
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

        private void bttnRefreshLog_Click(object sender, EventArgs e)
        {
            refreshLogs();
        }

        private void cboLogs_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadLog(cboLogs.Text);
        }
    }
}
