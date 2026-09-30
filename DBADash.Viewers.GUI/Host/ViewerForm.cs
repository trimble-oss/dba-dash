using DBADash.Deadlock.Model;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashGUI.Controls;
using DBADashGUI.CustomReports;
using DBADashGUI.Deadlocks;
using DBADashGUI.Grids;
using DBADashGUI.QueryPlans;
using DBADashGUI.ShellIntegration;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// The viewer window: every query plan, deadlock graph and result set open, one to a tab, in a single window
    /// rather than one window per file type.
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

        /// <summary>Offered once per process at most - the bar isn't something to see again on every file opened.</summary>
        private static bool _ssmsExtensionUpdateOffered;

        /// <summary>
        /// Offers the SSMS extension this build carries, on a bar across the top of the window, when it's newer than
        /// the one that just handed over a file - one that predates a feature the app now has, e.g. after DBA Dash was
        /// upgraded but the extension wasn't.  Not offered for a version the user chose to skip.
        /// </summary>
        public static void OfferSsmsExtensionUpdate()
        {
            if (_ssmsExtensionUpdateOffered || _current is not { IsDisposed: false } window) return;

            var installed = SsmsExtensionInstaller.LastLaunchedVersion;
            if (!SsmsExtensionInstaller.IsNewerThan(installed)) return;

            var available = SsmsExtensionInstaller.EmbeddedVersion;
            if (ViewerSettings.SsmsExtensionSkippedVersion == available.ToString()) return;

            _ssmsExtensionUpdateOffered = true;
            window.ShowSsmsExtensionUpdateBar(installed, available);
        }

        private void ShowSsmsExtensionUpdateBar(Version installed, Version available)
        {
            var bar = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Dock = DockStyle.Top,
                Name = "SsmsExtensionUpdateBar",
                Renderer = new NoticeBarRenderer(),
                ForeColor = NoticeBarRenderer.Text,
                Padding = new Padding(6, 3, 4, 5)
            };

            // Kept short, so the actions after it are never pushed into the overflow.
            bar.Items.Add(new ToolStripLabel(
                $"A newer DBA Dash SSMS extension is available: {available}" +
                (installed == null ? "." : $" (installed: {installed}).")));

            // The one action the bar is for, as a filled button - the text around it is plainly not clickable.
            var update = new ToolStripButton("Update SSMS Extension...")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Tag = NoticeBarRenderer.PrimaryTag,
                Padding = new Padding(8, 1, 8, 1),
                Margin = new Padding(8, 1, 4, 2),
                ToolTipText = "Runs the SSMS extension installer.  SSMS needs to be closed to finish the update."
            };
            update.Click += (_, _) =>
            {
                RemoveBar();
                ViewerApp.InstallSsmsExtension();
            };
            bar.Items.Add(update);

            var skip = new ToolStripLabel("Skip this version")
            {
                IsLink = true,
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = NoticeBarRenderer.Link,
                ActiveLinkColor = NoticeBarRenderer.Link,
                Margin = new Padding(8, 1, 0, 2),
                ToolTipText = $"Don't offer {available} again"
            };
            skip.Click += (_, _) =>
            {
                RemoveBar();
                ViewerSettings.SsmsExtensionSkippedVersion = available.ToString();
                ViewerSettings.Save();
            };
            bar.Items.Add(skip);

            var close = new ToolStripButton("✕")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Alignment = ToolStripItemAlignment.Right,
                ToolTipText = "Close"
            };
            close.Click += (_, _) => RemoveBar();
            bar.Items.Add(close);

            Controls.Add(bar);
            // The tabs fill what's left below it: docking goes in reverse z-order.
            _documents.BringToFront();

            void RemoveBar()
            {
                Controls.Remove(bar);
                bar.Dispose();
            }
        }

        /// <summary>
        /// A notice bar in Trimble's warning yellows, rather than themed like the toolbars below it, so it stands out
        /// from them - in the dark theme too, as Visual Studio's own yellow info bars do.  A pale background with the
        /// stronger yellow as a rule along the bottom; the primary action a button filled in that yellow, lighter on
        /// hover; a secondary one a link, in Trimble blue.
        /// </summary>
        private sealed class NoticeBarRenderer : ToolStripProfessionalRenderer
        {
            /// <summary>Tags the button drawn as the bar's primary action.</summary>
            internal const string PrimaryTag = "Primary";

            internal static readonly Color Background = ColorTranslator.FromHtml("#fff5e4");
            internal static readonly Color Hover = ColorTranslator.FromHtml("#fec157");
            internal static readonly Color Accent = ColorTranslator.FromHtml("#fbad26");
            internal static readonly Color Link = ColorTranslator.FromHtml("#0063a3");
            // Dark enough to read on all three yellows.
            internal static readonly Color Text = ColorTranslator.FromHtml("#252a2e");

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var brush = new SolidBrush(Background);
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var brush = new SolidBrush(Accent);
                e.Graphics.FillRectangle(brush, 0, e.ToolStrip.Height - 2, e.ToolStrip.Width, 2);
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                var bounds = new Rectangle(Point.Empty, e.Item.Size);

                if (Equals(e.Item.Tag, PrimaryTag))
                {
                    // Filled at rest, so it reads as a button; lighter on hover, the text colour's outline pressed.
                    using var fill = new SolidBrush(e.Item.Selected && !e.Item.Pressed ? Hover : Accent);
                    e.Graphics.FillRectangle(fill, bounds);
                    using var border = new Pen(e.Item.Pressed ? Text : ControlPaint.Dark(Accent, 0.1f));
                    e.Graphics.DrawRectangle(border, 0, 0, bounds.Width - 1, bounds.Height - 1);
                    return;
                }

                if (!e.Item.Selected && !e.Item.Pressed) return;

                using var brush = new SolidBrush(e.Item.Pressed ? Accent : Hover);
                e.Graphics.FillRectangle(brush, bounds);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                // A link label paints in its own LinkColor.
                if (e.Item is not ToolStripLabel { IsLink: true }) e.TextColor = Text;
                base.OnRenderItemText(e);
            }
        }

        /// <summary>
        /// Show a grid file on a new tab of the window, opening it if there is none - a result set, or a DataSet of
        /// several tables.  Always a new tab: unlike a plan or a graph, the same result set opened twice is more likely
        /// a re-run query whose results have moved on.
        ///
        /// The tab and window appear straight away, saying the file is loading, and the file is read off the UI thread:
        /// a result set can run to hundreds of MB, and reading that on the UI thread would hang the window - or the
        /// whole DBA Dash GUI, opening one from its Tools menu.
        /// </summary>
        public static void OpenGridFile(string path)
        {
            var window = _current is { IsDisposed: false } ? _current : null;

            if (window is null)
            {
                window = _current = new ViewerForm();
                window.AddGridFileTab(path);
                window.Show();
                return;
            }

            window.AddGridFileTab(path);

            if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
            window.Activate();
        }

        private void AddGridFileTab(string path)
        {
            var fileName = Path.GetFileName(path);
            var loading = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11F),
                Text = $"Loading {fileName}...",
                UseWaitCursor = true
            };
            var page = AddTab(loading, fileName, path);
            // Built after the form's own ApplyTheme() already ran, as the start tab's content is.
            loading.ApplyTheme();

            // The load is handed back to the window with BeginInvoke, which needs its handle - and a new window isn't
            // shown until after this returns, possibly after a small file has already loaded.
            _ = Handle;
            Task.Run(() => GridSerializer.LoadDataSet(path)).ContinueWith(load =>
            {
                try
                {
                    BeginInvoke(() => ShowLoadedGridFile(page, path, fileName, load));
                }
                catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
                {
                    // The window closed while the file loaded.
                    if (load.Status == TaskStatus.RanToCompletion) load.Result.Dispose();
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Swaps the tab's loading message for the grid, once the file has been read on a background thread.  A file
        /// that couldn't be read is reported and its tab closed - leaving the start tab if it was the only one, as a
        /// file that fails to open from Explorer does.
        /// </summary>
        private void ShowLoadedGridFile(TabPage page, string path, string fileName, Task<DataSet> load)
        {
            try
            {
                var dataSet = load.GetAwaiter().GetResult();

                // Closed while it loaded.
                if (page.IsDisposed)
                {
                    dataSet.Dispose();
                    return;
                }

                Control viewer;
                string title, tooltip;
                if (dataSet.Tables.Count == 1)
                {
                    // The SSMS extension names the tab in the table's extended properties, rather than leaving it
                    // named after its temp file.
                    var table = dataSet.Tables[0];
                    var tableTitle = table.ExtendedProperties["Title"] as string;
                    var grid = new GridViewerControl(table, string.IsNullOrWhiteSpace(tableTitle) ? fileName : tableTitle)
                    {
                        Dock = DockStyle.Fill
                    };
                    (viewer, title, tooltip) = (grid, grid.Title, grid.TabToolTip);
                }
                else
                {
                    // A DataSet the DBA Dash service saved, such as one left in its Failed folder - or all of a query's
                    // result sets from the SSMS extension, which names the tab as it does for one.
                    var dataSetTitle = dataSet.ExtendedProperties["Title"] as string;
                    var dataSetViewer = new DataSetViewerControl(dataSet, string.IsNullOrWhiteSpace(dataSetTitle) ? fileName : dataSetTitle)
                    {
                        Dock = DockStyle.Fill
                    };
                    (viewer, title, tooltip) = (dataSetViewer, dataSetViewer.Title, dataSetViewer.TabToolTip);
                }

                var loading = page.Controls[0];
                page.Controls.Clear();
                loading.Dispose();
                page.Controls.Add(viewer);
                page.Text = title;
                page.ToolTipText = tooltip;
                if (_documents.SelectedTab == page) ShowTitle();
            }
            catch (Exception ex)
            {
                if (!page.IsDisposed)
                {
                    if (_documents.TabCount == 1) AddStartTab();
                    CloseTab(page);
                }

                CommonShared.ShowExceptionDialog(ex, "Error opening grid", text: $"{path} could not be opened as a grid.");
            }
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

        /// <summary>The plan viewers open in this window, in tab order.</summary>
        private IReadOnlyList<QueryPlanViewerControl> PlanViewers =>
            _documents.TabPages.Cast<TabPage>()
                .Select(p => p.Controls.Count > 0 ? p.Controls[0] as QueryPlanViewerControl : null)
                .Where(viewer => viewer is not null)
                .ToList();

        /// <summary>The plan viewers open in the window <paramref name="from"/> is on, for offering something to compare with.</summary>
        internal static IReadOnlyList<QueryPlanViewerControl> PlanViewersBeside(Control from) =>
            from?.FindForm() is ViewerForm window ? window.PlanViewers : [];

        /// <summary>
        /// Every statement of every plan open in the window, as a side a comparison can take.  A plan
        /// of several statements offers each one that has a plan, in batch order.
        /// </summary>
        private IReadOnlyList<PlanCompareSide> ComparableSides() =>
            PlanViewers
                .SelectMany(viewer =>
                {
                    var statements = viewer.Plan.StatementsWithPlans.ToList();
                    if (statements.Count == 0) statements = viewer.Plan.Statements.ToList();
                    return statements.Select(statement => new PlanCompareSide(viewer.Title, viewer.Plan, statement));
                })
                .ToList();

        /// <summary>
        /// Compare the statement shown in <paramref name="before"/> with the one shown in
        /// <paramref name="after"/>, on a tab of their window.  Either side can then be changed to any
        /// statement of any plan open in the window.
        /// </summary>
        internal static void OpenPlanComparison(QueryPlanViewerControl before, QueryPlanViewerControl after, PlanStatement afterStatement = null)
        {
            if (before?.FindForm() is not ViewerForm window || after is null) return;

            var control = new QueryPlanCompareControl(
                new PlanCompareSide(before.Title, before.Plan, before.CurrentStatement),
                new PlanCompareSide(after.Title, after.Plan, afterStatement ?? after.CurrentStatement),
                window.ComparableSides)
            {
                Dock = DockStyle.Fill
            };

            var page = window.AddTab(control, control.Title, control.TabToolTip);
            control.TitleChanged += (_, _) =>
            {
                page.Text = control.Title;
                page.ToolTipText = control.TabToolTip;
                if (window._documents.SelectedTab == page) window.ShowTitle();
            };
        }

        /// <summary>
        /// Opens a plan file on a tab of the window <paramref name="from"/> is on - or finds it, if it's open already - and
        /// returns its viewer.  Null when the file couldn't be opened, or is the plan <paramref name="from"/> already
        /// shows, either of which has been reported.
        /// </summary>
        internal static QueryPlanViewerControl OpenPlanFileBeside(QueryPlanViewerControl from, string path)
        {
            if (from?.FindForm() is not ViewerForm window) return null;

            string sourceXml;
            ExecutionPlan plan;
            try
            {
                // SSMS saves plans as UTF-16 and other tools as UTF-8 - see ViewerLauncher.ShowQueryPlanFile.
                using (var reader = new StreamReader(path, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    sourceXml = reader.ReadToEnd();
                }

                plan = PlanParser.Parse(sourceXml);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error opening query plan", text: $"{path} could not be opened as a query plan.");
                return null;
            }

            // Opening it would only find the tab it was chosen from - say so, rather than doing nothing.
            if (string.Equals(sourceXml, from.SourceXml, StringComparison.Ordinal))
            {
                MessageBox.Show(window,
                    "That file is the plan already open here." +
                    (from.Plan.StatementsWithPlans.Skip(1).Any() ? " To compare two of its statements, choose With Another Statement in This Plan." : string.Empty),
                    "Compare With Query Plan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            window.AddQueryPlanTab(plan, sourceXml, Path.GetFileName(path), ViewerLauncher.DefaultHost);

            return window._documents.SelectedTab?.Controls.Count > 0 ? window._documents.SelectedTab.Controls[0] as QueryPlanViewerControl : null;
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
        private TabPage AddTab(Control viewer, string title, string tooltip)
        {
            RemoveStartTab();

            var page = new TabPage(title) { ToolTipText = tooltip };
            page.Controls.Add(viewer);

            _documents.TabPages.Add(page);
            _documents.SelectedTab = page;
            ShowTitle();
            return page;
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
                Text = "Open a query plan (.sqlplan), deadlock graph (.xdl) or grid file\n\n" +
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

            Icon = content switch
            {
                DeadlockViewerControl => Resources.DeadlockIcon,
                QueryPlanCompareControl => Resources.PlanCompareIcon,
                GridViewerControl or DataSetViewerControl => Resources.GridIcon,
                _ => Resources.PlanViewerIcon
            };
            Text = content switch
            {
                QueryPlanViewerControl => "Query Plan - " + _documents.SelectedTab.Text,
                QueryPlanCompareControl => "Plan Comparison - " + _documents.SelectedTab.Text,
                DeadlockViewerControl => "Deadlock - " + _documents.SelectedTab.Text,
                GridViewerControl => "Grid - " + _documents.SelectedTab.Text,
                DataSetViewerControl => "Data Set - " + _documents.SelectedTab.Text,
                _ => ViewerApp.Identity.DisplayName
            };
        }

        /// <summary>The files in a drag that the window can open, by extension - the same ones the Open dialog offers.</summary>
        private static string[] DroppedFiles(IDataObject data) =>
            data?.GetData(DataFormats.FileDrop) is string[] files
                ? files.Where(ViewerApp.CanOpen).ToArray()
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
