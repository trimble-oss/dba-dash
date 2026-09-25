using DBADashGUI.Viewers;
using DBADash.Deadlock.Model;
using DBADashGUI.Controls;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// The deadlock window: every source open, one to a tab.
    ///
    /// Comparing deadlocks is a common reason to open more than one - the same deadlock before a
    /// change and after it, two graphs from the same incident - and a window each for that leaves
    /// the reader juggling windows rather than reading graphs.  So a deadlock opened from anywhere
    /// joins the window already open, on a tab of its own, and the window closes with its last tab.
    /// Each tab is a whole viewer - see <see cref="DeadlockViewerControl"/> - so nothing about one
    /// source leaks into another.
    /// </summary>
    public sealed class DeadlockViewerForm : Form
    {
        private readonly DocumentTabControl _documents = new() { Dock = DockStyle.Fill };

        /// <summary>The window deadlocks open into: the one used last, while it is open.</summary>
        private static DeadlockViewerForm _current;

        private DeadlockViewerForm()
        {
            Text = "Deadlock";
            Icon = Resources.DeadlockIcon;
            Width = 1100;
            Height = 780;

            // No parent to centre on when the viewer was opened on its own from a file.
            StartPosition = IsStandalone ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;

            Controls.Add(_documents);

            _documents.CloseRequested += (_, page) => CloseTab(page);
            _documents.SelectedIndexChanged += (_, _) => ShowTitle();

            // Graphs dropped on the window open in it, the quickest way to compare a few saved ones.
            AllowDrop = true;
            DragEnter += (_, e) => e.Effect = DeadlockFiles(e.Data).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
            DragDrop += (_, e) =>
            {
                _current = this;
                foreach (var file in DeadlockFiles(e.Data)) ViewerLauncher.ShowDeadlockGraphFile(file);
            };

            Activated += (_, _) => _current = this;
            this.ApplyTheme();
        }

        /// <summary>
        /// Set when the viewer is all that is running - a graph opened from Explorer rather than from
        /// the GUI.
        /// </summary>
        public static bool IsStandalone { get; set; }

        /// <summary>
        /// Show a deadlock source on a tab of the deadlock window, opening the window if there is none.
        /// A source that is already open is brought to the front rather than opened a second time.
        /// </summary>
        public static void Open(IReadOnlyList<DeadlockGraph> graphs, string sourceXml, string fileName = null,
            IViewerHost host = null)
        {
            var window = _current is { IsDisposed: false } ? _current : null;

            if (window is null)
            {
                window = _current = new DeadlockViewerForm();
                window.AddTab(graphs, sourceXml, fileName, host);
                window.Show();
                return;
            }

            window.AddTab(graphs, sourceXml, fileName, host);

            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
        }

        private void AddTab(IReadOnlyList<DeadlockGraph> graphs, string sourceXml, string fileName, IViewerHost host)
        {
            // Only fold into an existing tab when there is a real key to match on: two graphs opened
            // without a source (null/blank) are not the same graph, and must not collapse into one.
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

            var viewer = new DeadlockViewerControl(graphs, sourceXml, fileName, host) { Dock = DockStyle.Fill };
            var page = new TabPage(viewer.Title) { ToolTipText = viewer.TabToolTip };
            page.Controls.Add(viewer);

            _documents.TabPages.Add(page);
            _documents.SelectedTab = page;
            ShowTitle();
        }

        private void CloseTab(TabPage page)
        {
            _documents.TabPages.Remove(page);
            page.Dispose();

            if (_documents.TabCount == 0) Close();
            else ShowTitle();
        }

        /// <summary>The window is named for the deadlock in front, so it can be told apart on the taskbar.</summary>
        private void ShowTitle()
        {
            Text = _documents.SelectedTab is { } page ? "Deadlock - " + page.Text : "Deadlock";
        }

        /// <summary>The files in a drag that the viewer can open, by extension - the same ones the Open dialog offers.</summary>
        private static string[] DeadlockFiles(IDataObject data) =>
            data?.GetData(DataFormats.FileDrop) is string[] files
                ? files.Where(f => Path.GetExtension(f).ToLowerInvariant() is ".xdl" or ".xml").ToArray()
                : [];

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Close the deadlock in front, as in a browser or an editor.
            var isCloseHotkey = keyData is (Keys.Control | Keys.W) or (Keys.Control | Keys.F4);
            if (isCloseHotkey && _documents.SelectedTab is { } page)
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
