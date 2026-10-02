using DBADashGUI.Controls;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Window for session detail: each session opened from running queries joins the window already open, on a tab
    /// of its own, so several sessions (e.g. a blocker and the sessions it blocks) can be flipped between.  Each tab is a
    /// whole <see cref="SessionDetailControl"/> with its own toolbar and status bar.  The window closes with its last tab.
    ///
    /// The tab strip only appears once a second session is opened.  Tabs are labelled with just the session ID to keep them short.  The instance name is added only when the window
    /// holds sessions from more than one instance, and the snapshot time only when the same session is open more than
    /// once - the full detail is always in the tab's tooltip and in the window title for the tab in front.
    /// </summary>
    public sealed class SessionDetailForm : Form
    {
        private const int MaxInstanceNameLength = 20;

        private readonly DocumentTabControl tabs = new() { Dock = DockStyle.Fill };

        /// <summary>The window sessions open into: the one used last, while it is open.</summary>
        private static SessionDetailForm current;

        private SessionDetailForm()
        {
            Text = "Session Detail";
            ClientSize = new System.Drawing.Size(1000, 964);
            Controls.Add(tabs);

            tabs.CloseRequested += (_, page) => CloseTab(page);
            tabs.SelectedIndexChanged += (_, _) => ShowTitle();
            Activated += (_, _) => current = this;
            this.ApplyTheme();
        }

        private static SessionDetailForm Current =>
            current is { IsDisposed: false }
                ? current
                : Application.OpenForms.OfType<SessionDetailForm>().LastOrDefault();

        /// <summary>
        /// Show a running queries session on a tab of the session detail window, opening the window if there is none.
        /// A session already open for the same snapshot is brought to the front rather than opened a second time.
        /// </summary>
        /// <param name="row">The running queries snapshot row to display.</param>
        /// <param name="context">The current context - used for object execution drill down.</param>
        /// <param name="staleWarning">Optional warning shown in the status bar - see <see cref="SessionDetailControl.StaleWarning"/>.</param>
        /// <param name="newWindow">Open in a new window instead of on a tab of the existing one (e.g. Ctrl+click).</param>
        public static void Open(DataRowView row, DBADashContext context, string staleWarning = null, bool newWindow = false)
        {
            var window = newWindow ? null : Current;
            if (window is null)
            {
                window = current = new SessionDetailForm();
                window.AddSession(row, context, staleWarning);
                window.ShowSingleInstance(forceNewInstance: newWindow);
                return;
            }

            window.AddSession(row, context, staleWarning);
            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
        }

        /// <summary>The session viewers open in this window, with their tabs, in tab order.</summary>
        private IReadOnlyList<(TabPage Page, SessionDetailControl Viewer)> Sessions =>
            tabs.TabPages.Cast<TabPage>()
                .Select(p => (Page: p, Viewer: p.Controls.Count > 0 ? p.Controls[0] as SessionDetailControl : null))
                .Where(s => s.Viewer is not null)
                .ToList();

        private void AddSession(DataRowView row, DBADashContext context, string staleWarning)
        {
            var instanceId = Convert.ToInt32(row["InstanceID"]);
            var sessionId = Convert.ToInt32(row["session_id"]);
            var snapshotDateUtc = Convert.ToDateTime(row["SnapshotDate"]).AppTimeZoneToUtc();

            var open = Sessions.FirstOrDefault(s => s.Viewer.InstanceID == instanceId &&
                                                    s.Viewer.SessionID == sessionId &&
                                                    Math.Abs((s.Viewer.SnapshotDateUtc - snapshotDateUtc).TotalSeconds) < 1);
            if (open.Page is not null)
            {
                tabs.SelectedTab = open.Page;
                return;
            }

            var viewer = new SessionDetailControl(row, context) { StaleWarning = staleWarning, Dock = DockStyle.Fill };
            var page = new TabPage();
            page.Controls.Add(viewer);
            viewer.TitleChanged += (_, _) =>
            {
                RefreshTabLabels();
                if (tabs.SelectedTab == page) ShowTitle();
            };

            tabs.TabPages.Add(page);
            RefreshTabLabels();
            tabs.SelectedTab = page;
            ShowTitle();
        }

        /// <summary>
        /// Label each tab with its session ID, plus whatever else is needed to tell the tabs apart: the instance when the
        /// window spans more than one, and the snapshot time when the same session is open more than once.
        /// The tab strip is hidden while there's only one session - the window title already says which it is.
        /// </summary>
        private void RefreshTabLabels()
        {
            var sessions = Sessions;
            tabs.ShowTabStrip = sessions.Count > 1;
            var multipleInstances = sessions.Select(s => s.Viewer.InstanceID).Distinct().Count() > 1;

            foreach (var group in sessions.GroupBy(s => (s.Viewer.InstanceID, s.Viewer.SessionID)))
            {
                var showSnapshot = group.Count() > 1;
                var spansDays = group.Select(s => s.Viewer.SnapshotDateUtc.ToAppTimeZone().Date).Distinct().Count() > 1;

                foreach (var (page, viewer) in group)
                {
                    var snapshot = viewer.SnapshotDateUtc.ToAppTimeZone();
                    var label = viewer.SessionID.ToString();
                    if (multipleInstances)
                    {
                        label += " · " + ShortInstanceName(viewer.InstanceName);
                    }
                    if (showSnapshot)
                    {
                        label += " @ " + snapshot.ToString(spansDays ? "g" : "T", CultureInfo.CurrentCulture);
                    }

                    page.Text = label;
                    page.ToolTipText = $"Session {viewer.SessionID}\n{viewer.InstanceName}\nSnapshot: {snapshot.ToString(CultureInfo.CurrentCulture)}";
                }
            }
        }

        private static string ShortInstanceName(string name) =>
            string.IsNullOrEmpty(name) || name.Length <= MaxInstanceNameLength
                ? name
                : name[..(MaxInstanceNameLength - 1)] + "…";

        private void CloseTab(TabPage page)
        {
            tabs.TabPages.Remove(page);
            page.Dispose();

            if (tabs.TabCount == 0)
            {
                Close();
                return;
            }

            RefreshTabLabels();
            ShowTitle();
        }

        /// <summary>The window is named for the session in front, so it can be told apart on the taskbar.</summary>
        private void ShowTitle()
        {
            Text = tabs.SelectedTab?.Controls.Count > 0 && tabs.SelectedTab.Controls[0] is SessionDetailControl viewer
                ? viewer.Title
                : "Session Detail";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Close the tab in front, as in a browser or an editor.
            if (keyData is (Keys.Control | Keys.W) or (Keys.Control | Keys.F4) && tabs.SelectedTab is { } page)
            {
                CloseTab(page);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (ReferenceEquals(current, this)) current = null;
        }
    }
}
