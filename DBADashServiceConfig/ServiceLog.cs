using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                foreach (var f in directory.GetFiles("*.txt")
                             .Select(f => (File: f, Period: ServiceLogPeriod(f.Name)))
                             .OrderByDescending(f => f.Period != null)
                             .ThenByDescending(f => f.Period, StringComparer.Ordinal)
                             .ThenByDescending(f => f.File.LastWriteTime))
                {
                    cboLogs.Items.Add(f.File.Name);
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

        /// <summary>
        /// A rolling service log is named for the period it covers - log-yyyyMMddHH.txt by default, with _001 etc. added
        /// when the file was locked - so the name orders the logs.  Last write time doesn't: while the service holds the
        /// current log open, the time Windows lists for it lags behind, and an older file can look newer.
        /// The whole name is matched, from the "log-" prefix in serilog.json, so another file that happens to end in a
        /// date (e.g. a copy of a GUI log) isn't taken for the latest service log.
        /// </summary>
        /// <returns>The period and sequence as a string that sorts in date order, or null for any other file, e.g. FatalError.txt</returns>
        private static string ServiceLogPeriod(string fileName)
        {
            var match = ServiceLogNameRegex.Match(fileName);
            if (!match.Success) return null;
            // Pad so logs from a different rolling interval (e.g. daily - yyyyMMdd) sort alongside hourly ones
            return match.Groups["period"].Value.PadRight(12, '0') + match.Groups["seq"].Value.PadLeft(6, '0');
        }

        private static readonly Regex ServiceLogNameRegex = new(@"^log-(?<period>\d{4,12})(?:_(?<seq>\d+))?\.txt$", RegexOptions.IgnoreCase);

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