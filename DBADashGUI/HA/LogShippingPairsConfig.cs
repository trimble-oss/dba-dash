using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.LogShipping
{
    /// <summary>
    /// Configure log shipping primary/secondary instance pairs.  Databases on the primary that are not restoring/in
    /// standby on the secondary are reported as missing (Critical).  Databases can be excluded via
    /// <see cref="LogShippingExclusionsConfig"/>.  See issue #625.
    /// </summary>
    public class LogShippingPairsConfig : Form
    {
        private sealed record InstanceItem(int InstanceID, string Name)
        {
            public override string ToString() => Name;
        }

        private DBADashDataGridView grid;
        private ComboBox cboPrimary;
        private ComboBox cboSecondary;
        private Button bttnRemove;
        private Button bttnSave;
        private List<LogShippingPair> pairs = new();
        private readonly BindingSource pairsBindingSource = new();
        private List<InstanceItem> instances = new();
        private bool isLoading;
        private bool isUpdatingEditor;

        /// <summary>True if any pairs were added, updated or removed.</summary>
        public bool HasChanges { get; private set; }

        public LogShippingPairsConfig()
        {
            BuildLayout();
            this.ApplyTheme();
            Load += LogShippingPairsConfig_Load;
        }

        private void BuildLayout()
        {
            Text = "Log Shipping Pairs";
            Width = 760;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 460);

            var lblInfo = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 116,
                Padding = new Padding(8, 6, 8, 6),
                Text = "Log shipped databases are detected automatically.  Add primary/secondary instance pairs to also report " +
                       "databases on the primary that are not restoring on the secondary as Missing (Critical).  " +
                       "System, snapshot, offline and SIMPLE recovery databases are ignored.  New databases are N/A for " +
                       "the new database exclude period configured in the thresholds.  Use Configure > Exclusions to exclude " +
                       "databases (applied to the secondary instance)."
            };

            grid = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LogShippingPair.PrimaryInstance), HeaderText = "Primary", FillWeight = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(LogShippingPair.SecondaryInstance), HeaderText = "Secondary", FillWeight = 40 });
            // Bound once to a BindingSource.  Re-assigning grid.DataSource with a new list could leave the grid painting
            // rows that no longer exist (NullReferenceException in DataGridViewCell paint).
            grid.DataSource = pairsBindingSource;
            grid.SelectionChanged += Grid_SelectionChanged;

            cboPrimary = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboSecondary = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cboPrimary.SelectedIndexChanged += Instance_SelectedIndexChanged;
            cboSecondary.SelectedIndexChanged += Instance_SelectedIndexChanged;

            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8)
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            editor.Controls.Add(new Label { Text = "Primary:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            editor.Controls.Add(cboPrimary, 1, 0);
            editor.Controls.Add(new Label { Text = "Secondary:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            editor.Controls.Add(cboSecondary, 1, 1);

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 52,
                Padding = new Padding(8, 8, 8, 16)
            };
            var bttnClose = new Button { Text = "Close", DialogResult = DialogResult.Cancel, AutoSize = true };
            bttnSave = new Button { Text = "Add Pair", AutoSize = true };
            bttnRemove = new Button { Text = "Remove Pair", AutoSize = true, Enabled = false };
            var bttnNew = new Button { Text = "New Pair", AutoSize = true };
            bttnSave.Click += BttnSave_Click;
            bttnRemove.Click += BttnRemove_Click;
            bttnNew.Click += (_, _) => ClearEditor();
            pnlButtons.Controls.Add(bttnClose);
            pnlButtons.Controls.Add(bttnSave);
            pnlButtons.Controls.Add(bttnRemove);
            pnlButtons.Controls.Add(bttnNew);

            Controls.Add(grid);
            Controls.Add(lblInfo);
            Controls.Add(editor);
            Controls.Add(pnlButtons);

            CancelButton = bttnClose;
        }

        private async void LogShippingPairsConfig_Load(object sender, EventArgs e)
        {
            try
            {
                // Pairs for soft-deleted (inactive) instances aren't shown, so only active instances are needed.
                var dtInstances = await CommonData.GetInstancesAsync(Active: true, azureDB: false);
                instances = dtInstances.Rows.Cast<DataRow>()
                    .Select(r => new InstanceItem(r.Field<int>("InstanceID"), r.Field<string>("InstanceDisplayName")))
                    .GroupBy(i => i.InstanceID).Select(g => g.First())
                    .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                cboPrimary.Items.AddRange(instances.Cast<object>().ToArray());
                cboSecondary.Items.AddRange(instances.Cast<object>().ToArray());
                RefreshPairs();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void RefreshPairs(int? selectPrimaryID = null, int? selectSecondaryID = null)
        {
            isLoading = true;
            try
            {
                pairs = LogShippingPair.GetPairs();
                pairsBindingSource.DataSource = pairs;
            }
            finally
            {
                isLoading = false;
            }
            SelectGridRow(pairs.FirstOrDefault(p => p.PrimaryInstanceID == selectPrimaryID && p.SecondaryInstanceID == selectSecondaryID));
            ShowSelectedPair();
        }

        private LogShippingPair SelectedPair =>
            grid.SelectedRows.Count == 1 ? grid.SelectedRows[0].DataBoundItem as LogShippingPair : null;

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (isLoading) return;
            ShowSelectedPair();
        }

        private void ShowSelectedPair()
        {
            var pair = SelectedPair;
            if (pair == null)
            {
                ClearEditor();
                return;
            }
            isUpdatingEditor = true;
            try
            {
                cboPrimary.SelectedItem = instances.FirstOrDefault(i => i.InstanceID == pair.PrimaryInstanceID);
                cboSecondary.SelectedItem = instances.FirstOrDefault(i => i.InstanceID == pair.SecondaryInstanceID);
            }
            finally
            {
                isUpdatingEditor = false;
            }
            UpdateButtons();
        }

        private void ClearEditor()
        {
            SelectGridRow(null);
            isUpdatingEditor = true;
            try
            {
                cboPrimary.SelectedIndex = -1;
                cboSecondary.SelectedIndex = -1;
            }
            finally
            {
                isUpdatingEditor = false;
            }
            UpdateButtons();
        }

        /// <summary>Selects the grid row for the pair (or clears the selection) without reloading the editor.</summary>
        private void SelectGridRow(LogShippingPair pair)
        {
            isLoading = true;
            try
            {
                grid.ClearSelection();
                var idx = pair == null ? -1 : pairs.IndexOf(pair);
                if (idx >= 0)
                {
                    grid.Rows[idx].Selected = true;
                    grid.CurrentCell = grid.Rows[idx].Cells[0];
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>The pair matching the primary/secondary selected in the editor, if it already exists.</summary>
        private LogShippingPair EditorPair =>
            cboPrimary.SelectedItem is InstanceItem primary && cboSecondary.SelectedItem is InstanceItem secondary
                ? pairs.FirstOrDefault(p => p.PrimaryInstanceID == primary.InstanceID && p.SecondaryInstanceID == secondary.InstanceID)
                : null;

        /// <summary>When the primary/secondary selected matches an existing pair, select it in the grid.</summary>
        private void Instance_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isUpdatingEditor) return;
            SelectGridRow(EditorPair);
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            var exists = EditorPair != null;
            bttnRemove.Enabled = exists;
            bttnSave.Enabled = !exists;
        }

        private void BttnSave_Click(object sender, EventArgs e)
        {
            if (cboPrimary.SelectedItem is not InstanceItem primary || cboSecondary.SelectedItem is not InstanceItem secondary)
            {
                MessageBox.Show("Please select a primary and secondary instance", "Log Shipping Pairs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (primary.InstanceID == secondary.InstanceID)
            {
                MessageBox.Show("Primary and secondary instance must be different", "Log Shipping Pairs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var pair = new LogShippingPair
            {
                PrimaryInstanceID = primary.InstanceID,
                SecondaryInstanceID = secondary.InstanceID
            };
            try
            {
                pair.Save();
                HasChanges = true;
                RefreshPairs(pair.PrimaryInstanceID, pair.SecondaryInstanceID);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void BttnRemove_Click(object sender, EventArgs e)
        {
            var pair = EditorPair;
            if (pair == null) return;
            if (MessageBox.Show($"Remove log shipping pair {pair.PrimaryInstance} => {pair.SecondaryInstance}?", "Log Shipping Pairs",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                pair.Delete();
                HasChanges = true;
                RefreshPairs();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }
}
