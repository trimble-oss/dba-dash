using System.Drawing;
using System.Runtime.Versioning;
using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Custom ToolStrip/Menu Renderer for Dark Mode
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DarkModeMenuRenderer : BaseMenuRenderer
    {
        public DarkModeMenuRenderer() : base(new DarkModeColors())
        {
        }

        public override Color MenuBackColor { get; set; } = DashColors.Gray7;

        public override Color MenuForeColor { get; set; } = DashColors.White;
        public override Color ArrowColor { get; set; } = DashColors.White;
        public override Color SeparatorColor { get; set; } = DashColors.Gray8;

        public override Color SelectionColor { get; set; } = DashColors.TrimbleBlue;
    }
}