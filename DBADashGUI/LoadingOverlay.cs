using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI
{
    /// <summary>
    /// Covers a control's content with a "Loading..." message while its data loads, so the previous results aren't
    /// mistaken for the new ones.  The overlay fills the host below any docked toolbars, which stay usable.
    /// It only appears once a load has run for the delay, so a quick refresh doesn't flash it.
    /// </summary>
    public sealed class LoadingOverlay
    {
        private readonly Control host;

        private readonly Label label = new()
        {
            Dock = DockStyle.Fill,
            Visible = false,
            TextAlign = ContentAlignment.MiddleCenter
        };

        private readonly Timer delayTimer;

        public LoadingOverlay(Control host, string text = "Loading...", int delayMs = 500)
        {
            this.host = host;
            label.Text = text;
            host.Controls.Add(label);
            delayTimer = new Timer { Interval = delayMs };
            delayTimer.Tick += (_, _) => ShowNow();
            host.Disposed += (_, _) => delayTimer.Dispose();
        }

        /// <summary>Show the overlay if the load is still running after the delay</summary>
        public void Show()
        {
            delayTimer.Stop();
            delayTimer.Start();
        }

        public void Hide()
        {
            delayTimer.Stop();
            label.Visible = false;
            host.UseWaitCursor = false;
        }

        private void ShowNow()
        {
            delayTimer.Stop();
            if (host.IsDisposed) return;
            // Match the host at the time it's shown, so it follows a theme change
            label.BackColor = host.BackColor;
            label.ForeColor = host.ForeColor;
            label.BringToFront();
            label.Visible = true;
            host.UseWaitCursor = true;
        }
    }
}
