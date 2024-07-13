using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Theme colors for White theme
    /// </summary>
    public class WhiteTheme : BaseTheme
    {
        public override ThemeType ThemeIdentifier => ThemeType.White;

        public WhiteTheme()
        {
            BackgroundColor = Color.White;
            GridCellBackColor = Color.White;
            GridBackgroundColor = Color.White;
            TimelineBodyBackColor = DashColors.White;
            TimelineChartBackColor = DashColors.White;
            TreeViewBackColor = DashColors.White;
            CodeEditorBackColor = DashColors.White;
            TabBackColor = DashColors.White;
        }
    }
}