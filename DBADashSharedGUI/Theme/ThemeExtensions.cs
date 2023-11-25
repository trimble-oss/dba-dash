using DBADashSharedGUI;
using LiveCharts.WinForms;
using System.Runtime.Versioning;
using System.Windows.Media;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;
using Control = System.Windows.Forms.Control;
using TextBox = System.Windows.Forms.TextBox;
using TreeView = System.Windows.Forms.TreeView;
using System;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Provides support for theming controls
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ThemeExtensions
    {
        public static BaseTheme CurrentTheme { get; set; } = new BaseTheme();

        public static int CellToolTipMaxLength { get; set; } = 1000;

        public static void ApplyTheme(this Control control)
        {
            control.ApplyTheme(CurrentTheme);
        }

        public static void ApplyTheme(this Control control, BaseTheme theme)
        {
            switch (control)
            {
                case IThemedControl themedControl:
                    themedControl.ApplyTheme(theme);
                    return;

                case DataGridView dgv:
                    dgv.ApplyTheme(theme);
                    break;

                case TreeView tv:
                    tv.ApplyTheme(theme);
                    break;

                case CartesianChart chart:
                    chart.ApplyTheme(theme);
                    break;

                case PieChart pie:
                    pie.ApplyTheme(theme);
                    break;

                case LinkLabel link:
                    link.ApplyTheme(theme);
                    break;

                case CheckedListBox chkL:
                    chkL.ApplyTheme(theme);
                    break;

                case MenuStrip menu:
                    menu.ApplyTheme(theme);
                    break;

                case ToolStrip menu:
                    menu.ApplyTheme(theme);
                    break;

                case Button btn:
                    btn.ApplyTheme(theme);
                    break;

                case SplitContainer splitter:
                    splitter.ApplyTheme(theme);
                    break;

                case ComboBox combo:
                    combo.ApplyTheme(theme);
                    break;

                case TextBox txt:
                    txt.ApplyTheme(theme);
                    break;

                default:
                    control.BackColor = theme.BackgroundColor;
                    control.ForeColor = theme.ForegroundColor;
                    break;
            }
            // Recursively theme child controls
            control.Controls.ApplyTheme(theme);
        }

        public static void ApplyTheme(this ComboBox combo, BaseTheme theme)
        {
            combo.BackColor = theme.InputBackColor;
            combo.ForeColor = theme.InputForeColor;
            combo.FlatStyle = FlatStyle.Flat;
        }

        public static void ApplyTheme(this Control.ControlCollection controls, BaseTheme theme)
        {
            foreach (Control control in controls)
            {
                control.ApplyTheme(theme);
            }
        }

        public static void ApplyTheme(this SplitContainer splitter, BaseTheme theme)
        {
            splitter.ForeColor = theme.ForegroundColor;
            splitter.BackColor = theme.BackgroundColor;
        }

        public static void ApplyTheme(this ToolStrip menu, BaseTheme theme)
        {
            if (menu.Tag != null && (string)menu.Tag == "ALT")
            {
                menu.Renderer = theme is DarkTheme ? new DarkModeAltMenuRenderer() : new LightModeAltMenuRenderer();
            }
            else
            {
                menu.Renderer = theme is DarkTheme ? new DarkModeMenuRenderer() : new LightModeMenuRenderer();
            }
        }

        public static void ApplyTheme(this MenuStrip menu, BaseTheme theme)
        {
            if (menu.Tag != null && (string)menu.Tag == "ALT")
            {
                menu.Renderer = theme is DarkTheme ? new DarkModeAltMenuRenderer() : new LightModeAltMenuRenderer();
            }
            else
            {
                menu.Renderer = theme is DarkTheme ? new DarkModeMenuRenderer() : new LightModeMenuRenderer();
            }
        }

        public static void ApplyTheme(this CheckedListBox chkL, BaseTheme theme)
        {
            chkL.BackColor = theme.BackgroundColor;
            chkL.ForeColor = theme.ForegroundColor;
        }

        public static void ApplyTheme(this TextBox txt, BaseTheme theme)
        {
            txt.BackColor = theme.InputBackColor;
            txt.ForeColor = theme.InputForeColor;
            txt.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void ApplyTheme(this Button btn, BaseTheme theme)
        {
            btn.BackColor = theme.ButtonBackColor;
            btn.ForeColor = theme.ButtonForeColor;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = theme.ButtonBorderColor;
            btn.FlatAppearance.BorderSize = 1;
        }

        public static void ApplyTheme(this DiffPlex.Wpf.Controls.DiffViewer diff, BaseTheme theme)
        {
            diff.Background = new SolidColorBrush(theme.BackgroundColor.ToMediaColor());
            diff.Foreground = new SolidColorBrush(theme.ForegroundColor.ToMediaColor());
        }

        public static void ApplyTheme(this LiveCharts.WinForms.PieChart chart, BaseTheme theme)
        {
            chart.BackColor = theme.BackgroundColor;
            chart.ForeColor = theme.ForegroundColor;
        }

        public static void ApplyTheme(this LinkLabel link, BaseTheme theme)
        {
            link.LinkColor = theme.LinkColor;
        }

        public static void ApplyTheme(this DataGridView dgv, BaseTheme theme)
        {
            dgv.BackgroundColor = theme.GridBackgroundColor;
            dgv.DefaultCellStyle.BackColor = theme.GridCellBackColor;
            dgv.DefaultCellStyle.ForeColor = theme.GridCellForeColor;
            dgv.DefaultCellStyle.SelectionForeColor = theme.GridCellForeColor;
            dgv.DefaultCellStyle.SelectionBackColor = theme.GridCellBackColor.AdjustBasedOnLuminance();

            dgv.ColumnHeadersDefaultCellStyle.BackColor = theme.ColumnHeaderBackColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = theme.ColumnHeaderForeColor;
            dgv.EnableHeadersVisualStyles = false;

            foreach (var col in dgv.Columns.OfType<DataGridViewLinkColumn>())
            {
                col.LinkColor = theme.LinkColor;
            }
            dgv.ShowCellToolTips = CellToolTipMaxLength > 0;
            dgv.CellFormatting -= TruncateTooltipTextHandler;
            if (CellToolTipMaxLength > 0)
            {
                dgv.CellFormatting += TruncateTooltipTextHandler;
            }
        }

        public static void ApplyTheme(this TreeView tv, BaseTheme theme)
        {
            tv.BackColor = theme.TreeViewBackColor;
            tv.ForeColor = theme.TreeViewForeColor;
        }

        public static void ApplyTheme(this LiveCharts.WinForms.CartesianChart chart, BaseTheme theme)
        {
            chart.BackColor = theme.BackgroundColor;
            chart.ForeColor = theme.ForegroundColor;
            chart.DefaultLegend.Foreground = new SolidColorBrush(theme.ForegroundColor.ToMediaColor());
        }

        public static void TruncateTooltipTextHandler(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                if (e.Value == null) return;
                if (sender is not DataGridView gridView) return;

                var currentTooltip = gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText;

                if (currentTooltip.Length <= CellToolTipMaxLength) return;
                var tooltipText = CellToolTipMaxLength > 0
                    ? string.Concat(currentTooltip.AsSpan(0, CellToolTipMaxLength), "...")
                    : "";

                gridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = tooltipText;
            }
            catch
            {
                Console.WriteLine("Error truncating tooltip text");
            }
        }
    }
}