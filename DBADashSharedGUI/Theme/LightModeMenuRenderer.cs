using System.Runtime.Versioning;
using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Custom ToolStrip/Menu Renderer for Dark Mode
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class LightModeMenuRenderer : BaseMenuRenderer
    {
        public LightModeMenuRenderer() : base(new LightModeColors())
        {
        }

        public override Color MenuBackColor { get; set; } = DashColors.Gray0;

        public override Color MenuForeColor { get; set; } = DashColors.TrimbleBlueDark;
        public override Color ArrowColor { get; set; } = DashColors.TrimbleBlue;
        public override Color SeparatorColor { get; set; } = DashColors.Gray8;

        public override Color SelectionColor { get; set; } = DashColors.TrimbleBlueDark;

        public override Color SelectionForeColor { get; set; } = DashColors.White;
    }
}