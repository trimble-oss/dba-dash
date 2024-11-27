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
            this.DrawItem += ThemedTabControl_DrawItem;
            this.DrawMode = TabDrawMode.OwnerDrawFixed;

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new(20, 8);
            this.Invalidate();
        }

        private void ThemedTabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            e.Graphics.Clear(theme.TabBackColor);
            var font = this.Font;
            // Draw each TabPage header
            for (var i = 0; i < TabCount; i++)
            {
                var rect = GetTabRect(i);

                var headerColor = theme.TabHeaderBackColor;
                var textColor = theme.TabHeaderForeColor;

                if (i == SelectedIndex)
                {
                    headerColor = theme.SelectedTabBackColor;
                    textColor = theme.SelectedTabForeColor;
                }

                using (Brush brush = new SolidBrush(headerColor))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
                using (var pen = new Pen(theme.TabBorderColor, 0.1f))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }

                using (Brush brush = new SolidBrush(textColor)) // Text color
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    TextRenderer.DrawText(e.Graphics, TabPages[i].Text, font, rect, theme.TabHeaderForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
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