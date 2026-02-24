using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    public class ChartLayoutHelper
    {
        public class PanelMaximizeChangedEventArgs : EventArgs
        {
            public Panel Panel { get; }
            public bool IsMaximized { get; }

            public PanelMaximizeChangedEventArgs(Panel panel, bool isMaximized)
            {
                Panel = panel;
                IsMaximized = isMaximized;
            }
        }

        // Saved layout state per TableLayoutPanel
        private sealed class LayoutState
        {
            public List<RowStyle> RowStyles { get; } = new();
            public List<ColumnStyle> ColumnStyles { get; } = new();
            public Dictionary<string, TableLayoutPanelCellPosition> PositionsByKey { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, int> SpansByKey { get; } = new(StringComparer.Ordinal);
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TableLayoutPanel, LayoutState> _stateTable = new();

        private ControlResizeHelper resizeHelper = new ControlResizeHelper();

        public static void SaveRowPercentages(TableLayoutPanel tableLayout, Dictionary<string, float> rowPercentages)
        {
            var totalHeight = tableLayout.Height;
            if (totalHeight == 0) return;

            for (int i = 0; i < tableLayout.RowStyles.Count; i++)
            {
                var control = tableLayout.GetControlFromPosition(0, i);

                if (control is Panel panel && panel.Tag is string key)
                {
                    var pixelHeight = tableLayout.GetRowHeights()[i];
                    var percentage = (pixelHeight / (float)totalHeight) * 100F;
                    rowPercentages[key] = percentage;
                }
            }
        }

        public void DisposeTableLayoutWithResizablePanels(
            Panel containerPanel,
            Action<Control> unhookControlEvents = null)
        {
            var tableLayout = containerPanel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (tableLayout == null) return;

            foreach (Control control in tableLayout.Controls.OfType<Control>().ToArray())
            {
                if (control is Panel panel)
                {
                    resizeHelper.DisableResizing(panel);
                }

                if (unhookControlEvents != null)
                {
                    UnhookControlsRecursively(control, unhookControlEvents);
                }

                control.Dispose();
            }

            // Clear any saved state for this table layout
            ClearState(tableLayout);
            tableLayout.Dispose();
            containerPanel.Controls.Clear();
        }

        private static string GetControlKey(Control ctrl)
        {
            if (ctrl == null) return null;
            // Prefer the control Name as stable key; fall back to runtime hash
            if (!string.IsNullOrEmpty(ctrl.Name)) return ctrl.Name;
            if (ctrl.Tag is string tag && !string.IsNullOrEmpty(tag)) return tag;
            return ctrl.GetHashCode().ToString();
        }

        private static void SaveState(TableLayoutPanel tableLayout)
        {
            if (tableLayout == null) return;
            var state = new LayoutState();
            foreach (RowStyle rs in tableLayout.RowStyles)
                state.RowStyles.Add(new RowStyle(rs.SizeType, rs.Height));
            foreach (ColumnStyle cs in tableLayout.ColumnStyles)
                state.ColumnStyles.Add(new ColumnStyle(cs.SizeType, cs.Width));

            foreach (Control ctrl in tableLayout.Controls)
            {
                var key = GetControlKey(ctrl);
                if (key == null) continue;
                try
                {
                    var pos = tableLayout.GetPositionFromControl(ctrl);
                    state.PositionsByKey[key] = pos;
                }
                catch { }
                try
                {
                    state.SpansByKey[key] = tableLayout.GetColumnSpan(ctrl);
                }
                catch { state.SpansByKey[key] = 1; }
            }

            _stateTable.Remove(tableLayout);
            _stateTable.Add(tableLayout, state);
        }

        private static bool TryRestoreState(TableLayoutPanel tableLayout)
        {
            if (tableLayout == null) return false;
            if (!_stateTable.TryGetValue(tableLayout, out var state)) return false;

            tableLayout.SuspendLayout();
            try
            {
                // restore column then row styles
                if (state.ColumnStyles.Count == tableLayout.ColumnStyles.Count)
                {
                    for (int i = 0; i < tableLayout.ColumnCount; i++)
                    {
                        tableLayout.ColumnStyles[i].SizeType = state.ColumnStyles[i].SizeType;
                        tableLayout.ColumnStyles[i].Width = state.ColumnStyles[i].Width;
                    }
                }
                if (state.RowStyles.Count == tableLayout.RowStyles.Count)
                {
                    for (int i = 0; i < tableLayout.RowCount; i++)
                    {
                        tableLayout.RowStyles[i].SizeType = state.RowStyles[i].SizeType;
                        tableLayout.RowStyles[i].Height = state.RowStyles[i].Height;
                    }
                }

                // restore positions and spans for controls that still exist
                foreach (Control ctrl in tableLayout.Controls)
                {
                    var key = GetControlKey(ctrl);
                    if (key == null) continue;
                    if (state.PositionsByKey.TryGetValue(key, out var pos))
                    {
                        try { tableLayout.SetCellPosition(ctrl, pos); } catch { }
                    }
                    if (state.SpansByKey.TryGetValue(key, out var span))
                    {
                        try { tableLayout.SetColumnSpan(ctrl, Math.Min(span, tableLayout.ColumnCount)); } catch { }
                    }
                    ctrl.Visible = true;
                    var ts = ctrl.Controls.OfType<ToolStrip>().FirstOrDefault();
                    var btn = ts?.Items.OfType<ToolStripButton>().FirstOrDefault();
                    if (btn != null) btn.Text = "+";
                    RefreshCharts(ctrl as Panel);
                }

                return true;
            }
            finally
            {
                tableLayout.ResumeLayout();
                tableLayout.Invalidate();
                _stateTable.Remove(tableLayout);
            }
        }

        private static void ClearState(TableLayoutPanel tableLayout)
        {
            if (tableLayout == null) return;
            _stateTable.Remove(tableLayout);
        }

        private static void UnhookControlsRecursively(Control control, Action<Control> unhookAction)
        {
            unhookAction(control);

            foreach (Control child in control.Controls)
            {
                UnhookControlsRecursively(child, unhookAction);
            }
        }

        public Panel CreateResizablePanel(Control childControl, object tag, bool enableResizing, string title = null, string panelName = null)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Tag = tag };
            // set a stable Name for persistence. If caller supplied a name, use it; otherwise generate a GUID
            panel.Name = !string.IsNullOrEmpty(panelName) ? panelName : "panel_" + Guid.NewGuid().ToString("N");
            var ts = new ToolStrip() { Dock = DockStyle.Top };
            var toggleButton = new ToolStripButton("+")
            {
                Alignment = ToolStripItemAlignment.Right,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
            };
            ts.Items.Add(toggleButton);
            if (!string.IsNullOrEmpty(title))
            {
                ts.Items.Add(new ToolStripLabel(title)
                {
                    Alignment = ToolStripItemAlignment.Right,
                    Font = new Font(ts.Font, FontStyle.Bold),
                });
            }
            toggleButton.Click += (sender, e) => ToggleMaximize(panel, toggleButton);

            panel.Controls.Add(childControl);
            panel.Controls.Add(ts);
            if (enableResizing)
            {
                resizeHelper.EnableResizing(panel);
            }

            return panel;
        }

        private void ToggleMaximize(Panel panel, ToolStripButton toggleButton)
        {
            var tableLayout = panel.Parent as TableLayoutPanel;
            if (tableLayout == null) return;
            // If we're minimizing (toggle is "-"), we don't need to locate the
            // current cell position. Restoring the saved layout will put
            // everything back where it belonged.
            if (toggleButton.Text == "-")
            {
                // Minimize: restore saved state
                var restored = TryRestoreState(tableLayout);
                if (restored)
                {
                    PanelMaximizeChanged?.Invoke(null, new PanelMaximizeChangedEventArgs(panel, false));
                }
                else
                {
                    // No saved state found - ensure toggle state is consistent
                    try { toggleButton.Text = "+"; } catch { }
                }

                return;
            }

            // Maximize: determine the row containing the panel. Prefer the
            // built-in lookup, fall back to a manual search for robustness.
            int targetRow = -1;
            try
            {
                var pos = tableLayout.GetPositionFromControl(panel);
                targetRow = pos.Row;
            }
            catch { }

            if (targetRow < 0)
            {
                for (int r = 0; r < tableLayout.RowCount && targetRow == -1; r++)
                {
                    for (int c = 0; c < tableLayout.ColumnCount; c++)
                    {
                        if (tableLayout.GetControlFromPosition(c, r) == panel)
                        {
                            targetRow = r;
                            break;
                        }
                    }
                }
            }

            if (targetRow < 0)
            {
                // If we still couldn't find the row, abort to avoid corrupting layout
                return;
            }

            // Save state then maximize selected panel
            SaveState(tableLayout);

            // Hide all panels
            foreach (Control ctrl in tableLayout.Controls)
            {
                if (ctrl is Panel pnl)
                {
                    pnl.Visible = false;
                }
            }

            // Move clicked panel to column 0, same row, and span all columns
            try
            {
                tableLayout.SetCellPosition(panel, new TableLayoutPanelCellPosition(0, targetRow));
                tableLayout.SetColumnSpan(panel, tableLayout.ColumnCount);
            }
            catch { }

            // Collapse other rows and expand the target row
            for (int r = 0; r < tableLayout.RowCount; r++)
            {
                if (r == targetRow)
                {
                    tableLayout.RowStyles[r].SizeType = SizeType.Percent;
                    tableLayout.RowStyles[r].Height = 100f;
                    panel.Visible = true;
                    toggleButton.Text = "-";
                    RefreshCharts(panel);
                }
                else
                {
                    tableLayout.RowStyles[r].SizeType = SizeType.Absolute;
                    tableLayout.RowStyles[r].Height = 0f;
                }
            }

            PanelMaximizeChanged?.Invoke(null, new PanelMaximizeChangedEventArgs(panel, true));
        }

        private static void RefreshCharts(Panel pnl)
        {
            pnl.BeginInvoke((Action)(() =>
            {
                pnl.PerformLayout();
                foreach (Control child in pnl.Controls)
                {
                    if (child is LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chart)
                    {
                        chart.Invalidate();
                        chart.Update();
                    }
                }
            }));
        }

        public event EventHandler<PanelMaximizeChangedEventArgs> PanelMaximizeChanged;

        public static bool HasLayoutChanged(ref string lastLayoutKey, List<string> metrics, bool showTable)
        {
            var currentLayoutKey = string.Join(",", metrics) + $"|ShowTable:{showTable}";
            var changed = currentLayoutKey != lastLayoutKey;
            lastLayoutKey = currentLayoutKey;
            return changed;
        }
    }
}