using System;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.Controls
{
    public class IconGroupBox : GroupBox
    {
        public IconGroupBox()
        {
            // Enable double buffering and user paint to reduce flicker
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
        }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image Icon { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int IconPadding { get; set; } = 4;

        protected override void OnPaint(PaintEventArgs e)
        {
            // custom paint to draw icon next to the text while keeping a standard group box appearance
            e.Graphics.Clear(BackColor);

            var text = Text ?? string.Empty;
            var font = Font;
            var fore = ForeColor;

            var textSize = TextRenderer.MeasureText(text, font);
            int iconW = Icon?.Width ?? 0;
            int iconH = Icon?.Height ?? 0;
            int headerHeight = Math.Max(textSize.Height, iconH);
            headerHeight += 6; // add some padding so header area is tall enough

            int startX = 8;

            // Draw the full border first (below the header area)
            int borderY = Math.Max(1, headerHeight / 2);
            var borderRect = new Rectangle(0, borderY, Width - 1, Height - borderY - 1);
            ControlPaint.DrawBorder(e.Graphics, borderRect, fore, ButtonBorderStyle.Solid);

            // compute mask area to hide the border where the icon and text will be
            int maskLeft = 6;

            // estimate mask width using icon + spacing + text width
            int estimatedStartX = startX + (Icon != null ? (iconW + IconPadding) : 0);
            int maskWidth = (estimatedStartX - maskLeft) + textSize.Width + 8;
            var textBg = new Rectangle(maskLeft - 2, 0, Math.Max(0, maskWidth), headerHeight);
            using (var b = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(b, textBg);
            }

            // now draw icon and text on top of the masked header
            if (Icon != null)
            {
                int iconY = (headerHeight - iconH) / 2;
                e.Graphics.DrawImage(Icon, new Rectangle(startX, iconY, iconW, iconH));
                startX += iconW + IconPadding;
            }

            // vertical position for text (centered in header area)
            int textY = Math.Max(0, (headerHeight - textSize.Height) / 2);

            // Draw text on top of the mask
            TextRenderer.DrawText(e.Graphics, text, font, new Point(startX, textY), fore);
        }
    }
}
