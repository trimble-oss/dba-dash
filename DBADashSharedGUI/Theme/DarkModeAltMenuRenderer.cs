using System.Drawing;
using System.Runtime.Versioning;
using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Custom ToolStrip/Menu Renderer for Dark Mode
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class DarkModeAltMenuRenderer : LightModeAltMenuRenderer
    {
        public DarkModeAltMenuRenderer() : base()
        {
        }
    }
}