﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashServiceConfig
{
    public partial class ServiceLog : Form
    {
        private readonly string logsFolder = Path.Combine(Application.StartupPath, "Logs");

        public ServiceLog()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private void ServiceLog_Load(object sender, EventArgs e)
        {
            RefreshLogs();
            Activated += ServiceLog_Activated; // Scroll to end of log when form is activated (doesn't work in form load event)
        }

        private void ServiceLog_Activated(object sender, EventArgs e)
        {
            // Scroll to end of log when form is activated (doesn't work in form load event)
            Activated -= ServiceLog_Activated;
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
                txtLog.Text = $"Log folder does not exist '{logsFolder}'. This is created when the service is started.";
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
                txtLog.Text = $"File not found '{filePath}'";
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