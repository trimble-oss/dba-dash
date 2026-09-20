using DBADash.QueryPlan.Model;
using DBADashGUI.Controls;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// The query plan window: every plan open, one to a tab.
    ///
    /// Comparing plans is a common reason to open more than one - a fast run and a slow one, the plan
    /// before a change and after it - and a window each for that leaves the reader juggling windows
    /// rather than reading plans.  So a plan opened from anywhere joins the window already open, on
    /// a tab of its own, and the window closes with its last tab.  Each tab is a whole viewer - see
    /// <see cref="QueryPlanViewerControl"/> - so nothing about one plan leaks into another.
    /// </summary>
    public sealed class QueryPlanViewerForm : Form
    {
        private readonly DocumentTabControl _documents = new() { Dock = DockStyle.Fill };

        /// <summary>The window plans open into: the one used last, while it is open.</summary>
        private static QueryPlanViewerForm _current;

        private QueryPlanViewerForm()
        {
            Text = "Query Plan";

            // The application's own icon, taken from the executable rather than added as another
            // copy in the resources.  It matters most when the viewer is standalone - a plan opened
            // from Explorer should look like it came from DBA Dash - and a window with the default
            // form icon looks like a dialog nobody meant to ship.
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                // Not worth failing to open a plan over.
                System.Diagnostics.Debug.WriteLine(ex);
            }

            Width = 1300;
            Height = 820;

            // No parent to centre on when the viewer was opened on its own from a file.
            StartPosition = IsStandalone ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;

            Controls.Add(_documents);

            _documents.CloseRequested += (_, page) => CloseTab(page);
            _documents.SelectedIndexChanged += (_, _) => ShowTitle();

            // Plans dropped on the window open in it, the quickest way to compare a few saved ones.
            AllowDrop = true;
            DragEnter += (_, e) => e.Effect = PlanFiles(e.Data).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None;
            DragDrop += (_, e) =>
            {
                _current = this;
                foreach (var file in PlanFiles(e.Data)) Common.ShowQueryPlanFile(file);
            };

            Activated += (_, _) => _current = this;
            this.ApplyTheme();
        }

        /// <summary>
        /// Set when the viewer is all that is running - a plan opened from Explorer rather than from
        /// the GUI.
        /// </summary>
        internal static bool IsStandalone { get; set; }

        /// <summary>
        /// Show a plan on a tab of the plan window, opening the window if there is none.  A plan that
        /// is already open is brought to the front rather than opened a second time.
        /// </summary>
        public static void Open(ExecutionPlan plan, string sourceXml, string fileName = null, DBADashContext context = null)
        {
            var window = _current is { IsDisposed: false } ? _current : null;

            if (window is null)
            {
                window = _current = new QueryPlanViewerForm();
                window.AddTab(plan, sourceXml, fileName, context);
                window.Show();
                return;
            }

            window.AddTab(plan, sourceXml, fileName, context);

            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
        }

        private void AddTab(ExecutionPlan plan, string sourceXml, string fileName, DBADashContext context)
        {
            var open = _documents.TabPages.Cast<TabPage>()
                .FirstOrDefault(p => p.Controls.Count > 0 && p.Controls[0] is QueryPlanViewerControl viewer &&
                                     string.Equals(viewer.SourceXml, sourceXml, StringComparison.Ordinal));

            if (open is not null)
            {
                _documents.SelectedTab = open;
                return;
            }

            var viewer = new QueryPlanViewerControl(plan, sourceXml, fileName, context) { Dock = DockStyle.Fill };
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

        /// <summary>The window is named for the plan in front, so it can be told apart on the taskbar.</summary>
        private void ShowTitle()
        {
            Text = _documents.SelectedTab is { } page ? "Query Plan - " + page.Text : "Query Plan";
        }

        /// <summary>The files in a drag that the viewer can open, by extension - the same ones the Open dialog offers.</summary>
        private static string[] PlanFiles(IDataObject data) =>
            data?.GetData(DataFormats.FileDrop) is string[] files
                ? files.Where(f => Path.GetExtension(f).ToLowerInvariant() is ".sqlplan" or ".xml").ToArray()
                : [];

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Close the plan in front, as in a browser or an editor.
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
