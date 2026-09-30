using DBADash.Viewers.GUI.Properties;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// A DataSet of several tables on one tab: its tables listed down the left with their row counts, and the one
    /// picked shown in a <see cref="GridViewerControl"/> on the right - the same grid, toolbar and all, as a single
    /// result set gets.
    ///
    /// Mostly for troubleshooting the DBA Dash service: what it collects travels as a DataSet of a table per
    /// collection, and one that fails to import is left in its Failed folder as XML.  This reads one of those without
    /// needing a separate tool.  The SSMS extension also hands over all of a query's result sets this way.
    /// </summary>
    public sealed class DataSetViewerControl : UserControl
    {
        private readonly DataSet _dataSet;
        private readonly TextBox _search = new() { Dock = DockStyle.Top, PlaceholderText = "Filter tables" };
        private readonly CheckBox _hideEmpty = new() { Dock = DockStyle.Top, Text = "Hide empty tables", AutoSize = true };
        // A grid rather than a ListView, so the theme styles its header the same as DBA Dash's grids - a ListView's
        // header can't be recoloured short of drawing it.
        private readonly DataGridView _tables = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        private bool _listing;
        private readonly Panel _gridPanel = new() { Dock = DockStyle.Fill };

        // A grid per table, made the first time it's picked and kept, so a filter or sort on one survives looking at
        // another.
        private readonly Dictionary<DataTable, GridViewerControl> _grids = new();

        public DataSetViewerControl(DataSet dataSet, string title)
        {
            _dataSet = dataSet;
            Title = string.IsNullOrWhiteSpace(title) ? "Data Set" : title;

            _tables.Columns.Add(new DataGridViewTextBoxColumn { Name = "Table", HeaderText = "Table", FillWeight = 75 });
            _tables.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Rows",
                HeaderText = "Rows",
                FillWeight = 25,
                ValueType = typeof(int),
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            _tables.SelectionChanged += (_, _) =>
            {
                if (!_listing) ShowSelectedTable();
            };
            _search.TextChanged += (_, _) => ListTables();
            _hideEmpty.CheckedChanged += (_, _) => ListTables();

            // Each grid's own toolbar saves the one table; this saves them all: as one file that opens here again, or as
            // a workbook with a sheet per table.
            var saveAll = new ToolStripDropDownButton("Save All", Resources.Save_16x)
            {
                ToolTipText = "Save every table to one file"
            };
            saveAll.DropDownItems.Add(new ToolStripMenuItem("Data Set File...", Resources.SaveTable_16x, (_, _) => SaveAs())
            {
                ToolTipText = "XML or JSON that opens here again - the XML keeps the column types"
            });
            saveAll.DropDownItems.Add(new ToolStripMenuItem("Excel...", Resources.excel16x16, (_, _) => ExportToExcel())
            {
                ToolTipText = "A workbook with a sheet per table"
            });
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
            toolbar.Items.Add(saveAll);

            var list = new Panel { Dock = DockStyle.Fill };
            list.Controls.Add(_tables);
            list.Controls.Add(_hideEmpty);
            list.Controls.Add(_search);
            list.Controls.Add(toolbar);

            // Sized before the splitter is placed: SplitterDistance throws if it's beyond the container's width, and a
            // new SplitContainer is only 150 wide until docking lays it out.
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                Size = new System.Drawing.Size(1200, 800),
                SplitterDistance = 300
            };
            split.Panel1.Controls.Add(list);
            split.Panel2.Controls.Add(_gridPanel);
            Controls.Add(split);

            ListTables();
            this.ApplyTheme();
        }

        /// <summary>What the tab is called: the file the DataSet came from.</summary>
        public string Title { get; }

        /// <summary>The tab's tooltip: the title, and how many tables there are.</summary>
        public string TabToolTip => $"{Title}\n{_dataSet.Tables.Count:N0} tables";

        /// <summary>
        /// Lists the tables that match the filter, keeping the one picked if it's still listed - or picking the first,
        /// so there's always a grid to look at.
        /// </summary>
        private void ListTables()
        {
            var selected = SelectedTable;
            var search = _search.Text.Trim();

            // Rebuilding the rows moves the selection about on the way; only where it ends up matters.
            _listing = true;
            try
            {
                _tables.Rows.Clear();
                foreach (var table in _dataSet.Tables.Cast<DataTable>()
                             .Where(t => search.Length == 0 || t.TableName.Contains(search, StringComparison.OrdinalIgnoreCase))
                             .Where(t => !_hideEmpty.Checked || t.Rows.Count > 0))
                {
                    var index = _tables.Rows.Add(table.TableName, table.Rows.Count);
                    _tables.Rows[index].Tag = table;
                }

                _tables.ClearSelection();
                var keep = _tables.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Tag == selected)
                           ?? _tables.Rows.Cast<DataGridViewRow>().FirstOrDefault();
                if (keep != null)
                {
                    keep.Selected = true;
                    _tables.CurrentCell = keep.Cells[0];
                }
            }
            finally
            {
                _listing = false;
            }

            ShowSelectedTable();
        }

        /// <summary>
        /// Saves the whole DataSet - every table, whatever the list is filtered to - as one file that opens back on a tab
        /// like this one.
        /// </summary>
        private void SaveAs()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = GridSerializer.SaveDataSetFilter,
                FileName = DefaultFileName() + GridSerializer.CompressedXmlExtension
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                GridSerializer.SaveDataSet(_dataSet, dialog.FileName);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving data set");
            }
        }

        /// <summary>Every table to an Excel workbook, a sheet each - all of them, whatever the list is filtered to.</summary>
        private void ExportToExcel()
        {
            try
            {
                CommonShared.PromptSaveDataSetToXLSX(_dataSet, DefaultFileName());
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error exporting to Excel");
            }
        }

        /// <summary>The title as a file name, without the extension of the file it may already be named after.</summary>
        private string DefaultFileName()
        {
            var name = Title;
            foreach (var extension in new[] { GridSerializer.CompressedXmlExtension, GridSerializer.CompressedJsonExtension,
                         GridSerializer.XmlExtension, GridSerializer.JsonExtension })
            {
                if (!name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;
                name = name[..^extension.Length];
                break;
            }

            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        private DataTable SelectedTable => _tables.SelectedRows.Count > 0 ? (DataTable)_tables.SelectedRows[0].Tag : null;

        private void ShowSelectedTable()
        {
            // Nothing picked - e.g. the filter matches no tables - leaves the last grid showing.
            if (SelectedTable is not { } table) return;

            if (!_grids.TryGetValue(table, out var grid))
            {
                // The SSMS extension's own title for a result set, where it gave one.
                var title = table.ExtendedProperties["Title"] as string;
                grid = new GridViewerControl(table, string.IsNullOrWhiteSpace(title) ? table.TableName : title) { Dock = DockStyle.Fill };
                _grids.Add(table, grid);
            }

            if (_gridPanel.Controls.Count == 1 && _gridPanel.Controls[0] == grid) return;

            _gridPanel.Controls.Clear();
            _gridPanel.Controls.Add(grid);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Only one is on screen; the rest are only here.
                foreach (var grid in _grids.Values) grid.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
