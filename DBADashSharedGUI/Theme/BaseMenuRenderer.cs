using System.Runtime.Versioning;
using DBADashSharedGUI;

namespace DBADashGUI.Theme
{
    /// <summary>
    /// Custom ToolStrip/Menu Renderer.  To be inherited by other renderers
    /// </summary>
    [SupportedOSPlatform("windows")]
    public abstract class BaseMenuRenderer : ToolStripProfessionalRenderer
    {
        protected BaseMenuRenderer(ProfessionalColorTable colors) : base(colors)
        {
        }

        public virtual Color MenuBackColor { get; set; } = DashColors.Gray7;

        public virtual Color MenuForeColor { get; set; } = DashColors.White;
        public virtual Color ArrowColor { get; set; } = DashColors.White;
        public virtual Color SeparatorColor { get; set; } = DashColors.Gray8;

        public virtual Color SelectionColor { get; set; } = DashColors.TrimbleBlue;

        public virtual Color SelectionForeColor { get; set; } = DashColors.White;

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var backgroundBrush = new SolidBrush(MenuBackColor);
            e.Graphics.FillRectangle(backgroundBrush, e.AffectedBounds);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is ToolStripStatusLabel)
            {
                // Don't change the color of the status label
            }
            else if (e.Item.Selected || e.Item.Pressed)
            {
                e.TextColor = SelectionForeColor;
            }
            else
            {
                e.TextColor = MenuForeColor;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = ArrowColor;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var separatorPen = new Pen(SeparatorColor, 1))
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);

                if (bounds.Width >= 4)
                    bounds.Inflate(-4, 0);

                var startY = bounds.Height / 2;
                e.Graphics.DrawLine(separatorPen, bounds.Left, startY, bounds.Right - 1, startY);
            }

            base.OnRenderSeparator(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                using var selectionBrush = new SolidBrush(SelectionColor);
                e.Graphics.FillRectangle(selectionBrush, e.Item.ContentRectangle);
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                using var backgroundBrush = new SolidBrush(SelectionColor);
                e.Graphics.FillRectangle(backgroundBrush, e.Item.ContentRectangle);
            }
            else
            {
                using var backgroundBrush = new SolidBrush(MenuBackColor);
                e.Graphics.FillRectangle(backgroundBrush, e.Item.ContentRectangle);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                using var backgroundBrush = new SolidBrush(SelectionColor);
                e.Graphics.FillRectangle(backgroundBrush, e.Item.ContentRectangle);
            }
            else
            {
                using var backgroundBrush = new SolidBrush(MenuBackColor);
                e.Graphics.FillRectangle(backgroundBrush, e.Item.ContentRectangle);
            }
        }
    }
}