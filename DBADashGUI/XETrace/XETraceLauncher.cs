using DBADash.Messaging;
using System;
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

        /// <summary>
        /// Opens the standalone XE file viewer.  With no <paramref name="path"/> it prompts for a file immediately;
        /// pass a path to open it directly.  Handles native <c>.xel</c> files and DBA Dash-native JSON/XML saves - it
        /// needs no monitored instance, so it's available even without an instance context.
        /// </summary>
        public static void LaunchFileViewer(IWin32Window owner, string path = null)
        {
            var control = new XEFileViewerControl { Dock = DockStyle.Fill };
            var frm = new Form
            {
                Text = "View XE File",
                Width = 1200,
                Height = 850,
                StartPosition = FormStartPosition.CenterParent
            };
            frm.Controls.Add(control);
            frm.Show(owner);
            _ = control.OpenAsync(path); // after Show so the control has a handle for async UI updates
        }

        /// <summary>
        /// Opens a read-only viewer over the events already captured for a persisted ad-hoc trace session (or the whole
        /// merged run when <paramref name="runGroupID"/> is set - the same grouping the QuickXETrace history uses).
        /// Backs the Trace History report's "View Data" link.  Loads the stored events through
        /// <see cref="XEStoredEvents.Expand"/> so the grid matches the live/history views exactly.
        /// </summary>
        public static void LaunchStoredData(IWin32Window owner, long sessionID, Guid? runGroupID, string title)
        {
            var control = new XEResultsControl { Dock = DockStyle.Fill };
            var frm = new Form
            {
                Text = string.IsNullOrEmpty(title) ? "Trace Data" : $"Trace Data - {title}",
                Width = 1200,
                Height = 850,
                StartPosition = FormStartPosition.CenterParent
            };
            frm.Controls.Add(control);
            frm.Show(owner);
            _ = LoadStoredDataAsync(control, sessionID, runGroupID);
        }

        private static async System.Threading.Tasks.Task LoadStoredDataAsync(XEResultsControl control, long sessionID, Guid? runGroupID)
        {
            try
            {
                var stored = runGroupID.HasValue
                    ? await XETraceRepo.GetEventsByRunGroupAsync(runGroupID.Value)
                    : await XETraceRepo.GetEventsAsync(sessionID);
                control.LoadEvents(XEStoredEvents.Expand(stored));
            }
            catch (Exception ex)
            {
                MessageBox.Show(control, ex.Message, "Trace Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
