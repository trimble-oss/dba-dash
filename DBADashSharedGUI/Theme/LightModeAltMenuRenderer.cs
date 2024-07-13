using System.Runtime.Versioning;
using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Custom ToolStrip/Menu Renderer for Dark Mode
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LightModeAltMenuRenderer : BaseMenuRenderer
    {
        public LightModeAltMenuRenderer() : base(new LightModeAltColors())
        {
        }

        public override Color MenuBackColor { get; set; } = DashColors.BluePale;

        public override Color MenuForeColor { get; set; } = DashColors.TrimbleBlueDark;
        public override Color ArrowColor { get; set; } = DashColors.Gray8;
        public override Color SeparatorColor { get; set; } = DashColors.Gray8;

        public override Color SelectionColor { get; set; } = DashColors.TrimbleBlue;

        public override Color SelectionForeColor { get; set; } = DashColors.White;
    }
}