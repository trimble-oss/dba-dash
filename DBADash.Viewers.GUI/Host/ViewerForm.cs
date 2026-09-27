using DBADash.Deadlock.Model;
using DBADash.QueryPlan.Model;
using DBADashGUI.Controls;
using DBADashGUI.Deadlocks;
using DBADashGUI.QueryPlans;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// The viewer window: every query plan and deadlock graph open, one to a tab, in a single window rather than
    /// one window per file type.
    ///
    /// Comparing artifacts is a common reason to open more than one - the plan before a change and after it, a
    /// deadlock and the plan for the statement that lost it - and a window each for that leaves the reader
    /// juggling windows rather than reading them.  So anything opened from anywhere joins the window already
    /// open, on a tab of its own, and the window closes with its last tab.  Each tab is a whole viewer - see
    /// <see cref="QueryPlanViewerControl"/> and <see cref="DeadlockViewerControl"/> - with its own toolbar, so
    /// nothing about one leaks into another and the toolbar shown is simply whichever tab is in front.
    ///
    /// With nothing open yet - <see cref="ShowEmpty"/> - the window shows a placeholder tab to open a file
    /// from instead of one of the real viewers, which it's replaced by once there's something to show.
    /// </summary>
    public sealed class ViewerForm : Form
    {
        private readonly DocumentTabControl _documents = new() { Dock = DockStyle.Fill };

        /// <summary>The window plans and deadlocks open into: the one used last, while it is open.</summary>
        private static ViewerForm _current;

        /// <summary>The placeholder tab shown while the window has nothing else open; removed once it does.</summary>
        private TabPage _startTab;

        private ViewerForm()
        {
            Text = ViewerApp.Identity.DisplayName;
            Icon = Resources.PlanViewerIcon;
            Width = 1300;
            Height = 820;

            // No parent to centre on when the viewer was opened on its own from a file.
            StartPosition = IsStandalone ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;

            Controls.Add(_documents);

            _documents.CloseRequested += (_, page) => CloseTab(page);
            _documents.SelectedIndexChanged += (_, _) => ShowTitle();

            // Plans and graphs dropped on the window open in it, the quickest way to compare a few saved ones -
            // including while the placeholder tab is showing, since neither it nor the tab strip registers as
            // its own drop target, so the drop reaches the form.
            AllowDrop = true;
            DragEnter += (_, e) => e.Effect = DroppedFiles(e.Data).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
            DragDrop += (_, e) =>
            {
                _current = this;
                ViewerApp.OpenFiles(DroppedFiles(e.Data));
            };

            Activated += (_, _) => _current = this;
            this.ApplyTheme();
        }

        /// <summary>
        /// Set when the viewer is all that is running - a file opened from Explorer rather than from the GUI.
        /// </summary>
        public static bool IsStandalone { get; set; }

        /// <summary>
        /// Shows the window with a placeholder tab to open a file from, rather than a real viewer - the
        /// stand-alone viewer's start-up with no file given, or a file that failed to open.  Reuses the window
        /// already open, if there is one, rather than opening a second.
        /// </summary>
        public static void ShowEmpty()
        {
            if (_current is { IsDisposed: false })
            {
                if (_current.WindowState == FormWindowState.Minimized) _current.WindowState = FormWindowState.Normal;
                _current.Activate();
                return;
            }

            _current = new ViewerForm();
            _current.AddStartTab();
            _current.Show();
        }

        /// <summary>
        /// Show a plan on a tab of the window, opening it if there is none.  A plan that is already open is
        /// brought to the front rather than opened a second time.
        /// </summary>
        public static void OpenQueryPlan(ExecutionPlan plan, string sourceXml, string fileName = null, IViewerHost host = null)
        {
            var window = _current is { IsDisposed: false } ? _current : null;

            if (window is null)
            {
                window = _current = new ViewerForm();
                window.AddQueryPlanTab(plan, sourceXml, fileName, host);
                window.Show();
                return;
            }

            window.AddQueryPlanTab(plan, sourceXml, fileName, host);

            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
        }

        /// <summary>
        /// Show a deadlock source on a tab of the window, opening it if there is none.  A source that is
        /// already open is brought to the front rather than opened a second time.
        /// </summary>
        public static void OpenDeadlock(IReadOnlyList<DeadlockGraph> graphs, string sourceXml, string fileName = null,
            IViewerHost host = null)
        {
            var window = _current is { IsDisposed: false } ? _current : null;

            if (window is null)
            {
                window = _current = new ViewerForm();
                window.AddDeadlockTab(graphs, sourceXml, fileName, host);
                window.Show();
                return;
            }

            window.AddDeadlockTab(graphs, sourceXml, fileName, host);

            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
        }

        private void AddQueryPlanTab(ExecutionPlan plan, string sourceXml, string fileName, IViewerHost host)
        {
            var open = _documents.TabPages.Cast<TabPage>()
                .FirstOrDefault(p => p.Controls.Count > 0 && p.Controls[0] is QueryPlanViewerControl viewer &&
                                     string.Equals(viewer.SourceXml, sourceXml, StringComparison.Ordinal));

            if (open is not null)
            {
                _documents.SelectedTab = open;
                return;
            }

            var control = new QueryPlanViewerControl(plan, sourceXml, fileName, host) { Dock = DockStyle.Fill };
            AddTab(control, control.Title, control.TabToolTip);
        }

        private void AddDeadlockTab(IReadOnlyList<DeadlockGraph> graphs, string sourceXml, string fileName, IViewerHost host)
        {
            // Only fold into an existing tab when there is a real key to match on: two graphs opened without a
            // source (null/blank) are not the same graph, and must not collapse into one.
            var open = string.IsNullOrWhiteSpace(sourceXml)
                ? null
                : _documents.TabPages.Cast<TabPage>()
                    .FirstOrDefault(p => p.Controls.Count > 0 && p.Controls[0] is DeadlockViewerControl viewer &&
                                         string.Equals(viewer.SourceXml, sourceXml, StringComparison.Ordinal));

            if (open is not null)
            {
                _documents.SelectedTab = open;
                return;
            }

            var control = new DeadlockViewerControl(graphs, sourceXml, fileName, host) { Dock = DockStyle.Fill };
            AddTab(control, control.Title, control.TabToolTip);
        }

        /// <summary>Adds a viewer on a tab of its own, replacing the placeholder tab if that's all there was.</summary>
        private void AddTab(Control viewer, string title, string tooltip)
        {
            RemoveStartTab();

            var page = new TabPage(title) { ToolTipText = tooltip };
            page.Controls.Add(viewer);

            _documents.TabPages.Add(page);
            _documents.SelectedTab = page;
            ShowTitle();
        }

        /// <summary>The placeholder tab shown when the window is opened with nothing to open - click or drag a file.
        /// Has its own Settings, same as a real tab's, since it's otherwise the only way to reach one before a file
        /// is open.</summary>
        private void AddStartTab()
        {
            var message = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11F),
                Text = "Open a query plan (.sqlplan) or deadlock graph (.xdl)\n\n" +
                       "Click here to browse, or drag a file onto this window."
            };
            message.Click += (_, _) =>
            {
                var files = ViewerApp.PromptForFiles(this);
                if (files.Count > 0) ViewerApp.OpenFiles(files);
            };

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            var settings = new ToolStripDropDownButton("Settings", Resources.SettingsOutline_16x)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Alignment = ToolStripItemAlignment.Right,
                ToolTipText = "Settings"
            };
            ViewerApp.AddFileAssociationMenuItems(settings);
            ViewerApp.AddSettingsMenuItems(settings.DropDownItems);
            toolbar.Items.Add(settings);

            var content = new Panel { Dock = DockStyle.Fill };
            content.Controls.Add(message);
            content.Controls.Add(toolbar);

            _startTab = new TabPage("Start");
            _startTab.Controls.Add(content);

            _documents.TabPages.Add(_startTab);
            _documents.SelectedTab = _startTab;
            ShowTitle();

            // Built after the form's own ApplyTheme() already ran in the constructor, so it's missed unless
            // themed here - the bug that left the placeholder's text unreadable in dark mode.
            content.ApplyTheme();
        }

        private void RemoveStartTab()
        {
            if (_startTab is null) return;

            _documents.TabPages.Remove(_startTab);
            _startTab.Dispose();
            _startTab = null;
        }

        private void CloseTab(TabPage page)
        {
            _documents.TabPages.Remove(page);
            page.Dispose();
            if (ReferenceEquals(_startTab, page)) _startTab = null;

            if (_documents.TabCount == 0) Close();
            else ShowTitle();
        }

        /// <summary>
        /// The window is named, and iconed, for the tab in front, so it can be told apart on the taskbar.
        /// </summary>
        private void ShowTitle()
        {
            var content = _documents.SelectedTab?.Controls.Count > 0 ? _documents.SelectedTab.Controls[0] : null;

            Icon = content is DeadlockViewerControl ? Resources.DeadlockIcon : Resources.PlanViewerIcon;
            Text = content switch
            {
                QueryPlanViewerControl => "Query Plan - " + _documents.SelectedTab.Text,
                DeadlockViewerControl => "Deadlock - " + _documents.SelectedTab.Text,
                _ => ViewerApp.Identity.DisplayName
            };
        }

        /// <summary>The files in a drag that the window can open, by extension - the same ones the Open dialog offers.</summary>
        private static string[] DroppedFiles(IDataObject data) =>
            data?.GetData(DataFormats.FileDrop) is string[] files
                ? files.Where(f => Path.GetExtension(f).ToLowerInvariant() is ".sqlplan" or ".xdl" or ".xml").ToArray()
                : [];

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Close the tab in front, as in a browser or an editor.
            if (keyData is (Keys.Control | Keys.W) or (Keys.Control | Keys.F4) && _documents.SelectedTab is { } page)
            {
                CloseTab(page);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (ReferenceEquals(_current, this)) _current = null;
            Dispose();
        }
    }
}
