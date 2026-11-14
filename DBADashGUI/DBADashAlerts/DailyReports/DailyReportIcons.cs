using System;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts.DailyReports
{
    /// <summary>Icons shared by the daily report windows</summary>
    internal static class DailyReportIcons
    {
        private static readonly Lazy<Icon> formIcon = new(() => Icon.FromHandle(Properties.Resources.VBReport_16x.GetHicon()));

        /// <summary>The report icon for the daily report windows' title bars</summary>
        public static Icon FormIcon => formIcon.Value;

        /// <summary>Shows the image to the left of the button's text</summary>
        public static T WithImage<T>(this T button, Image image) where T : ButtonBase
        {
            button.Image = image;
            button.ImageAlign = ContentAlignment.MiddleLeft;
            button.TextImageRelation = TextImageRelation.ImageBeforeText;
            return button;
        }
    }
}
