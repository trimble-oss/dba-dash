using System.Drawing;
using System.Windows.Forms;
using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Professional Color table for menu/toolstrip items in light mode
    /// </summary>
    public class LightModeColors : ProfessionalColorTable
    {
        public override Color CheckBackground => DashColors.GrayLight;

        public override Color CheckPressedBackground => DashColors.Gray10;

        public override Color CheckSelectedBackground => DashColors.TrimbleGray;

        public override Color MenuItemSelected => DashColors.Gray10;

        public override Color ToolStripDropDownBackground => DashColors.TrimbleGray;

        public override Color SeparatorDark => DashColors.Gray8;

        public override Color ImageMarginGradientBegin => Color.White;

        public override Color ImageMarginGradientEnd => DashColors.Gray1;

        public override Color ImageMarginGradientMiddle => DashColors.Gray0;
    }
}