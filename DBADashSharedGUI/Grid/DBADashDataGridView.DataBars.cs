using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Data bars: a bar in a numeric column's cells, its length the value's share of the column's range.
    /// Any grid gets them from its context menu; a custom report saves them with the column.
    /// </summary>
    public partial class DBADashDataGridView
    {
        private readonly Dictionary<string, DataBarSettings> _dataBars = new(StringComparer.OrdinalIgnoreCase);

        // The range each bar column is drawn against, worked out on first paint and thrown away whenever
        // the rows change, so a filter or a refresh rescales the bars to what is showing.
        private readonly Dictionary<string, (double Min, double Max)> _dataBarScales = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The data bars on the grid, by <see cref="DataBarKey"/>.</summary>
        public IReadOnlyDictionary<string, DataBarSettings> DataBars => _dataBars;

        /// <summary>
        /// Columns, by <see cref="DataBarKey"/>, whose bars the grid's owner paints itself - measured against
        /// something the grid's data bars can't express.  The Data Bar menu is hidden for them, as a bar set
        /// from it would never show.
        /// </summary>
        public ISet<string> ColumnsWithOwnDataBars { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Raised when the user adds, changes or removes a data bar from the context menu, with the
        /// <see cref="DataBarKey"/> of the column - so a host can keep the bar across a refresh.  The key is
        /// null when several columns changed at once.
        /// </summary>
        public event EventHandler<string> DataBarChanged;

        /// <summary>
        /// Where the host can keep the grid's data bars beyond this session - a custom report saves them with the
        /// report.  When set, the Data Bar menu offers Save Data Bars, which calls it.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action SaveDataBars { get; set; }

        /// <summary>A column the Data Bar menu is offered on: a number, and not one with a bar its owner paints.</summary>
        private bool CanHaveDataBar(DataGridViewColumn column) =>
            column.ValueType?.IsNumericType() == true && !ColumnsWithOwnDataBars.Contains(DataBarKey(column));

        /// <summary>
        /// Give every other visible column that can have a data bar the look of <paramref name="source"/>'s - its style,
        /// colouring and thresholds.  Not its scale: a fixed minimum and maximum belong to the figures in one column,
        /// so each column keeps its own, or scales to its values.  Returns the number of columns changed.
        /// </summary>
        public int CopyDataBarToAllColumns(DataGridViewColumn source)
        {
            if (!_dataBars.TryGetValue(DataBarKey(source), out var template)) return 0;

            var changed = 0;
            foreach (var column in Columns.Cast<DataGridViewColumn>().Where(c => c != source && c.Visible && CanHaveDataBar(c)))
            {
                var key = DataBarKey(column);
                var existing = _dataBars.GetValueOrDefault(key);
                var copy = template.Clone();
                copy.Minimum = existing?.Minimum;
                copy.Maximum = existing?.Maximum;
                SetDataBar(key, copy);
                changed++;
            }
            if (changed > 0) DataBarChanged?.Invoke(this, null);
            return changed;
        }

        /// <summary>
        /// What a column's data bar is keyed on: the data column it shows, so the bar follows the data
        /// when the grid is rebuilt, or the grid column's name when it is not bound.
        /// </summary>
        public static string DataBarKey(DataGridViewColumn column) =>
            string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;

        /// <summary>Add, replace or (with null <paramref name="settings"/>) remove the data bar on a column.</summary>
        public void SetDataBar(string key, DataBarSettings settings)
        {
            if (settings == null) _dataBars.Remove(key);
            else _dataBars[key] = settings;
            _dataBarScales.Remove(key);
            Invalidate();
        }

        /// <summary>Replace all of the grid's data bars, e.g. with the ones saved in a report.</summary>
        public void SetDataBars(IEnumerable<KeyValuePair<string, DataBarSettings>> dataBars)
        {
            _dataBars.Clear();
            _dataBarScales.Clear();
            foreach (var (key, settings) in dataBars)
            {
                if (settings != null) _dataBars[key] = settings;
            }
            Invalidate();
        }

        private void WireDataBars()
        {
            DataBindingComplete += (_, _) => _dataBarScales.Clear();
            RowsAdded += (_, _) => _dataBarScales.Clear();
            RowsRemoved += (_, _) => _dataBarScales.Clear();
            // An edit can move the column's range, so every bar in it may need redrawing, not just the edited cell.
            CellValueChanged += (_, e) =>
            {
                if (_dataBars.Count == 0 || e.ColumnIndex < 0) return;
                var key = DataBarKey(Columns[e.ColumnIndex]);
                if (_dataBarScales.Remove(key)) InvalidateColumn(e.ColumnIndex);
            };
            GridFilterChanged += (_, _) => RescaleDataBars();
        }

        /// <summary>Throw away the bars' scales and redraw them, after the rows on show have changed.</summary>
        private void RescaleDataBars()
        {
            _dataBarScales.Clear();
            Invalidate();
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            // Whoever else paints the grid goes first - a grid with bars of its own keeps them.
            base.OnCellPainting(e);
            if (e.Handled || _dataBars.Count == 0 || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var column = Columns[e.ColumnIndex];
            var key = DataBarKey(column);
            if (!_dataBars.TryGetValue(key, out var settings)) return;

            if (!_dataBarScales.TryGetValue(key, out var scale))
            {
                scale = GetDataBarScale(column, settings);
                _dataBarScales[key] = scale;
            }

            var (min, max) = scale;
            if (max <= min || !DataBarPainter.TryGetNumber(e.Value, out var number)) return;

            // Bars run out from zero - rightwards for positive values, leftwards for negative - or from the
            // end of the scale where it doesn't reach zero.
            var zero = Math.Clamp(0, min, max);
            var from = (zero - min) / (max - min);
            var to = (Math.Clamp(number, min, max) - min) / (max - min);
            // Colour by the value's place in the whole scale, so on a scale of negatives the highest value is still
            // the top of a gradient or traffic light, though its bar is the shortest.
            DataBarPainter.PaintRange(e, from, to, settings.ColorFor(to, to < from), settings.Style);
        }

        /// <summary>
        /// The values an empty and a full bar stand for: the settings' fixed limits where there are any,
        /// otherwise from the lowest value showing to the highest, stretched to take in zero.
        /// </summary>
        private (double Min, double Max) GetDataBarScale(DataGridViewColumn column, DataBarSettings settings)
        {
            if (settings.Minimum.HasValue && settings.Maximum.HasValue)
                return ((double)settings.Minimum.Value, (double)settings.Maximum.Value);

            var low = 0d;
            var high = 0d;
            foreach (var value in ShowingValues(column))
            {
                if (!DataBarPainter.TryGetNumber(value, out var number)) continue;
                if (number < low) low = number;
                if (number > high) high = number;
            }

            return (settings.Minimum.HasValue ? (double)settings.Minimum.Value : low,
                settings.Maximum.HasValue ? (double)settings.Maximum.Value : high);
        }

        /// <summary>
        /// The column's values in the rows on show.  Read from the data view where there is one, which
        /// avoids unsharing every row of a large grid; a group drill hides rows rather than filtering the
        /// view, so it reads the rows themselves.
        /// </summary>
        private IEnumerable<object> ShowingValues(DataGridViewColumn column)
        {
            if (DataSource is DataView dv && !_groupDrillFilterActive && !string.IsNullOrEmpty(column.DataPropertyName)
                && dv.Table?.Columns.Contains(column.DataPropertyName) == true)
            {
                var ordinal = dv.Table.Columns[column.DataPropertyName]!.Ordinal;
                foreach (DataRowView row in dv)
                {
                    yield return row.Row[ordinal];
                }
                yield break;
            }

            for (var i = 0; i < Rows.Count; i++)
            {
                if ((Rows.GetRowState(i) & DataGridViewElementStates.Visible) == 0) continue;
                var row = Rows[i];
                if (row.IsNewRow) continue;
                yield return row.Cells[column.Index].Value;
            }
        }

        private ToolStripMenuItem GetDataBarMenuItem()
        {
            var menuItem = new ToolStripMenuItem("Data Bar", DataBarIcon)
            {
                ToolTipText = "Draw a bar in each cell of the column, sized by its value"
            };

            var underline = new ToolStripMenuItem("Bar Under Value", null, (_, _) => UpdateClickedDataBar(s => s.Style = DataBarStyle.Underline));
            var fill = new ToolStripMenuItem("Fill Behind Value", null, (_, _) => UpdateClickedDataBar(s => s.Style = DataBarStyle.Fill));
            var solid = new ToolStripMenuItem("Solid Colour", null, (_, _) => UpdateClickedDataBar(s => s.ColorMode = DataBarColorMode.Solid));
            var worse = new ToolStripMenuItem("Traffic Light (Higher is Worse)", null, (_, _) => UpdateClickedDataBar(s =>
            {
                s.ColorMode = DataBarColorMode.TrafficLight;
                s.HigherIsBetter = false;
                s.NeutralBelowWarning = false;
            }));
            var better = new ToolStripMenuItem("Traffic Light (Higher is Better)", null, (_, _) => UpdateClickedDataBar(s =>
            {
                s.ColorMode = DataBarColorMode.TrafficLight;
                s.HigherIsBetter = true;
                s.NeutralBelowWarning = false;
            }));
            var gradient = new ToolStripMenuItem("Gradient", null, (_, _) => UpdateClickedDataBar(s => s.ColorMode = DataBarColorMode.Gradient));
            var positiveGood = new ToolStripMenuItem("Positive / Negative (Positive is Good)", null, (_, _) => UpdateClickedDataBar(s =>
            {
                s.ColorMode = DataBarColorMode.PositiveNegative;
                s.HigherIsBetter = true;
            }));
            var positiveBad = new ToolStripMenuItem("Positive / Negative (Positive is Bad)", null, (_, _) => UpdateClickedDataBar(s =>
            {
                s.ColorMode = DataBarColorMode.PositiveNegative;
                s.HigherIsBetter = false;
            }));
            var options = new ToolStripMenuItem("Options...", null, (_, _) => ConfigureClickedDataBar());
            var remove = new ToolStripMenuItem("Remove", null, (_, _) => SetClickedDataBar(null));
            var copyToAll = new ToolStripMenuItem("Copy to All Numeric Columns", null, (_, _) =>
            {
                if (ClickedColumnIndex >= 0 && ClickedColumnIndex < Columns.Count) CopyDataBarToAllColumns(Columns[ClickedColumnIndex]);
            })
            {
                ToolTipText = "The same style and colouring on every number column - each keeps its own scale"
            };
            var removeAll = new ToolStripMenuItem("Remove All Data Bars", null, (_, _) =>
            {
                foreach (var key in _dataBars.Keys.ToList()) SetDataBar(key, null);
                DataBarChanged?.Invoke(this, null);
            });

            var saveSeparator = new ToolStripSeparator();
            var save = new ToolStripMenuItem("Save Data Bars", null, (_, _) => SaveDataBars?.Invoke())
            {
                ToolTipText = "Keep the data bars on every column of the grid"
            };

            menuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                underline, fill, new ToolStripSeparator(),
                solid, worse, better, gradient, positiveGood, positiveBad, new ToolStripSeparator(),
                options, copyToAll, remove, removeAll, saveSeparator, save
            });

            menuItem.DropDownOpening += (_, _) =>
            {
                var current = ClickedDataBar;
                underline.Checked = current?.Style == DataBarStyle.Underline;
                fill.Checked = current?.Style == DataBarStyle.Fill;
                solid.Checked = current?.ColorMode == DataBarColorMode.Solid;
                worse.Checked = current is { ColorMode: DataBarColorMode.TrafficLight, HigherIsBetter: false };
                better.Checked = current is { ColorMode: DataBarColorMode.TrafficLight, HigherIsBetter: true };
                gradient.Checked = current?.ColorMode == DataBarColorMode.Gradient;
                positiveGood.Checked = current is { ColorMode: DataBarColorMode.PositiveNegative, HigherIsBetter: true };
                positiveBad.Checked = current is { ColorMode: DataBarColorMode.PositiveNegative, HigherIsBetter: false };
                remove.Enabled = current != null;
                copyToAll.Visible = current != null && Columns.Cast<DataGridViewColumn>()
                    .Any(c => c.Index != ClickedColumnIndex && c.Visible && CanHaveDataBar(c));
                removeAll.Visible = _dataBars.Count > 1 || (_dataBars.Count == 1 && current == null);
                saveSeparator.Visible = save.Visible = SaveDataBars != null;
            };

            void UpdateVisibility(object sender, DataGridViewCellEventArgs e)
            {
                menuItem.Visible = ClickedColumnIndex >= 0 && ClickedColumnIndex < Columns.Count
                                   && CanHaveDataBar(Columns[ClickedColumnIndex]);
                menuItem.Checked = ClickedDataBar != null;
            }

            CellContextMenuOpening += UpdateVisibility;
            ColumnContextMenuOpening += UpdateVisibility;
            return menuItem;
        }

        private DataBarSettings ClickedDataBar =>
            ClickedColumnIndex >= 0 && ClickedColumnIndex < Columns.Count
                ? _dataBars.GetValueOrDefault(DataBarKey(Columns[ClickedColumnIndex]))
                : null;

        /// <summary>Change one thing about the clicked column's bar, adding a bar first if it has none.</summary>
        private void UpdateClickedDataBar(Action<DataBarSettings> change)
        {
            var settings = ClickedDataBar?.Clone() ?? new DataBarSettings();
            change(settings);
            SetClickedDataBar(settings);
        }

        private void SetClickedDataBar(DataBarSettings settings)
        {
            if (ClickedColumnIndex < 0 || ClickedColumnIndex >= Columns.Count) return;
            var key = DataBarKey(Columns[ClickedColumnIndex]);
            SetDataBar(key, settings);
            DataBarChanged?.Invoke(this, key);
        }

        private void ConfigureClickedDataBar()
        {
            if (ClickedColumnIndex < 0 || ClickedColumnIndex >= Columns.Count) return;
            var column = Columns[ClickedColumnIndex];
            using var frm = new DataBarConfig(column.HeaderText, ClickedDataBar?.Clone());
            if (frm.ShowDialog(this) != DialogResult.OK) return;
            SetClickedDataBar(frm.Settings);
        }

        private static Bitmap _dataBarIcon;

        // Three bars of different lengths - there is no data bar image among the shared resources.
        private static Bitmap DataBarIcon
        {
            get
            {
                if (_dataBarIcon != null) return _dataBarIcon;
                var bmp = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bmp))
                using (var blue = new SolidBrush(DashColors.BlueLight))
                using (var red = new SolidBrush(DashColors.Fail))
                using (var amber = new SolidBrush(DashColors.Warning))
                {
                    g.FillRectangle(amber, 1, 2, 8, 3);
                    g.FillRectangle(red, 1, 7, 14, 3);
                    g.FillRectangle(blue, 1, 12, 5, 3);
                }
                return _dataBarIcon = bmp;
            }
        }
    }
}
