using DBADash.XE;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// "Save as template" dialog.  Collects the template name and scope (this user / everyone) and, for each configured
    /// filter, whether loading the template should <b>prompt</b> for the value (with an optional prompt label) instead
    /// of reloading the stored value - so a template can, say, ask for the SPID or application name each time it is used.
    /// Returns the choices, or null if cancelled.
    /// </summary>
    internal static class SaveXETemplateForm
    {
        public sealed class Result
        {
            public string Name { get; init; }
            public bool IsGlobal { get; init; }

            /// <summary>Per-filter prompt choices, in the same order as the filters passed in.</summary>
            public List<FilterPrompt> FilterPrompts { get; init; } = new();
        }

        public sealed class FilterPrompt
        {
            public bool Prompt { get; init; }
            public string PromptText { get; init; }
        }

        private const string ColPrompt = "colPrompt";
        private const string ColPromptText = "colPromptText";

        public static Result Show(IWin32Window owner, string defaultName, IReadOnlyList<XEFilter> filters, bool canGlobal)
        {
            filters ??= Array.Empty<XEFilter>();

            using var form = new Form
            {
                Text = "Save trace template",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(560, 460),
                ShowInTaskbar = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // name
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // scope
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); // filters header
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // filters grid

            layout.Controls.Add(new Label { Text = "Name:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            var txtName = new TextBox { Dock = DockStyle.Fill, Text = defaultName ?? string.Empty };
            layout.Controls.Add(txtName, 1, 0);

            var scopePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var optUser = new RadioButton { Text = "This user", AutoSize = true, Checked = true, Margin = new Padding(0, 4, 16, 0) };
            var optGlobal = new RadioButton { Text = "Everyone (global)", AutoSize = true, Enabled = canGlobal, Margin = new Padding(0, 4, 0, 0) };
            scopePanel.Controls.Add(optUser);
            scopePanel.Controls.Add(optGlobal);
            layout.Controls.Add(new Label { Text = "Scope:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            layout.Controls.Add(scopePanel, 1, 1);

            var filtersLabel = new Label
            {
                Text = filters.Count > 0
                    ? "Prompt for a value when loading (instead of reusing the stored value):"
                    : "No filters configured - nothing to prompt for.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            layout.SetColumnSpan(filtersLabel, 2);
            layout.Controls.Add(filtersLabel, 0, 2);

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Filter", Width = 250, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            { Name = ColPrompt, HeaderText = "Prompt", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            { Name = ColPromptText, HeaderText = "Prompt text", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            foreach (var f in filters)
            {
                grid.Rows.Add(DescribeFilter(f), false, string.Empty);
            }
            // Commit the checkbox edit immediately so the value is read back reliably on OK.
            grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            layout.SetColumnSpan(grid, 2);
            layout.Controls.Add(grid, 0, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 80, Height = 30 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80, Height = 30 };
            buttons.Controls.AddRange(new Control[] { ok, cancel });

            form.Controls.Add(layout);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show(form, "Enter a template name.", "Save trace template", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    form.DialogResult = DialogResult.None;
                }
            };

            form.ApplyTheme();
            if (form.ShowDialog(owner) != DialogResult.OK) return null;

            var prompts = new List<FilterPrompt>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                prompts.Add(new FilterPrompt
                {
                    Prompt = row.Cells[ColPrompt].Value is true,
                    PromptText = row.Cells[ColPromptText].Value as string
                });
            }

            return new Result
            {
                Name = txtName.Text.Trim(),
                IsGlobal = optGlobal.Checked,
                FilterPrompts = prompts
            };
        }

        internal static string DescribeFilter(XEFilter f)
        {
            if (f == null) return string.Empty;
            var scope = string.IsNullOrEmpty(f.EventName) ? "(all events)" : f.EventName;
            return $"{scope}.{f.Field} {f.Op} {f.Value}";
        }
    }
}
