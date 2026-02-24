using LiveChartsCore.SkiaSharpView.WinForms;
using System;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public class ControlResizeHelper : IDisposable
    {
        // Track controls we've enabled resizing on so we can reliably remove handlers on Dispose
        private readonly System.Collections.Generic.HashSet<Control> subscribedControls = new();
        private bool _disposed = false;
        private bool isResizing = false;
        private int startY;
        private int startHeight;
        private int startNextHeight;
        private int activeRow;
        private int neighborRow;
        private Control resizingControl;
        private const int ResizeZoneHeight = 50;
        private const int MinimumHeight = 100;
        private const int ResizeSensitivity = 5;

        public void EnableResizing(Control control)
        {
            if (control == null) return;

            // Attach handlers to the container control and its immediate children. We record
            // the top-level control so Dispose can call DisableResizing later.
            lock (subscribedControls)
            {
                if (!subscribedControls.Contains(control))
                {
                    subscribedControls.Add(control);
                }
            }

            control.MouseDown += Control_MouseDown;
            control.MouseMove += Control_MouseMove;
            control.MouseUp += Control_MouseUp;
            control.MouseLeave += Control_MouseLeave;

            foreach (Control child in control.Controls)
            {
                child.MouseDown += Control_MouseDown;
                child.MouseMove += Control_MouseMove;
                child.MouseUp += Control_MouseUp;
                child.MouseLeave += Control_MouseLeave;
            }
        }

        public void DisableResizing(Control control)
        {
            if (control == null) return;

            control.MouseDown -= Control_MouseDown;
            control.MouseMove -= Control_MouseMove;
            control.MouseUp -= Control_MouseUp;
            control.MouseLeave -= Control_MouseLeave;

            foreach (Control child in control.Controls)
            {
                child.MouseDown -= Control_MouseDown;
                child.MouseMove -= Control_MouseMove;
                child.MouseUp -= Control_MouseUp;
                child.MouseLeave -= Control_MouseLeave;
            }

            lock (subscribedControls)
            {
                subscribedControls.Remove(control);
            }
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            var control = (Control)sender;
            var panelToResize = control is Panel ? control : control.Parent;

            if (panelToResize == null) return;
            // Determine mouse position relative to the panel so we can decide if we are near
            // the top or bottom edge of the panel rather than the child control.
            var panelTop = panelToResize.PointToScreen(new System.Drawing.Point(0, 0)).Y;
            var mouseYOnScreen = control.PointToScreen(e.Location).Y;
            var relativeY = mouseYOnScreen - panelTop;

            var nearTopEdge = relativeY <= ResizeZoneHeight;
            var nearBottomEdge = relativeY >= panelToResize.Height - ResizeZoneHeight;
            if (!nearTopEdge && !nearBottomEdge) return;

            if (panelToResize.Parent is TableLayoutPanel tableLayout)
            {
                var position = tableLayout.GetPositionFromControl(panelToResize);
                if (position.Row < 0) return;

                activeRow = position.Row;

                // If we are on the first row and near the top edge, do not allow resize.
                if (activeRow == 0 && nearTopEdge)
                {
                    return;
                }

                // If we are on the last row and near the bottom edge, do not allow resize.
                if (activeRow == tableLayout.RowCount - 1 && nearBottomEdge)
                {
                    return;
                }

                // If we are near the top edge, first try the row above; otherwise try the row below.
                neighborRow = nearTopEdge ? activeRow - 1 : activeRow + 1;
                if (neighborRow < 0 || neighborRow >= tableLayout.RowCount)
                {
                    // Fallback: try the opposite side if the preferred neighbor is not valid.
                    neighborRow = nearTopEdge ? activeRow + 1 : activeRow - 1;
                }
                if (neighborRow < 0 || neighborRow >= tableLayout.RowCount) return;

                var rowHeights = tableLayout.GetRowHeights();

                startY = control.PointToScreen(e.Location).Y;
                startHeight = rowHeights[activeRow];
                startNextHeight = rowHeights[neighborRow];

                isResizing = true;
                resizingControl = panelToResize;
                control.Cursor = Cursors.SizeNS;

                HideChartsInRow(tableLayout, activeRow);
                HideChartsInRow(tableLayout, neighborRow);
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            var control = (Control)sender;

            if (isResizing && resizingControl != null)
            {
                var currentY = control.PointToScreen(e.Location).Y;
                var totalDelta = currentY - startY;

                if (Math.Abs(totalDelta) < ResizeSensitivity)
                {
                    return;
                }

                if (resizingControl.Parent is TableLayoutPanel tableLayout)
                {
                    // For top-edge drags we invert the delta so that dragging "up" decreases
                    // the current row height and increases the row above (more intuitive).
                    var effectiveDelta = totalDelta;
                    var panelTop = resizingControl.PointToScreen(new System.Drawing.Point(0, 0)).Y;
                    var mouseYOnScreen = control.PointToScreen(e.Location).Y;
                    var relativeY = mouseYOnScreen - panelTop;
                    var nearTopEdge = relativeY <= ResizeZoneHeight;

                    if (nearTopEdge)
                    {
                        effectiveDelta = -totalDelta;
                    }

                    var newHeight = startHeight + effectiveDelta;
                    var newNextHeight = startNextHeight - effectiveDelta;

                    if (newHeight < MinimumHeight || newNextHeight < MinimumHeight) return;

                    tableLayout.SuspendLayout();
                    try
                    {
                        tableLayout.RowStyles[activeRow].SizeType = SizeType.Absolute;
                        tableLayout.RowStyles[activeRow].Height = newHeight;
                        tableLayout.RowStyles[neighborRow].SizeType = SizeType.Absolute;
                        tableLayout.RowStyles[neighborRow].Height = newNextHeight;
                    }
                    finally
                    {
                        tableLayout.ResumeLayout(true);
                    }
                }
            }
            else
            {
                // Only show resize cursor when we're near a valid resizable edge.

                bool showResize = false;

                var panel = control is Panel p ? p : control.Parent as Panel;
                if (panel != null && panel.Parent is TableLayoutPanel tableLayout)
                {
                    var position = tableLayout.GetPositionFromControl(panel);
                    if (position.Row >= 0)
                    {
                        var isTopRow = position.Row == 0;
                        var isBottomRow = position.Row == tableLayout.RowCount - 1;

                        // Use mouse position relative to the *panel*, not the child control
                        var panelTop = panel.PointToScreen(new System.Drawing.Point(0, 0)).Y;
                        var mouseYOnScreen = control.PointToScreen(e.Location).Y;
                        var relativeY = mouseYOnScreen - panelTop;

                        var nearTopEdge = relativeY <= ResizeZoneHeight;
                        var nearBottomEdge = relativeY >= panel.Height - ResizeZoneHeight;

                        if (isTopRow)
                        {
                            // For first row, only bottom edge can start a resize.
                            showResize = nearBottomEdge;
                        }
                        else if (isBottomRow)
                        {
                            // For last row, only top edge can start a resize.
                            showResize = nearTopEdge;
                        }
                        else
                        {
                            // For other rows, both top and bottom edges can be used.
                            showResize = nearTopEdge || nearBottomEdge;
                        }
                    }
                }
                else
                {
                    // Fallback: original behavior based only on bottom edge.
                    showResize = e.Y >= control.Height - ResizeZoneHeight;
                }

                control.Cursor = showResize ? Cursors.SizeNS : Cursors.Default;
            }
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isResizing) return;
            isResizing = false;
            if (resizingControl != null)
            {
                ((Control)sender).Cursor = Cursors.Default;

                if (resizingControl.Parent is TableLayoutPanel tableLayout)
                {
                    var totalHeight = tableLayout.Height;
                    var rowHeights = tableLayout.GetRowHeights();

                    tableLayout.SuspendLayout();
                    try
                    {
                        for (int i = 0; i < tableLayout.RowCount; i++)
                        {
                            var percentage = (rowHeights[i] / (float)totalHeight) * 100F;
                            tableLayout.RowStyles[i].SizeType = SizeType.Percent;
                            tableLayout.RowStyles[i].Height = percentage;
                        }
                    }
                    finally
                    {
                        tableLayout.ResumeLayout(true);
                    }

                    ShowChartsInRow(tableLayout, activeRow);
                    ShowChartsInRow(tableLayout, neighborRow);
                }

                resizingControl = null;
            }
        }

        private void HideChartsInRow(TableLayoutPanel tableLayout, int row)
        {
            var control = tableLayout.GetControlFromPosition(0, row);
            if (control is Panel panel)
            {
                foreach (Control child in panel.Controls.OfType<CartesianChart>())
                {
                    child.Visible = false;
                }
            }
        }

        private void ShowChartsInRow(TableLayoutPanel tableLayout, int row)
        {
            if (row >= tableLayout.RowCount) return;

            var control = tableLayout.GetControlFromPosition(0, row);
            if (control is Panel panel)
            {
                foreach (Control child in panel.Controls.OfType<CartesianChart>())
                {
                    child.Visible = true;
                    child.Invalidate(true);
                    child.Refresh();
                }
            }
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            if (!isResizing)
            {
                ((Control)sender).Cursor = Cursors.Default;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Unhook event handlers for any controls we enabled resizing on
                try
                {
                    lock (subscribedControls)
                    {
                        foreach (var ctrl in subscribedControls.ToList())
                        {
                            try { DisableResizing(ctrl); } catch { }
                        }
                        subscribedControls.Clear();
                    }
                }
                catch { }
            }

            _disposed = true;
        }

        ~ControlResizeHelper()
        {
            Dispose(false);
        }
    }
}