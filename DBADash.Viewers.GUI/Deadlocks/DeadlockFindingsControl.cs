using DBADash.Deadlock.Analysis;
using DBADash.Deadlock.Model;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// What the graph means: the patterns <see cref="DeadlockAnalyser"/> recognises, in a grid.
    ///
    /// The picture and the grids say what happened; this is the part that says why, and what usually
    /// stops it happening again.  Nothing here goes back to the instance, so it works the same for a
    /// graph opened from a file as for one from a monitored instance.
    /// </summary>
    internal sealed class DeadlockFindingsControl : UserControl
    {
        private readonly DBADashDataGridView _grid;
        private IReadOnlyList<DeadlockFinding> _findings = Array.Empty<DeadlockFinding>();

        private const string SeverityColumn = "Severity";
        private const string TitleColumn = "Finding";
        private const string DetailColumn = "Detail";
        private const string InvolvesColumn = "Involves";

        /// <summary>Raised when a finding about a process is chosen, so the host can select it.</summary>
        internal event EventHandler<DeadlockProcess> ProcessActivated;

        internal DeadlockFindingsControl()
        {
            _grid = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                MultiSelect = false,

                // The detail is a sentence or two, so rows grow to fit rather than eliding the part
                // that says what to do about it.
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };

            _grid.DataBindingComplete += (_, _) => ConfigureColumns();
            _grid.SelectionChanged += Grid_SelectionChanged;

            Controls.Add(_grid);
        }

        /// <summary>How many findings the current graph produced, for the tab caption.</summary>
        internal int Count => _findings.Count;

        internal void Show(DeadlockGraph graph)
        {
            _findings = DeadlockAnalyser.Analyse(graph);

            var table = new DataTable();
            table.Columns.Add(SeverityColumn, typeof(string));
            table.Columns.Add(TitleColumn, typeof(string));
            table.Columns.Add(DetailColumn, typeof(string));
            table.Columns.Add(InvolvesColumn, typeof(string));

            foreach (var finding in _findings)
            {
                table.Rows.Add(
                    Describe(finding.Severity),
                    finding.Title,
                    finding.Detail,
                    Involves(finding));
            }

            _grid.DataSource = table;
            _grid.ClearSelection(); // Binding selects the first row, which would fire a selection nobody asked for
        }

        /// <summary>
        /// Severity as a word rather than an icon: there are three of them, they are read once, and a
        /// colour alone would not say which is which.
        /// </summary>
        private static string Describe(DeadlockFindingSeverity severity) => severity switch
        {
            DeadlockFindingSeverity.Warning => "Warning",
            DeadlockFindingSeverity.Advice => "Advice",
            _ => "Info"
        };

        private static string Involves(DeadlockFinding finding)
        {
            var parts = finding.Processes.Select(p => p.DisplayName)
                .Concat(finding.Resources.Select(r => r.DisplayName));

            return string.Join(", ", parts);
        }

        private void ConfigureColumns()
        {
            var detail = _grid.Columns[DetailColumn];
            if (detail != null)
            {
                detail.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                detail.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                detail.FillWeight = 100;
            }

            foreach (var name in new[] { SeverityColumn, TitleColumn, InvolvesColumn })
            {
                var column = _grid.Columns[name];
                if (column == null) continue;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            HighlightWarnings();
        }

        /// <summary>
        /// Warnings are about the capture rather than the deadlock - they say how much the rest is
        /// worth - so they are the one severity worth colouring.
        /// </summary>
        private void HighlightWarnings()
        {
            var theme = ThemeExtensions.CurrentTheme;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Cells[SeverityColumn].Value as string != "Warning") continue;
                row.DefaultCellStyle.BackColor = theme.WarningBackColor;
                row.DefaultCellStyle.ForeColor = theme.WarningForeColor;
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            var index = _grid.CurrentRow?.Index ?? -1;
            if (index < 0 || index >= _findings.Count) return;

            var process = _findings[index].Processes.FirstOrDefault();
            if (process != null) ProcessActivated?.Invoke(this, process);
        }
    }
}
