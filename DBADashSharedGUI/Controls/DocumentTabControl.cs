using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Controls
{
    /// <summary>
    /// Tabs for open documents - a query plan each, say - that the user closes: a × on every tab,
    /// middle click to close, and a right click menu with Close, Close Others and Close All.
    ///
    /// The tab control does not close anything itself.  It raises <see cref="CloseRequested"/> for
    /// the owner, which knows what closing a document means - disposing it, and closing the window
    /// with the last one.
    /// </summary>
    public sealed class DocumentTabControl : ThemedTabControl
    {
        private const int CloseSize = 14;
        private const int CloseMargin = 6;

        private int _hoveredClose = -1;

        public DocumentTabControl()
        {
            // Room for the × after the text, which the base control centres in the tab.
            Padding = new Point(24, 8);
            ShowToolTips = true;

            // After the base control's handler, which clears the strip and draws every tab.
            DrawItem += (_, e) => DrawCloseButtons(e.Graphics);
        }

        /// <summary>A tab asked to close, by its ×, a middle click or the right click menu.</summary>
        public event EventHandler<TabPage> CloseRequested;

        private Rectangle CloseBounds(int index)
        {
            var tab = GetTabRect(index);
            return new Rectangle(tab.Right - CloseMargin - CloseSize, tab.Top + ((tab.Height - CloseSize) / 2), CloseSize, CloseSize);
        }

        private void DrawCloseButtons(Graphics g)
        {
            var theme = ThemeExtensions.CurrentTheme;

            for (var i = 0; i < TabCount; i++)
            {
                var bounds = CloseBounds(i);

                if (i == _hoveredClose)
                {
                    using var hover = new SolidBrush(Color.FromArgb(60, theme.TabHeaderForeColor));
                    g.FillRectangle(hover, bounds);
                }

                using var pen = new Pen(theme.TabHeaderForeColor, 1.4f);
                var inset = 4;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(pen, bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Bottom - inset);
                g.DrawLine(pen, bounds.Right - inset, bounds.Top + inset, bounds.Left + inset, bounds.Bottom - inset);
            }
        }

        private int TabAt(Point location) =>
            Enumerable.Range(0, TabCount).FirstOrDefault(i => GetTabRect(i).Contains(location), -1);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var index = TabAt(e.Location);

            // Closed on the press rather than letting the tab be selected first: selecting a tab only
            // to close it makes the window flash to its contents.
            if (index >= 0 && (e.Button == MouseButtons.Middle ||
                               (e.Button == MouseButtons.Left && CloseBounds(index).Contains(e.Location))))
            {
                CloseRequested?.Invoke(this, TabPages[index]);
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Right) return;

            var index = TabAt(e.Location);
            if (index < 0) return;

            var page = TabPages[index];
            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Close", null, (_, _) => CloseRequested?.Invoke(this, page)) { ShortcutKeyDisplayString = "Ctrl+W" });
            menu.Items.Add(new ToolStripMenuItem("Close Others", null, (_, _) => CloseAll(except: page)) { Enabled = TabCount > 1 });
            menu.Items.Add(new ToolStripMenuItem("Close All", null, (_, _) => CloseAll(except: null)));
            menu.ApplyTheme(ThemeExtensions.CurrentTheme);

            // Disposed once it has closed and its click has been handled.
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(this, e.Location);
        }

        /// <summary>Ask to close every tab, or every tab but <paramref name="except"/>.</summary>
        public void CloseAll(TabPage except)
        {
            foreach (var page in TabPages.Cast<TabPage>().Where(p => !ReferenceEquals(p, except)).ToList())
            {
                CloseRequested?.Invoke(this, page);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var index = TabAt(e.Location);
            var hovered = index >= 0 && CloseBounds(index).Contains(e.Location) ? index : -1;
            if (hovered == _hoveredClose) return;

            _hoveredClose = hovered;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredClose < 0) return;

            _hoveredClose = -1;
            Invalidate();
        }
    }
}
