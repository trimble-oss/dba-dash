using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DBADashGUI.Theme
{
    [SupportedOSPlatform("windows")]
    public class ThemedTabControl : TabControl, IThemedControl
    {
        private BaseTheme theme = ThemeExtensions.CurrentTheme;

        public ThemedTabControl()
        {
            this.DrawMode = TabDrawMode.OwnerDrawFixed;

            // Painted entirely in OnPaint (UserPaint) so it's double buffered.  Owner drawing over the native control's
            // own painting flickers - the native control repaints tabs as the mouse moves across them, before each is
            // drawn over again.  The native control still lays out the tabs and handles the mouse.
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            this.Padding = new(20, 8);
            this.Invalidate();
        }

        // The native control measures the tabs with the font it is sent, but WinForms doesn't send one to a UserPaint
        // control - so UserPaint is lifted while the font is sent, or the tabs are sized for the wrong font.
        protected override void OnHandleCreated(EventArgs e)
        {
            SetStyle(ControlStyles.UserPaint, false);
            base.OnHandleCreated(e);
            SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            SetStyle(ControlStyles.UserPaint, false);
            base.OnFontChanged(e);
            SetStyle(ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawTabs(e.Graphics);
            base.OnPaint(e);
        }

        /// <summary>Draw the tab strip.  Overridden to draw more over each tab - e.g. a close button.</summary>
        protected virtual void DrawTabs(Graphics g)
        {
            g.Clear(theme.TabBackColor);
            var font = this.Font;
            // Draw each TabPage header
            for (var i = 0; i < TabCount; i++)
            {
                var rect = GetTabRect(i);

                var headerColor = theme.TabHeaderBackColor;

                if (i == SelectedIndex)
                {
                    headerColor = theme.SelectedTabBackColor;
                }

                using (Brush brush = new SolidBrush(headerColor))
                {
                    g.FillRectangle(brush, rect);
                }
                using (var pen = new Pen(theme.TabBorderColor, 0.1f))
                {
                    g.DrawRectangle(pen, rect);
                }

                TextRenderer.DrawText(g, TabPages[i].Text, font, rect, theme.TabHeaderForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        public void ApplyTheme(BaseTheme theme)
        {
            this.theme = theme;
            Controls.ApplyTheme(theme);
            Invalidate();
        }
    }
}