using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class SelectPerformanceCounters : Form, IThemedControl
    {
        public SelectPerformanceCounters()
        {
            InitializeComponent();
        }

        public DataTable Counters;

        /// <summary>
        /// When false the "Current" checkbox column will be hidden. Default is true.
        /// Callers can set this property before showing the dialog.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool ShowCurrent { get; set; } = true;

        private Dictionary<int, Counter> selectedCounters;

        private ToolStripMenuItem miCheckSelected;
        private ToolStripMenuItem miUncheckSelected;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<int, Counter> SelectedCounters
        {
            get
            {
                return (from DataRow row in Counters.Rows
                        select new Counter()
                        {
                            CounterID = (int)row["CounterID"],
                            Avg = (bool)row["Avg"],
                            Max = (bool)row["Max"],
                            Min = (bool)row["Min"],
                            SampleCount = (bool)row["SampleCount"],
                            Current = (bool)row["Current"],
                            Total = (bool)row["Total"],
                            CounterName = (string)row["counter_name"],
                            ObjectName = (string)row["object_name"],
                            InstanceName = (string)row["instance_name"]
                        }
                    into ctr
                        where ctr.GetAggColumns(ShowCurrent).Count > 0
                        select ctr).ToDictionary(ctr => ctr.CounterID);
            }
            set => selectedCounters = value;
        }

        private void AddAggSelectionColumns()
        {
            Counters.Columns.Add(new DataColumn("Total", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Avg", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Max", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Min", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Current", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("SampleCount", typeof(bool)) { DefaultValue = false });
        }

        private void SelectPerformanceCounters_Load(object sender, EventArgs e)
        {
            dgvCounters.CurrentCellDirtyStateChanged += DgvCounters_CurrentCellDirtyStateChanged;
            if (Counters == null || Counters.Rows.Count == 0)
            {
                // Use a copy so we don't mutate the shared cached DataTable returned by CommonData.GetCounters()
                Counters = CommonData.GetCounters()?.Copy();
                AddAggSelectionColumns();
                if (selectedCounters is { Count: > 0 })
                {
                    foreach (DataRow row in Counters.Rows)
                    {
                        var counterID = (int)row["CounterID"];
                        if (!selectedCounters.TryGetValue(counterID, out var counter)) continue;
                        row["Total"] = counter.Total;
                        row["Avg"] = counter.Avg;
                        row["Max"] = counter.Max;
                        row["Min"] = counter.Min;
                        row["Current"] = counter.Current;
                        row["SampleCount"] = counter.SampleCount;
                    }
                }
            }
            dgvCounters.AutoGenerateColumns = false;
            dgvCounters.DataSource = new DataView(Counters);
            // Allow callers to hide the "Current" selection when appropriate (e.g., plotting over time)
            colCurrent.Visible = ShowCurrent;
            AddBulkSelectionMenuItems();
        }

        #region Bulk aggregate selection

        /// <summary>
        /// Space toggles every selected checkbox cell at once (not just the current one), so an
        /// aggregate can be applied to a whole block of counters in a single keystroke. The grid
        /// consumes Space before KeyDown fires, so it is intercepted here at the form level.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space && dgvCounters.Focused && dgvCounters.SelectedCells.Count > 1
                && ToggleSelectedCheckboxes())
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>The visible checkbox cells in the current selection.</summary>
        private List<DataGridViewCell> SelectedCheckboxCells() =>
            dgvCounters.SelectedCells.Cast<DataGridViewCell>()
                .Where(c => c is DataGridViewCheckBoxCell && c.OwningColumn.Visible)
                .ToList();

        /// <summary>
        /// Toggles all selected checkbox cells to a single state. The target is the inverse of the
        /// current cell (or, if that isn't a checkbox, the inverse of "all already checked"), so a
        /// mixed selection resolves to a consistent state. Returns false if nothing was toggled.
        /// </summary>
        private bool ToggleSelectedCheckboxes()
        {
            var cells = SelectedCheckboxCells();
            if (cells.Count == 0) return false;
            bool target = dgvCounters.CurrentCell is DataGridViewCheckBoxCell cur && cells.Contains(cur)
                ? !Convert.ToBoolean(cur.Value ?? false)
                : !cells.All(c => Convert.ToBoolean(c.Value ?? false));
            SetCheckboxCells(cells, target);
            return true;
        }

        private void SetCheckboxCells(IEnumerable<DataGridViewCell> cells, bool value)
        {
            foreach (var cell in cells)
            {
                cell.Value = value; // writes straight through to the bound DataRow
            }
            RefreshCheckboxDisplay();
        }

        /// <summary>Sets an aggregate column for every row currently shown (respects the search filter).</summary>
        private void SetColumnForVisibleRows(string dataPropertyName, bool value)
        {
            foreach (DataRowView drv in (DataView)dgvCounters.DataSource)
            {
                drv[dataPropertyName] = value;
            }
            RefreshCheckboxDisplay();
        }

        /// <summary>
        /// Repaints the grid after checkbox values are changed programmatically. The current cell caches
        /// its display value (and may be in edit mode from a prior click), so without this it keeps showing
        /// the old state until focus moves off it.
        /// </summary>
        private void RefreshCheckboxDisplay()
        {
            if (dgvCounters.IsCurrentCellInEditMode)
            {
                dgvCounters.RefreshEdit();
            }
            dgvCounters.Invalidate();
        }

        private void ClearAllAggregates()
        {
            foreach (DataRow row in Counters.Rows)
            {
                row["Total"] = false;
                row["Avg"] = false;
                row["Max"] = false;
                row["Min"] = false;
                row["Current"] = false;
                row["SampleCount"] = false;
            }
            RefreshCheckboxDisplay();
        }

        /// <summary>
        /// Adds the bulk aggregate-selection items to the top of the grid's built-in cell context menu.
        /// The "Check all" submenu is fixed once the columns are known (Current visibility depends on
        /// <see cref="ShowCurrent"/>, already applied before this runs), so only the selection-dependent
        /// items are refreshed each time the menu opens.
        /// </summary>
        private void AddBulkSelectionMenuItems()
        {
            var menu = dgvCounters.CellContextMenu;
            if (menu == null) return;

            miCheckSelected = new ToolStripMenuItem("Check selected", Properties.Resources.Tick_Blue_32x32_72);
            miCheckSelected.Click += (_, _) => SetCheckboxCells(SelectedCheckboxCells(), true);
            miUncheckSelected = new ToolStripMenuItem("Uncheck selected");
            miUncheckSelected.Click += (_, _) => SetCheckboxCells(SelectedCheckboxCells(), false);

            // Check every (filtered) row for a chosen aggregate.
            var checkAll = new ToolStripMenuItem("Check all");
            foreach (DataGridViewColumn col in dgvCounters.Columns)
            {
                if (col is not DataGridViewCheckBoxColumn cb || !cb.Visible) continue;
                var dataPropertyName = cb.DataPropertyName;
                var item = new ToolStripMenuItem(cb.HeaderText);
                item.Click += (_, _) => SetColumnForVisibleRows(dataPropertyName, true);
                checkAll.DropDownItems.Add(item);
            }

            var clearAll = new ToolStripMenuItem("Clear all", Properties.Resources.Close_red_16x);
            clearAll.Click += (_, _) => ClearAllAggregates();

            // Insert at the top so the aggregate actions lead the grid's copy/filter/export items.
            menu.Items.Insert(0, new ToolStripSeparator());
            menu.Items.Insert(0, clearAll);
            menu.Items.Insert(0, checkAll);
            menu.Items.Insert(0, miUncheckSelected);
            menu.Items.Insert(0, miCheckSelected);

            dgvCounters.CellContextMenuOpening += (_, _) =>
            {
                var hasSelection = SelectedCheckboxCells().Count > 0;
                miCheckSelected.Enabled = hasSelection;
                miUncheckSelected.Enabled = hasSelection;
            };
        }

        #endregion Bulk aggregate selection

        private void DgvCounters_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvCounters.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvCounters.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            var dv = (DataView)dgvCounters.DataSource;
            dv.RowFilter = string.Format("(counter_name LIKE '*{0}*' OR object_name LIKE '*{0}*' OR instance_name LIKE '*{0}*')", txtSearch.Text.Replace("'", ""));
        }

        private void BttnClear_Click(object sender, EventArgs e)
        {
            ClearAllAggregates();
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void ApplyTheme(BaseTheme theme)
        {
            foreach (Control child in Controls)
            {
                child.ApplyTheme(theme);
            }
            panel1.BackColor = theme.PanelBackColor;
            panel1.ForeColor = theme.ForegroundColor;
            lblSearch.ForeColor = theme.ForegroundColor;
        }
    }
}