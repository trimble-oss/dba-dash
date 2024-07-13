using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DBADashGUI
{
    public class CustomProgressBar : ProgressBar
    {
        public CustomProgressBar()
        {
            SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = new(0, 0, Width, Height);
            var scaleFactor = ((Value - (double)Minimum) / (Maximum - (double)Minimum));
            if (ProgressBarRenderer.IsSupported)
                ProgressBarRenderer.DrawHorizontalBar(e.Graphics, rec);
            var width = (int)((rec.Width * scaleFactor) - 4);
            if (width <= 0) { width = 1; }
            rec.Width = width;
            rec.Height = rec.Height > 5 ? rec.Height -= 4 : 1;
            LinearGradientBrush brush = new(rec, ForeColor, BackColor, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, 2, 2, rec.Width, rec.Height);
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var result = base.CreateParams;
                result.ExStyle |= 0x02000000; // WS_EX_COMPOSITED 
                return result;
            }
        }
    }
}
