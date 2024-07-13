using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Professional Color table for menu/tool strip items in dark mode
    /// </summary>
    public class LightModeAltColors : ProfessionalColorTable
    {
        public override Color CheckBackground => DashColors.BlueLight;

        public override Color CheckPressedBackground => DashColors.White;

        public override Color CheckSelectedBackground => DashColors.TrimbleBlue;

        public override Color MenuItemSelected => DashColors.Gray10;

        public override Color ToolStripDropDownBackground => DashColors.TrimbleGray;

        public override Color SeparatorDark => DashColors.Gray8;

        public override Color ImageMarginGradientBegin => DashColors.BluePale;

        public override Color ImageMarginGradientEnd => DashColors.BluePale;

        public override Color ImageMarginGradientMiddle => DashColors.BluePale;
    }
}