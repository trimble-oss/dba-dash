using System;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public class ControlResizeHelper : IDisposable
    {
        private bool isResizing = false;
        private int startY;
        private int startHeight;
        private int startNextHeight;
        private Control resizingControl;
        private const int ResizeZoneHeight = 50;
        private const int MinimumHeight = 100;
        private const int ResizeSensitivity = 5;

        public void EnableResizing(Control control)
        {
            if (control == null) return;

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
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            var control = (Control)sender;
            var panelToResize = control is Panel ? control : control.Parent;

            if (panelToResize == null) return;
            if (e.Y < control.Height - ResizeZoneHeight) return;

            if (panelToResize.Parent is TableLayoutPanel tableLayout)
            {
                var position = tableLayout.GetPositionFromControl(panelToResize);
                if (position.Row >= tableLayout.RowCount - 1) return;

                var rowHeights = tableLayout.GetRowHeights();

                startY = control.PointToScreen(e.Location).Y;
                startHeight = rowHeights[position.Row];
                startNextHeight = rowHeights[position.Row + 1];

                isResizing = true;
                resizingControl = panelToResize;
                control.Cursor = Cursors.SizeNS;

                HideChartsInRow(tableLayout, position.Row);
                HideChartsInRow(tableLayout, position.Row + 1);
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
                    var position = tableLayout.GetPositionFromControl(resizingControl);

                    var newHeight = startHeight + totalDelta;
                    var newNextHeight = startNextHeight - totalDelta;

                    if (newHeight < MinimumHeight || newNextHeight < MinimumHeight) return;

                    tableLayout.SuspendLayout();
                    try
                    {
                        tableLayout.RowStyles[position.Row].SizeType = SizeType.Absolute;
                        tableLayout.RowStyles[position.Row].Height = newHeight;
                        tableLayout.RowStyles[position.Row + 1].SizeType = SizeType.Absolute;
                        tableLayout.RowStyles[position.Row + 1].Height = newNextHeight;
                    }
                    finally
                    {
                        tableLayout.ResumeLayout(true);
                    }
                }
            }
            else if (e.Y >= control.Height - ResizeZoneHeight)
            {
                control.Cursor = Cursors.SizeNS;
            }
            else
            {
                control.Cursor = Cursors.Default;
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
                    var position = tableLayout.GetPositionFromControl(resizingControl);
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

                    ShowChartsInRow(tableLayout, position.Row);
                    ShowChartsInRow(tableLayout, position.Row + 1);
                }

                resizingControl = null;
            }
        }

        private void HideChartsInRow(TableLayoutPanel tableLayout, int row)
        {
            var control = tableLayout.GetControlFromPosition(0, row);
            if (control is Panel panel)
            {
                foreach (Control child in panel.Controls)
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
                foreach (Control child in panel.Controls)
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
            // Cleanup if needed
        }
    }
}