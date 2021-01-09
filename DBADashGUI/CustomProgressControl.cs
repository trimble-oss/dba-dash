using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CustomProgressControl
{
    /// <summary>
    /// A DataGridViewColumn implementation that provides a column that
    /// will display a progress bar.
    /// </summary>
    public class DataGridViewProgressBarColumn : DataGridViewTextBoxColumn
    {
        public DataGridViewProgressBarColumn()
        {
            // Set the cell template
            CellTemplate = new DataGridViewProgressBarCell();

            // Set the default style padding
            Padding pad = new Padding(
              DataGridViewProgressBarCell.STANDARD_HORIZONTAL_MARGIN,
              DataGridViewProgressBarCell.STANDARD_VERTICAL_MARGIN,
              DataGridViewProgressBarCell.STANDARD_HORIZONTAL_MARGIN,
              DataGridViewProgressBarCell.STANDARD_VERTICAL_MARGIN);
            DefaultCellStyle.Padding = pad;

            // Set the default format
            DefaultCellStyle.Format = "0.#\\%";
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }


    }

    /// <summary>
    /// A DataGridViewCell class that is used to display a progress bar
    /// within a grid cell.
    /// </summary>
    public class DataGridViewProgressBarCell : DataGridViewTextBoxCell
    {
        /// <summary>
        /// Standard value to use for horizontal margins
        /// </summary>
        internal const int STANDARD_HORIZONTAL_MARGIN = 4;

        /// <summary>
        /// Standard value to use for vertical margins
        /// </summary>
        internal const int STANDARD_VERTICAL_MARGIN = 4;

        /// <summary>
        /// Constructor sets the expected type to int
        /// </summary>
        public DataGridViewProgressBarCell()
        {
            this.ValueType = typeof(int);
        }

        public Color ProgressBarColorFrom { get; set; } = Color.Azure;
        public Color ProgressBarColorTo { get; set; } = Color.LightSkyBlue;

        /// <summary>
        /// Paints the content of the cell
        /// </summary>
        protected override void Paint(Graphics g, Rectangle clipBounds, Rectangle cellBounds,
          int rowIndex, DataGridViewElementStates cellState,
          object value, object formattedValue,
          string errorText,
          DataGridViewCellStyle cellStyle,
          DataGridViewAdvancedBorderStyle advancedBorderStyle,
          DataGridViewPaintParts paintParts)
        {
            int leftMargin = STANDARD_HORIZONTAL_MARGIN;
            int rightMargin = STANDARD_HORIZONTAL_MARGIN;
            int topMargin = STANDARD_VERTICAL_MARGIN;
            int bottomMargin = STANDARD_VERTICAL_MARGIN;
            int imgHeight = 1;
            int imgWidth = 1;
            int progressWidth = 1;
            PointF fontPlacement = new PointF(0, 0);

            int progressVal;
            if (value != null)
                progressVal = value==DBNull.Value ? 0 : Convert.ToInt32(value);
            else
                progressVal = 0;

            // Draws the cell grid
            base.Paint(g, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, (paintParts & ~DataGridViewPaintParts.ContentForeground));

            // Get margins from the style
            if (null != cellStyle)
            {
                leftMargin = cellStyle.Padding.Left+1;
                rightMargin = cellStyle.Padding.Right+1;
                topMargin = cellStyle.Padding.Top+1;
                bottomMargin = cellStyle.Padding.Bottom+1;
            }

            // Calculate the sizes
            imgHeight = cellBounds.Bottom - cellBounds.Top - (topMargin + bottomMargin);
            imgWidth = cellBounds.Right - cellBounds.Left - (leftMargin + rightMargin);
            if (imgWidth <= 0)
            {
                imgWidth = 1;
            }
            if (imgHeight <= 0)
            {
                imgHeight = 1;
            }

            // Calculate the progress
            progressWidth = (imgWidth * (progressVal) / 100);
            if (progressWidth <= 0)
            {
                if (progressVal > 0)
                {
                    progressWidth = 1;
                }
                else
                {
                    progressWidth = 0;
                }
            }

            // Calculate the font
            if (null != formattedValue)
            {
                SizeF availArea = new SizeF(imgWidth, imgHeight);
                SizeF fontSize = g.MeasureString(formattedValue.ToString(), cellStyle.Font, availArea);

                #region [Font Placement Calc]

                if (null == cellStyle)
                {
                    fontPlacement.Y = cellBounds.Y + topMargin + (((float)imgHeight - fontSize.Height) / 2);
                    fontPlacement.X = cellBounds.X + leftMargin + (((float)imgWidth - fontSize.Width) / 2);
                }
                else
                {
                    // Set the Y vertical position
                    switch (cellStyle.Alignment)
                    {
                        case DataGridViewContentAlignment.BottomCenter:
                        case DataGridViewContentAlignment.BottomLeft:
                        case DataGridViewContentAlignment.BottomRight:
                            {
                                fontPlacement.Y = cellBounds.Y + topMargin + imgHeight - fontSize.Height;
                                break;
                            }
                        case DataGridViewContentAlignment.TopCenter:
                        case DataGridViewContentAlignment.TopLeft:
                        case DataGridViewContentAlignment.TopRight:
                            {
                                fontPlacement.Y = cellBounds.Y + topMargin - fontSize.Height;
                                break;
                            }
                        case DataGridViewContentAlignment.MiddleCenter:
                        case DataGridViewContentAlignment.MiddleLeft:
                        case DataGridViewContentAlignment.MiddleRight:
                        case DataGridViewContentAlignment.NotSet:
                        default:
                            {
                                fontPlacement.Y = cellBounds.Y + topMargin + (((float)imgHeight - fontSize.Height) / 2);
                                break;
                            }
                    }
                    // Set the X horizontal position
                    switch (cellStyle.Alignment)
                    {

                        case DataGridViewContentAlignment.BottomLeft:
                        case DataGridViewContentAlignment.MiddleLeft:
                        case DataGridViewContentAlignment.TopLeft:
                            {
                                fontPlacement.X = cellBounds.X + leftMargin;
                                break;
                            }
                        case DataGridViewContentAlignment.BottomRight:
                        case DataGridViewContentAlignment.MiddleRight:
                        case DataGridViewContentAlignment.TopRight:
                            {
                                fontPlacement.X = cellBounds.X + leftMargin + imgWidth - fontSize.Width;
                                break;
                            }
                        case DataGridViewContentAlignment.BottomCenter:
                        case DataGridViewContentAlignment.MiddleCenter:
                        case DataGridViewContentAlignment.TopCenter:
                        case DataGridViewContentAlignment.NotSet:
                        default:
                            {
                                fontPlacement.X = cellBounds.X + leftMargin + (((float)imgWidth - fontSize.Width) / 2);
                                break;
                            }
                    }
                }
                #endregion [Font Placement Calc]
            }

            //Draw the background
           Rectangle backRectangle = new Rectangle(cellBounds.X + leftMargin, cellBounds.Y + topMargin, imgWidth, imgHeight);
            using (SolidBrush backgroundBrush = new SolidBrush(cellStyle.BackColor))
            {
                g.FillRectangle(backgroundBrush, backRectangle);
            }

            // Draw the progress bar
            if (progressWidth > 0)
            {
                Rectangle progressRectangle = new Rectangle(cellBounds.X + leftMargin, cellBounds.Y + topMargin, progressWidth, imgHeight);
                using (LinearGradientBrush progressGradientBrush = new LinearGradientBrush(progressRectangle, ProgressBarColorFrom, ProgressBarColorTo, LinearGradientMode.Vertical))
                {
                    progressGradientBrush.SetBlendTriangularShape((float).5);
                    g.FillRectangle(progressGradientBrush, progressRectangle);
                }
            }

            // Draw the text
            if (null != formattedValue && null != cellStyle)
            {
                using (SolidBrush fontBrush = new SolidBrush(cellStyle.ForeColor))
                {
                    g.DrawString(formattedValue.ToString(), cellStyle.Font, fontBrush, fontPlacement);
                }
            }

        }
    }

}