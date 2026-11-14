using DBADash.DailyReport;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts.DailyReports
{
    /// <summary>
    /// Per-report performance settings: which measures are reported and how big a change has to be, the number of baseline weeks, how many
    /// instances are listed and how many procedures/queries are shown behind each.  Works on a copy - Settings is the edited copy after OK.
    /// </summary>
    public class DailyReportPerformanceSettingsForm : Form
    {
        public DailyReportSettings Settings { get; private set; }

        private readonly DataTable metrics = new();
        private readonly DBADashDataGridView grid = new()
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            EditMode = DataGridViewEditMode.EditOnEnter
        };
        private readonly NumericUpDown numBaselineWeeks = new() { Minimum = DailyReportSettings.MinBaselineWeeks, Maximum = DailyReportSettings.MaxBaselineWeeks, Width = 60 };
        private readonly NumericUpDown numMaxInstances = new() { Minimum = 1, Maximum = 500, Width = 60 };
        private readonly NumericUpDown numTopQueries = new() { Minimum = 1, Maximum = 10, Width = 60 };

        public DailyReportPerformanceSettingsForm(DailyReportSettings settings)
        {
            Settings = (settings ?? new DailyReportSettings()).Clone();
            Text = "Daily report performance settings";
            Icon = DailyReportIcons.FormIcon;
            StartPosition = FormStartPosition.CenterParent;
            Width = 620;
            Height = 560;
            MinimumSize = new System.Drawing.Size(520, 420);
            MinimizeBox = false;

            metrics.Columns.Add("Key", typeof(string));
            metrics.Columns.Add("Enabled", typeof(bool));
            metrics.Columns.Add("Label", typeof(string));
            metrics.Columns.Add("MinChange", typeof(double));
            metrics.Columns.Add("Unit", typeof(string));
            metrics.Columns.Add("Threshold", typeof(string));

            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Enabled", HeaderText = "Report", Width = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Label", HeaderText = "Measure", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MinChange", HeaderText = "Min change", AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader, DefaultCellStyle = { Format = "0.##", Alignment = DataGridViewContentAlignment.MiddleRight } });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Unit", HeaderText = "Unit", ReadOnly = true, Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Threshold", HeaderText = "Thresholds", ReadOnly = true, Width = 120 });
            grid.DataError += (_, e) =>
            {
                MessageBox.Show("Enter a number of 0 or more.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            };

            var lblInfo = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Padding = new Padding(8, 6, 8, 6),
                Text = "A measure is listed when it's over the Performance Summary thresholds, or when it's worse than usual by at least the report's " +
                       "change threshold % and by at least its minimum change.  Untick a measure to leave it out of the report."
            };

            var options = new TableLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
            void Row(string label, Control control)
            {
                options.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 4, 8, 0) });
                options.Controls.Add(control);
            }
            Row("Baseline weeks (same weekday)", numBaselineWeeks);
            Row("Instances listed, at most", numMaxInstances);
            Row("Procedures/queries per instance", numTopQueries);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8, 8, 8, 12) };
            var btnOK = new Button { Text = "OK", AutoSize = true }.WithImage(Properties.Resources.StatusOK_16x);
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var btnDefaults = new Button { Text = "Defaults", AutoSize = true }.WithImage(Properties.Resources.Undo_grey_16x);
            buttons.Controls.AddRange(new Control[] { btnOK, btnCancel, btnDefaults });
            btnOK.Click += (_, _) => Accept();
            btnDefaults.Click += (_, _) => Fill(new DailyReportSettings());

            Controls.Add(grid);
            Controls.Add(lblInfo);
            Controls.Add(options);
            Controls.Add(buttons);
            AcceptButton = btnOK;
            CancelButton = btnCancel;

            Fill(Settings);
            grid.DataSource = metrics;
            this.ApplyTheme();
            // Size the explanation to its wrapped text, which depends on the width, font and DPI
            void SizeInfo() => lblInfo.Height = TextRenderer.MeasureText(lblInfo.Text, lblInfo.Font, new System.Drawing.Size(Math.Max(1, ClientSize.Width - lblInfo.Padding.Horizontal), 0), TextFormatFlags.WordBreak).Height + lblInfo.Padding.Vertical + 2;
            Resize += (_, _) => SizeInfo();
            SizeInfo();
        }

        private void Fill(DailyReportSettings s)
        {
            metrics.Rows.Clear();
            foreach (var m in DailyReportSettings.Metrics)
            {
                var setting = s.For(m.Key);
                metrics.Rows.Add(m.Key, setting.Enabled, m.Label, setting.MinChange, m.Unit, m.HasThreshold ? "Also compared" : "Change only");
            }
            numBaselineWeeks.Value = Math.Clamp(s.BaselineWeeks, DailyReportSettings.MinBaselineWeeks, DailyReportSettings.MaxBaselineWeeks);
            numMaxInstances.Value = Math.Clamp(s.MaxPerformanceInstances, 1, 500);
            numTopQueries.Value = Math.Clamp(s.TopQueriesCount, 1, 10);
        }

        private void Accept()
        {
            grid.EndEdit();
            foreach (DataRow row in metrics.Rows)
            {
                var minChange = row["MinChange"] == DBNull.Value ? 0 : Convert.ToDouble(row["MinChange"], CultureInfo.InvariantCulture);
                // A double cell accepts NaN and Infinity, which can't be saved as JSON
                if (minChange < 0 || !double.IsFinite(minChange))
                {
                    MessageBox.Show($"The minimum change for {row["Label"]} must be a number of 0 or more.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Settings.MetricSettings[(string)row["Key"]] = new PerformanceMetricSetting { Enabled = (bool)row["Enabled"], MinChange = minChange };
            }
            Settings.BaselineWeeks = (int)numBaselineWeeks.Value;
            Settings.MaxPerformanceInstances = (int)numMaxInstances.Value;
            Settings.TopQueriesCount = (int)numTopQueries.Value;
            Settings.Normalized();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
