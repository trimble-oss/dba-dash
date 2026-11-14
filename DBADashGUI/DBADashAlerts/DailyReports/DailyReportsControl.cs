using DBADash.DailyReport;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts.DailyReports
{
    /// <summary>
    /// Lists the daily health reports and lets the user add, edit, delete and send one now.  Reports are stored in the repository.
    /// </summary>
    public class DailyReportsControl : UserControl
    {
        private readonly DBADashDataGridView dgv = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoGenerateColumns = false, RowHeadersVisible = false };
        private readonly ToolStrip toolStrip = new();
        private readonly ToolStripButton btnAdd = new("Add", Properties.Resources._077_AddFile_16x16_72);
        private readonly ToolStripButton btnEdit = new("Edit", Properties.Resources.EditTooltip_16x);
        private readonly ToolStripButton btnDelete = new("Delete", Properties.Resources.Close_red_16x);
        private readonly ToolStripButton btnSendNow = new("Send now", Properties.Resources.SendEmail_16x) { ToolTipText = "Send this report now to its channels.  Doesn't change the schedule.  A changes report's next send lists what changed since this one." };
        private readonly ToolStripButton btnRefresh = new("Refresh", Properties.Resources._112_RefreshArrow_Green_16x16_72);
        private readonly ToolStripLabel lblStatus = new();

        private List<DailyReportConfig> reports = new();

        public DailyReportsControl()
        {
            Dock = DockStyle.Fill;
            dgv.Columns.AddRange(
                Col("ReportName", "Name", 160),
                Col("IsActive", "Active", 55),
                Col("Type", "Type", 75),
                Col("Schedule", "Schedule", 230),
                Col("ScopeDescription", "Scope", 200),
                Col("Channels", "Channels", 200),
                Col("Performance", "Performance", 90),
                Col("WeekOverWeek", "Compare with usual", 130),
                Col("LastSent", "Last sent (UTC)", 130),
                Col("LastSendResult", "Last send result", 360),
                Col("LastRun", "Last scheduled run (UTC)", 150),
                Col("LastRunError", "Last scheduled run error", 260));

            btnAdd.Click += (_, _) => OpenEditor(null);
            btnEdit.Click += (_, _) => OpenEditor(Selected);
            btnDelete.Click += Delete_Click;
            btnSendNow.Click += async (_, _) => await SendNowAsync(Selected);
            btnRefresh.Click += (_, _) => LoadReports();
            dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenEditor(Selected); };
            dgv.SelectionChanged += (_, _) => UpdateButtons();
            // The full send result, one line per channel, as the cell shows it on one line
            dgv.CellFormatting += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "LastSendResult" && dgv.Rows[e.RowIndex].DataBoundItem is DailyReportRow row)
                {
                    dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = row.Config.LastSendResult;
                }
            };

            toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, btnDelete, new ToolStripSeparator(), btnSendNow, new ToolStripSeparator(), btnRefresh, new ToolStripSeparator(), lblStatus });
            toolStrip.Dock = DockStyle.Top;

            Controls.Add(dgv);
            Controls.Add(toolStrip);
            this.ApplyTheme();
            Load += (_, _) => LoadReports();
        }

        private static DataGridViewTextBoxColumn Col(string property, string header, int width) =>
            new() { DataPropertyName = property, HeaderText = header, Width = width, Name = property };

        private DailyReportConfig Selected => (dgv.CurrentRow?.DataBoundItem as DailyReportRow)?.Config;

        private string ConnectionString => Common.ConnectionString;

        private void UpdateButtons()
        {
            var hasSelection = Selected != null;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
            btnSendNow.Enabled = hasSelection && !sending;
        }

        /// <summary>A Send now is running: selecting another row or refreshing mustn't enable the button and start a second one</summary>
        private bool sending;

        public void LoadReports()
        {
            try
            {
                var selectedID = Selected?.DailyReportID;
                reports = DailyReportRepository.GetReports(ConnectionString);
                dgv.DataSource = new BindingList<DailyReportRow>(reports.Select(r => new DailyReportRow(r)).ToList());
                if (selectedID != null)
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.DataBoundItem is DailyReportRow dr && dr.Config.DailyReportID == selectedID)
                        {
                            dgv.CurrentCell = row.Cells[0];
                        }
                    }
                }
                lblStatus.Text = reports.Count == 0 ? "No daily reports yet" : $"{reports.Count} daily report(s)";
                UpdateButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Daily reports", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Opens the editor for the report, or for a new report when config is null</summary>
        private void OpenEditor(DailyReportConfig config)
        {
            using var frm = new DailyReportEditForm(config, ConnectionString);
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                LoadReports();
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            var config = Selected;
            if (config == null) return;
            if (MessageBox.Show($"Delete the daily report '{config.ReportName}'?  This can't be undone.", "Delete daily report", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                DailyReportRepository.Delete(ConnectionString, config.DailyReportID);
                LoadReports();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete daily report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendNowAsync(DailyReportConfig config)
        {
            if (config == null || sending) return;
            sending = true;
            btnSendNow.Enabled = false;
            lblStatus.Text = $"Sending '{config.ReportName}'...";
            try
            {
                var (content, result) = await Task.Run(() => DailyReportRunner.SendNowAsync(config, ConnectionString, DateTime.UtcNow));

                // Each channel's outcome with the mail server's reply: "accepted" means the server took it, so a report that then doesn't
                // arrive was held or filtered further on, not lost here
                var message = $"{content.Subject}{Environment.NewLine}{Environment.NewLine}{result}";
                if (result.SentToAny)
                {
                    message += Environment.NewLine + Environment.NewLine +
                               "If it doesn't arrive in a few minutes, check the junk or quarantine folder: the mail server accepted it, so it was held or filtered after that.";
                }
                MessageBox.Show(message, result.SentToAll ? "Daily report sent" : result.SentToAny ? "Daily report not sent to every channel" : "Daily report not sent",
                    MessageBoxButtons.OK, result.SentToAll ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Daily report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                sending = false;
                LoadReports(); // Shows the send in Last sent / Last send result, and enables the buttons again
            }
        }

        /// <summary>Grid row: formats the config for display</summary>
        private class DailyReportRow
        {
            public DailyReportRow(DailyReportConfig config) => Config = config;
            public DailyReportConfig Config { get; }
            public string ReportName => Config.ReportName;
            public string IsActive => Config.IsActive ? "Yes" : "No";
            public string Type => Config.ReportType == DailyReportType.Changes ? "Changes" : "Full";
            public string Schedule => $"{Config.ScheduleDescription} ({Config.TimeZone})";
            public string ScopeDescription => Config.ScopeDescription;
            public string Channels => string.Join(", ", Config.Channels.Select(c => c.ChannelName));
            public string Performance => Config.IncludePerformance ? "Yes" : "No";
            public string WeekOverWeek => Config.IncludeWeekOverWeek ? $"Yes ({Config.ChangeThresholdPct}%)" : "No";
            public string LastRun => Config.LastRunUTC?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
            public string LastSent => Config.LastSentUTC?.ToString("yyyy-MM-dd HH:mm") ?? (Config.LastSendAttemptUTC.HasValue ? "Not sent" : "Never");
            // One line per channel - shown on one line in the grid, in full in the tooltip
            public string LastSendResult => Config.LastSendResult?.Replace(Environment.NewLine, " | ");
            public string LastRunError => Config.LastRunError;
        }
    }
}
