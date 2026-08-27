using DBADash.Messaging;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>Opens the ad-hoc XE trace UI (<see cref="QuickXETrace"/>) in a window.  Shared by the Slow Queries
    /// launcher and the Extended Events node so both offer the same "New Ad-hoc XE Trace" action.</summary>
    internal static class XETraceLauncher
    {
        public static void LaunchAdhocTrace(IWin32Window owner, DBADashContext context)
        {
            if (context is not { InstanceID: > 0 }) return;
            var control = new QuickXETrace { Dock = DockStyle.Fill };
            var frm = new Form
            {
                Text = $"Ad-hoc XE Trace - {context.InstanceName}",
                Width = 1200,
                Height = 900,
                StartPosition = FormStartPosition.CenterParent
            };
            frm.Controls.Add(control);
            frm.Show(owner);
            control.SetContext(context); // after Show so the control has a handle for async UI updates
        }

        public static void LaunchWatch(IWin32Window owner, DBADashContext context, string sessionName, int durationSeconds = 0)
        {
            if (context is not { InstanceID: > 0 }) return;
            // A watch is bounded by the service's hard cap (AdhocXEMaxDurationSeconds).  When no explicit duration is
            // requested, watch for exactly the cap so the request isn't silently clamped down server-side.
            if (durationSeconds <= 0) durationSeconds = context.AdhocXEMaxDurationSeconds;
            var control = new XEWatchControl { Dock = DockStyle.Fill };
            var frm = new Form
            {
                Text = $"Watch XE Session - {sessionName} ({context.InstanceName})",
                Width = 1200,
                Height = 850,
                StartPosition = FormStartPosition.CenterParent
            };
            frm.Controls.Add(control);
            frm.Show(owner);
            control.Watch(context, sessionName, durationSeconds); // after Show so the control has a handle for async UI updates
        }

        public static void LaunchViewData(IWin32Window owner, DBADashContext context, string sessionName,
            int maxEvents = XEViewTargetDataMessage.DefaultMaxEvents)
        {
            if (context is not { InstanceID: > 0 }) return;
            var control = new XEViewDataControl { Dock = DockStyle.Fill };
            var frm = new Form
            {
                Text = $"View XE Data - {sessionName} ({context.InstanceName})",
                Width = 1200,
                Height = 850,
                StartPosition = FormStartPosition.CenterParent
            };
            frm.Controls.Add(control);
            frm.Show(owner);
            control.ViewData(context, sessionName, maxEvents); // after Show so the control has a handle for async UI updates
        }
    }
}
