using DBADashSharedGUI;
using System.Drawing;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Theme colors for Dark Mode
    /// </summary>
   public  class DarkTheme : BaseTheme
    {
        public override ThemeType ThemeIdentifier => ThemeType.Dark;

        public DarkTheme()
        {
            ForegroundColor = Color.White;
            BackgroundColor = DashColors.TrimbleGray;
            GridGridlineColor = Color.White;
            GridCellForeColor = Color.White;
            GridCellBackColor = DashColors.TrimbleGray;
            GridBackgroundColor = DashColors.TrimbleGray;
            LinkColor = DashColors.BluePale;
            NotApplicableBackColor = DashColors.Gray9;
            NotApplicableForeColor = DashColors.White;
            SearchBackColor = DashColors.Gray9;
            TimeZoneBackColor = DashColors.Gray9;
            TimeZoneForeColor = DashColors.White;
            TitleBackColor = DashColors.Gray9;
            TitleForeColor = DashColors.White;

            TimelineTitleForeColor = DashColors.White;
            TimelineTitleBackColor = DashColors.TrimbleGray;
            TimelineBodyBackColor = DashColors.Gray9;
            TimelineBodyForeColor = DashColors.White;
            TimelineLabelColor = DashColors.White;
            TimelineChartBackColor = DashColors.Gray8;
            TimelineGridColor = DashColors.White;
            TimelineToolTipBackColor = DashColors.TrimbleGray;
            TimelineToolTipForeColor = DashColors.White;

            PanelBackColor = DashColors.TrimbleGray;
            PanelForeColor = DashColors.White;
            ButtonBackColor = DashColors.TrimbleBlueDark;
            ButtonForeColor = DashColors.White;
            ButtonBorderColor = DashColors.TrimbleBlue;
            TreeViewBackColor = DashColors.Gray9;
            TreeViewForeColor = DashColors.White;
            CodeEditorBackColor = DashColors.Gray9;
            CodeEditorForeColor = DashColors.White;
            TabBackColor = DashColors.Gray9;
            TabBorderColor = DashColors.BluePale;
            InputBackColor = DashColors.Gray7;
            InputForeColor = DashColors.White;
        }
    }
}