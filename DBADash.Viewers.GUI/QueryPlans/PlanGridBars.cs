using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// The bar under a figure in the plan viewer's lists, so the big number in a column stands out
    /// without reading every row.  One painter for the statements list and the waits list, so a bar
    /// looks and is coloured the same wherever it appears - and the drawing is the grid's own
    /// <see cref="DataBarPainter"/>, so a bar a user adds to any other grid matches these.
    /// </summary>
    internal static class PlanGridBars
    {
        private static readonly DataBarSettings RampSettings = DataBarSettings.MoreIsWorse();

        /// <summary>
        /// The same low-to-high run the node bars on the plan use, so a colour means the same thing
        /// in both places.  For figures where more is worse: cost, time.  The grid's own
        /// <see cref="DataBarSettings.MoreIsWorse"/> bars, so a bar the plan grids draw themselves
        /// matches the ones they leave to the grid.
        /// </summary>
        public static Color Ramp(double share) => RampSettings.ColorFor(share);

        /// <summary>
        /// <paramref name="value"/> as a share of <paramref name="max"/>, 0 to 1, or null where there
        /// is nothing to draw - no value, or nothing to compare it with.
        /// </summary>
        public static double? Share(object value, double max) => DataBarPainter.Share(value, 0, max);

        /// <summary>
        /// Draw the cell with a bar of <paramref name="share"/> of its width along the bottom, and
        /// mark the event handled.  Does nothing - leaving the grid to paint the cell as usual - when
        /// there is no bar to draw.  <paramref name="colour"/> is the bar's colour; null uses
        /// <see cref="Ramp"/>.
        /// </summary>
        public static void Paint(DataGridViewCellPaintingEventArgs e, double? share, Color? colour = null) =>
            DataBarPainter.Paint(e, share, colour ?? Ramp(share ?? 0));
    }
}
