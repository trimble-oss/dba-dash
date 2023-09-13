using System.Drawing;
using DBADashSharedGUI;
using Color = System.Drawing.Color;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Base theme used to style the application.  Provides defaults that can be overridden.
    /// </summary>
    public class BaseTheme
    {
        public virtual ThemeType ThemeIdentifier => ThemeType.Default;

        public Color ForegroundColor { get; set; } = DashColors.TrimbleBlueDark;
        public Color BackgroundColor { get; set; } = DashColors.GrayLight;

        public Color GridGridlineColor { get; set; } = SystemColors.Control;

        public Color GridCellBackColor { get; set; } = DashColors.GrayLight;

        public Color GridCellForeColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color GridBackgroundColor { get; set; } = DashColors.GrayLight;

        public Color LinkColor { get; set; } = DashColors.LinkColor;

        public Color WarningBackColor { get; set; } = DashColors.Warning;
        public Color WarningForeColor { get; set; } = DashColors.Warning.ContrastColor();

        public Color WarningLowBackColor { get; set; } = DashColors.YellowPale;

        public Color WarningLowForeColor { get; set; } = DashColors.YellowPale.ContrastColor();

        public Color CriticalBackColor { get; set; } = DashColors.Fail;

        public Color CriticalForeColor { get; set; } = DashColors.Fail.ContrastColor();

        public Color SuccessBackColor { get; set; } = DashColors.Success;

        public Color SuccessForeColor { get; set; } = DashColors.Success.ContrastColor();

        public Color NotApplicableBackColor { get; set; } = DashColors.NotApplicable;

        public Color NotApplicableForeColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color AcknowledgedBackColor { get; set; } = DashColors.BlueLight;

        public Color AcknowledgedForeColor { get; set; } = DashColors.BlueLight.ContrastColor();

        public Color InformationBackColor { get; set; } = DashColors.Information;
        public Color InformationForeColor { get; set; } = DashColors.Information.ContrastColor();

        public Color SearchBackColor { get; set; } = DashColors.TrimbleBlue;

        public Color TimeZoneBackColor { get; set; } = DashColors.TrimbleBlue;

        public Color TimeZoneForeColor { get; set; } = Color.White;

        public Color TitleBackColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color TitleForeColor { get; set; } = Color.White;

        public Color TimelineTitleForeColor { get; set; } = DashColors.White;

        public Color TimelineTitleBackColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color TimelineBodyBackColor { get; set; } = DashColors.GrayLight;

        public Color TimelineBodyForeColor { get; set; } = Color.Black;

        public Color TimelineLabelColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color TimelineChartBackColor { get; set; } = DashColors.GrayLight;

        public Color TimelineGridColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color TimelineToolTipBackColor { get; set; } = DashColors.TrimbleBlue;

        public Color TimelineToolTipForeColor { get; set; } = DashColors.White;

        public Color PanelBackColor { get; set; } = SystemColors.Control;

        public Color PanelForeColor { get; set; } = SystemColors.ControlText;

        public Color ButtonBackColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color ButtonForeColor { get; set; } = Color.White;

        public Color ButtonBorderColor { get; set; } = DashColors.TrimbleBlue;

        public Color TreeViewBackColor { get; set; } = DashColors.GrayLight;

        public Color TreeViewForeColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color ColumnHeaderBackColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color ColumnHeaderForeColor { get; set; } = DashColors.White;

        public Color CodeEditorBackColor { get; set; } = DashColors.GrayLight;

        public Color CodeEditorForeColor { get; set; } = Color.Black;

        public Color TabHeaderBackColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color TabHeaderForeColor { get; set; } = DashColors.White;

        public Color SelectedTabBackColor { get; set; } = DashColors.BlueLight;

        public Color SelectedTabForeColor { get; set; } = DashColors.White;

        public Color TabBackColor { get; set; } = DashColors.Gray0;

        public Color TabBorderColor { get; set; } = Color.Black;

        public Color InputBackColor { get; set; } = DashColors.White;

        public Color InputForeColor { get; set; } = DashColors.TrimbleBlueDark;

        public Color InputDisabledBackColor { get; set; } = DashColors.GrayLight;
    }
}