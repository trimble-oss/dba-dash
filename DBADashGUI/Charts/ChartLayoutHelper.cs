using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Charts
{
    public static class ChartLayoutHelper
    {
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

        public static void DisposeTableLayoutWithResizablePanels(
            Panel containerPanel,
            ControlResizeHelper resizeHelper,
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

            tableLayout.Dispose();
            containerPanel.Controls.Clear();
        }

        private static void UnhookControlsRecursively(Control control, Action<Control> unhookAction)
        {
            unhookAction(control);

            foreach (Control child in control.Controls)
            {
                UnhookControlsRecursively(child, unhookAction);
            }
        }

        public static Panel CreateResizablePanel(Control childControl, string tag, bool enableResizing, ControlResizeHelper resizeHelper)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Tag = tag };
            panel.Controls.Add(childControl);

            if (enableResizing)
            {
                resizeHelper.EnableResizing(panel);
            }

            return panel;
        }

        public static bool HasLayoutChanged(ref string lastLayoutKey, List<string> metrics, bool showTable)
        {
            var currentLayoutKey = string.Join(",", metrics) + $"|ShowTable:{showTable}";
            var changed = currentLayoutKey != lastLayoutKey;
            lastLayoutKey = currentLayoutKey;
            return changed;
        }
    }
}
