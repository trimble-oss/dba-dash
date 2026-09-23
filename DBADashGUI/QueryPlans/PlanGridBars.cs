using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// The bar under a figure in the plan viewer's lists, so the big number in a column stands out
    /// without reading every row.  One painter for the statements list and the waits list, so a bar
    /// looks and is coloured the same wherever it appears.
    /// </summary>
    internal static class PlanGridBars
    {
        // Below half a percent there is no bar worth drawing, and a two pixel stub under "0.0"
        // reads as a smudge rather than a measurement.
        private const double MinimumShare = 0.005;

        /// <summary>
        /// The same low-to-high run the node bars on the plan use, so a colour means the same thing
        /// in both places.  For figures where more is worse: cost, time.
        /// </summary>
        public static Color Ramp(double share) => share switch
        {
            >= 0.5 => DashColors.Fail,
            >= 0.2 => DashColors.Warning,
            _ => DashColors.BlueLight
        };

        /// <summary>
        /// <paramref name="value"/> as a share of <paramref name="max"/>, 0 to 1, or null where there
        /// is nothing to draw - no value, or nothing to compare it with.
        /// </summary>
        public static double? Share(object value, double max)
        {
            if (value is null || value == DBNull.Value || max <= 0) return null;

            double number;
            try
            {
                number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                return null;
            }

            return number <= 0 ? null : Math.Min(1, number / max);
        }

        /// <summary>
        /// Draw the cell with a bar of <paramref name="share"/> of its width along the bottom, and
        /// mark the event handled.  Does nothing - leaving the grid to paint the cell as usual - when
        /// there is no bar to draw.  <paramref name="colour"/> is the bar's colour; null uses
        /// <see cref="Ramp"/>.
        /// </summary>
        public static void Paint(DataGridViewCellPaintingEventArgs e, double? share, Color? colour = null)
        {
            if (share is not { } fraction || fraction < MinimumShare) return;

            e.PaintBackground(e.CellBounds, true);

            var width = (int)Math.Round((e.CellBounds.Width - 8) * Math.Min(1, fraction));
            var bar = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Bottom - 9, Math.Max(2, width), 5);

            using (var brush = new SolidBrush(colour ?? Ramp(fraction)))
            {
                e.Graphics.FillRectangle(brush, bar);
            }

            e.PaintContent(e.CellBounds);
            e.Handled = true;
        }
    }
}
