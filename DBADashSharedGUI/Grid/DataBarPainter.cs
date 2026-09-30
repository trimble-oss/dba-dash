using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Draws a data bar in a grid cell.  One painter for the plan viewer's lists and the bars a user adds
    /// to any grid, so a bar looks the same wherever it appears.
    /// </summary>
    public static class DataBarPainter
    {
        // Below half a percent there is no bar worth drawing, and a two pixel stub under "0.0"
        // reads as a smudge rather than a measurement.
        private const double MinimumShare = 0.005;

        // A fill sits behind the figure, so it is pale enough for the text to read in either theme.
        private const int FillAlpha = 90;

        /// <summary>
        /// <paramref name="value"/>'s place between <paramref name="min"/> and <paramref name="max"/>, 0 to 1,
        /// or null where there is nothing to draw - no value, not a number, or no range to measure it in.
        /// </summary>
        public static double? Share(object value, double min, double max)
        {
            if (max <= min || !TryGetNumber(value, out var number) || number <= min) return null;
            return Math.Min(1, (number - min) / (max - min));
        }

        /// <summary>A cell value as a number to draw a bar for - false for null, text, NaN and infinities.</summary>
        public static bool TryGetNumber(object value, out double number)
        {
            number = 0;
            if (value is null || value == DBNull.Value) return false;
            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                return false;
            }
            return double.IsFinite(number);
        }

        /// <summary>
        /// Draw the cell with a bar filling <paramref name="share"/> of its width, and mark the event
        /// handled.  Does nothing - leaving the grid to paint the cell as usual - when there is no bar to draw.
        /// </summary>
        public static void Paint(DataGridViewCellPaintingEventArgs e, double? share, Color colour, DataBarStyle style = DataBarStyle.Underline)
        {
            if (share is not { } fraction) return;
            PaintRange(e, 0, fraction, colour, style);
        }

        /// <summary>
        /// Draw the cell with a bar between <paramref name="from"/> and <paramref name="to"/>, as shares of its width,
        /// and mark the event handled - a bar out from a zero line part way across, for a scale with negative values.
        /// A zero line is drawn where <paramref name="from"/> isn't the left edge.  Does nothing when the bar is too
        /// short to draw.
        /// </summary>
        public static void PaintRange(DataGridViewCellPaintingEventArgs e, double from, double to, Color colour, DataBarStyle style = DataBarStyle.Underline)
        {
            from = Math.Clamp(from, 0, 1);
            to = Math.Clamp(to, 0, 1);
            var hasAxis = from >= MinimumShare;
            var hasBar = Math.Abs(to - from) >= MinimumShare;
            // The zero line runs down the column through the zeros too, so it reads as one line.
            if (!hasBar && !hasAxis) return;

            e.PaintBackground(e.CellBounds, true);

            var span = e.CellBounds.Width - 8;
            var axis = e.CellBounds.X + 4 + (int)Math.Round(span * from);
            if (hasAxis)
            {
                using var pen = new Pen(Color.FromArgb(120, Color.Gray)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
                e.Graphics.DrawLine(pen, axis, e.CellBounds.Top, axis, e.CellBounds.Bottom - 1);
            }

            if (hasBar) FillBar(e, axis, (int)Math.Round(span * Math.Abs(to - from)), to < from, colour, style);

            e.PaintContent(e.CellBounds);
            e.Handled = true;
        }

        private static void FillBar(DataGridViewCellPaintingEventArgs e, int axis, int length, bool leftward, Color colour, DataBarStyle style)
        {
            var width = Math.Max(2, length);
            var left = leftward ? axis - width : axis;
            if (style == DataBarStyle.Fill)
            {
                var bar = new Rectangle(left, e.CellBounds.Y + 3, width, Math.Max(2, e.CellBounds.Height - 7));
                using var brush = new SolidBrush(Color.FromArgb(FillAlpha, colour));
                e.Graphics.FillRectangle(brush, bar);
            }
            else
            {
                // A row of one line of text has no room under the figure for the full bar, so it gets a thinner one
                // tucked against the bottom rather than one struck through the text.
                var bar = e.CellBounds.Height >= 30
                    ? new Rectangle(left, e.CellBounds.Bottom - 9, width, 5)
                    : new Rectangle(left, e.CellBounds.Bottom - 5, width, 3);
                using var brush = new SolidBrush(colour);
                e.Graphics.FillRectangle(brush, bar);
            }
        }
    }
}
