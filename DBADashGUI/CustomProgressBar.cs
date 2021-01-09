using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DBADashGUI
{
    public class CustomProgressBar : ProgressBar
    {
        public CustomProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = new Rectangle(0, 0, this.Width, this.Height);
            double scaleFactor = (((double)Value - (double)Minimum) / ((double)Maximum - (double)Minimum));
            if (ProgressBarRenderer.IsSupported)
                ProgressBarRenderer.DrawHorizontalBar(e.Graphics, rec);
            var width = (int)((rec.Width * scaleFactor) - 4);
            if (width <= 0) { width = 1; }
            rec.Width = width;
            rec.Height = rec.Height>5 ? rec.Height-= 4 : 1;
            LinearGradientBrush brush = new LinearGradientBrush(rec, this.ForeColor, this.BackColor, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, 2, 2, rec.Width, rec.Height);
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams result = base.CreateParams;
                result.ExStyle |= 0x02000000; // WS_EX_COMPOSITED 
                return result;
            }
        }
    }
}
