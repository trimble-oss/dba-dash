using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// Modal list of the stored procedures available in the UserAlert schema for use as custom SQL alert rules.
    /// Shows each proc and whether it matches the expected result-set contract (InstanceID, AlertKey, Message).
    /// Built entirely in code (no designer) to keep the surface small.
    /// </summary>
    public partial class CustomSqlProcPicker : Form
    {
        private readonly DataGridView dgv;
        private readonly TextBox txtSearch;
        private readonly Label lblSelected;
        private readonly Label lblInfo;
        private DataTable dt;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string SelectedProcName { get; set; }

        public CustomSqlProcPicker()
        {
            Text = @"Select Custom SQL Alert Procedure";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 420);
            Size = new Size(720, 520);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(8)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // search
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // grid
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // selected label
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // info label (wraps)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));  // buttons

            txtSearch = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search procedures...", Margin = new Padding(0, 0, 0, 6) };
            txtSearch.TextChanged += (_, _) => ApplySearch();

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                Margin = new Padding(0),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProcName", HeaderText = "Procedure", DataPropertyName = "ProcName", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colValid", HeaderText = "Valid Schema", DataPropertyName = "Valid", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInUse", HeaderText = "In Use", DataPropertyName = "InUse", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQualified", HeaderText = "Qualified Name", DataPropertyName = "QualifiedName", FillWeight = 27 });
            dgv.SelectionChanged += Dgv_SelectionChanged;
            dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && CanAccept()) { DialogResult = DialogResult.OK; } };
            dgv.CellFormatting += Dgv_CellFormatting;

            lblSelected = new Label { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            lblInfo = new Label { Dock = DockStyle.Fill, AutoSize = false, ForeColor = Color.Firebrick, Margin = new Padding(0, 2, 0, 0) };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = false,
                Margin = new Padding(0, 6, 0, 0)
            };
            var bttnOK = new Button { Text = @"OK", DialogResult = DialogResult.None, AutoSize = true, MinimumSize = new Size(80, 28), Margin = new Padding(6, 0, 0, 0) };
            var bttnCancel = new Button { Text = @"Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(80, 28), Margin = new Padding(6, 0, 0, 0) };
            var bttnRefresh = new Button { Text = @"Refresh", AutoSize = true, MinimumSize = new Size(80, 28), Margin = new Padding(6, 0, 0, 0) };
            bttnOK.Click += (_, _) => { if (CanAccept()) DialogResult = DialogResult.OK; };
            bttnRefresh.Click += (_, _) => LoadProcs(refresh: true);
            buttonPanel.Controls.Add(bttnOK);
            buttonPanel.Controls.Add(bttnCancel);
            buttonPanel.Controls.Add(bttnRefresh);

            layout.Controls.Add(txtSearch, 0, 0);
            layout.Controls.Add(dgv, 0, 1);
            layout.Controls.Add(lblSelected, 0, 2);
            layout.Controls.Add(lblInfo, 0, 3);
            layout.Controls.Add(buttonPanel, 0, 4);
            Controls.Add(layout);

            AcceptButton = bttnOK;
            CancelButton = bttnCancel;

            Load += (_, _) => LoadProcs(refresh: false);
            Disposed += (_, _) => _inUseFont?.Dispose();
            this.ApplyTheme();
        }

        private void LoadProcs(bool refresh)
        {
            if (refresh) CustomSqlProc.Invalidate();

            dt = new DataTable();
            dt.Columns.Add("ProcName", typeof(string));
            dt.Columns.Add("Valid", typeof(string));
            dt.Columns.Add("InUse", typeof(string));
            dt.Columns.Add("QualifiedName", typeof(string));
            foreach (var p in CustomSqlProc.Cached)
            {
                dt.Rows.Add(p.ProcName, p.IsValidSchema ? "Yes" : "No", p.InUse ? "Yes" : string.Empty, p.QualifiedName);
            }

            dgv.DataSource = new DataView(dt);
            SelectCurrent();
            UpdateSelected();
        }

        private Font _inUseFont;

        /// <summary>Italic (de-emphasized) font for procs already referenced by a rule. Survives sorting via CellFormatting.</summary>
        private Font InUseFont => _inUseFont ??= new Font(dgv.Font, FontStyle.Italic);

        /// <summary>De-emphasize procs already referenced by a rule so they're less prominent for a new selection.</summary>
        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || dgv.Rows[e.RowIndex].DataBoundItem is not DataRowView drv) return;
            if (drv["InUse"] is string s && s.Length > 0)
            {
                e.CellStyle.Font = InUseFont;
            }
        }

        private void SelectCurrent()
        {
            dgv.ClearSelection();
            if (string.IsNullOrEmpty(SelectedProcName)) return;
            var row = dgv.Rows.Cast<DataGridViewRow>()
                .FirstOrDefault(r => (string)r.Cells["colProcName"].Value == SelectedProcName);
            if (row != null)
            {
                row.Selected = true;
                dgv.CurrentCell = row.Cells["colProcName"];
            }
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 1)
            {
                SelectedProcName = (string)dgv.SelectedRows[0].Cells["colProcName"].Value;
            }
            UpdateSelected();
        }

        private CustomSqlProc Selected => CustomSqlProc.Cached.FirstOrDefault(p => p.ProcName == SelectedProcName);

        private void UpdateSelected()
        {
            var proc = Selected;
            var usage = proc is { InUse: true } ? " (already in use)" : string.Empty;
            lblSelected.Text = string.IsNullOrEmpty(SelectedProcName)
                ? @"Selected: None"
                : $@"Selected: UserAlert.{SelectedProcName}{usage}";

            lblInfo.Text = proc is { IsValidSchema: false }
                ? @"This procedure does not return the expected result set (exactly: InstanceID int, AlertKey nvarchar, AlertMessage nvarchar)."
                : string.Empty;
        }

        private bool CanAccept()
        {
            if (string.IsNullOrEmpty(SelectedProcName))
            {
                MessageBox.Show(@"Select a procedure.", @"No procedure selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            var proc = Selected;
            if (proc is { IsValidSchema: false } &&
                MessageBox.Show(@"The selected procedure does not match the expected result set contract and may fail at runtime. Use it anyway?",
                    @"Invalid schema", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return false;
            }
            return true;
        }

        private void ApplySearch()
        {
            if (dgv.DataSource is not DataView view) return;
            var search = txtSearch.Text.Replace("'", "''");
            try
            {
                view.RowFilter = string.IsNullOrEmpty(search)
                    ? string.Empty
                    : $"ProcName LIKE '%{search}%' OR QualifiedName LIKE '%{search}%'";
            }
            catch
            {
                // ignore invalid filter input
            }
            SelectCurrent();
        }
    }
}
