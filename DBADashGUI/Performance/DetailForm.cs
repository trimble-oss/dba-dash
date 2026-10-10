using DBADashGUI.Controls;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Window for object, query hash and plan hash detail: each item opened from a link joins the window already open, on
    /// a tab of its own, so several (e.g. a procedure and the query shapes in it) can be flipped between.  Each tab is a
    /// whole <see cref="DetailControlBase"/> with its own tabs, time window and status bar.  The window closes with its
    /// last tab.
    ///
    /// <para>The tab strip only appears once a second item is opened.  Tabs are labelled with the item's short label - an
    /// object's schema qualified name, or a hash - plus its database where two items open share a label and the instance
    /// where the window spans more than one.  The full detail is always in the tab's tooltip and in the window title for
    /// the tab in front.</para>
    ///
    /// <para>Opening an item that's already open brings its tab to the front on the requested tab, which is also how a link
    /// inside the window (e.g. Query Stats on the Object Execution tab) moves between tabs.  Ctrl+click opens a new
    /// window.</para>
    /// </summary>
    public sealed class DetailForm : Form
    {
        private readonly DocumentTabControl tabs = new() { Dock = DockStyle.Fill };

        /// <summary>The window items open into: the one used last, while it is open.</summary>
        private static DetailForm current;

        private DetailForm()
        {
            Text = "Detail";
            ClientSize = new System.Drawing.Size(1200, 800);
            Controls.Add(tabs);

            tabs.CloseRequested += (_, page) => CloseTab(page);
            tabs.SelectedIndexChanged += (_, _) => ShowTitle();
            Activated += (_, _) => current = this;
            this.ApplyTheme();
        }

        private static DetailForm Current =>
            current is { IsDisposed: false }
                ? current
                : Application.OpenForms.OfType<DetailForm>().LastOrDefault();

        /// <summary>
        /// Show an object.  Pass whatever is known about it: an ObjectID alone is enough, otherwise the instance and the
        /// object's name, with its database and schema where they're known.
        /// </summary>
        /// <param name="instanceId">The instance the object is on.</param>
        /// <param name="objectId">The repository's ObjectID (dbo.DBObjects), or zero if it isn't known.</param>
        /// <param name="databaseId">The repository's DatabaseID, or zero if it isn't known.</param>
        /// <param name="databaseName">The database name, used where <paramref name="databaseId"/> isn't known.</param>
        /// <param name="schemaName">The schema, if it isn't part of <paramref name="objectName"/>.</param>
        /// <param name="objectName">
        /// The object name - one, two or three part.  The parts of a qualified name take precedence over
        /// <paramref name="databaseName"/> and <paramref name="schemaName"/>: they're the module's own, where a database
        /// column is often the session's, which an EXEC across databases leaves in the caller's database.
        /// </param>
        /// <param name="tab">The tab to show - e.g. <see cref="DetailControlBase.QueryStatsTab"/>.  The first tab if omitted.</param>
        public static void OpenObject(int instanceId, long objectId = 0, int databaseId = 0, string databaseName = null,
            string schemaName = null, string objectName = null, string tab = null)
        {
            var parts = SplitName(objectName);
            if (objectId <= 0 && (instanceId <= 0 || parts.ObjectName == null)) return;
            schemaName = parts.SchemaName ?? schemaName;
            if (parts.DatabaseName != null && !string.Equals(parts.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase))
            {
                databaseName = parts.DatabaseName;
                databaseId = 0; // The id was for the other database
            }

            Open(v => v is ObjectDetailControl o && o.IsSameObject(instanceId, objectId, databaseId, databaseName, schemaName, parts.ObjectName),
                () => new ObjectDetailControl(instanceId, objectId, databaseId, databaseName, schemaName, parts.ObjectName, tab),
                tab);
        }

        /// <summary>Show a query hash or plan hash on an instance.</summary>
        /// <param name="kind">Whether <paramref name="hash"/> is a query hash or a plan hash.</param>
        /// <param name="instanceId">The instance.</param>
        /// <param name="hash">The hash as a hex string.</param>
        /// <param name="databaseName">The database to search Query Store in, or null for every database.</param>
        /// <param name="queryStoreOn">Whether Query Store is on for <paramref name="databaseName"/>, where the caller knows.</param>
        /// <param name="tab">
        /// The tab to show - e.g. <see cref="DetailControlBase.QueryStoreTab"/>.  If omitted, Query Stats, or Query Store
        /// where the instance doesn't collect query stats.
        /// </param>
        public static void OpenHash(QueryHashDetailControl.HashKind kind, int instanceId, string hash, string databaseName = null,
            bool? queryStoreOn = null, string tab = null)
        {
            if (instanceId <= 0 || string.IsNullOrWhiteSpace(hash) || !QueryHashDetailControl.NormalizeHash(hash).IsHex()) return;
            Open(v => v is QueryHashDetailControl q && q.IsSame(kind, instanceId, hash),
                () => new QueryHashDetailControl(kind, instanceId, hash, databaseName, queryStoreOn, tab),
                tab);
        }

        /// <summary>Bring an item already open to the front, or open it on a tab of the current window.</summary>
        private static void Open(Func<DetailControlBase, bool> isSame, Func<DetailControlBase> create, string tab)
        {
            // An item already open anywhere is brought to the front rather than opened a second time
            foreach (var form in Application.OpenForms.OfType<DetailForm>().Where(f => !f.IsDisposed))
            {
                var open = form.Items.FirstOrDefault(i => isSame(i.Viewer));
                if (open.Page is null) continue;
                form.tabs.SelectedTab = open.Page;
                open.Viewer.SelectTab(tab);
                form.ShowInFront();
                return;
            }

            var newWindow = (ModifierKeys & Keys.Control) == Keys.Control;
            var window = newWindow ? null : Current;
            var viewer = create();
            viewer.Dock = DockStyle.Fill;
            if (window is null)
            {
                window = current = new DetailForm();
                window.AddItem(viewer);
                window.ShowSingleInstance(forceNewInstance: newWindow);
                return;
            }

            window.AddItem(viewer);
            window.ShowInFront();
        }

        private void ShowInFront()
        {
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        /// <summary>
        /// Splits a qualified name into its parts, counting back from the object name so that a one, two or three part
        /// name is handled the same way.  Brackets are trimmed.
        /// </summary>
        private static (string DatabaseName, string SchemaName, string ObjectName) SplitName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return (null, null, null);
            var parts = name.Split('.');

            string Part(int partsFromEnd)
            {
                var index = parts.Length - partsFromEnd;
                if (index < 0) return null;
                var part = parts[index].Trim().Trim('[', ']').Trim();
                return string.IsNullOrWhiteSpace(part) ? null : part;
            }

            return (Part(3), Part(2), Part(1));
        }

        /// <summary>The items open in this window, with their tabs, in tab order.</summary>
        private IReadOnlyList<(TabPage Page, DetailControlBase Viewer)> Items =>
            tabs.TabPages.Cast<TabPage>()
                .Select(p => (Page: p, Viewer: p.Controls.Count > 0 ? p.Controls[0] as DetailControlBase : null))
                .Where(i => i.Viewer is not null)
                .ToList();

        private void AddItem(DetailControlBase viewer)
        {
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
        /// Label each tab with its item's short label, plus whatever else is needed to tell the tabs apart: the qualifier
        /// (the database) when two items share a label, and the instance when the window spans more than one.  The tab
        /// strip is hidden while there's only one item - the window title already says which it is.
        /// </summary>
        private void RefreshTabLabels()
        {
            var items = Items;
            tabs.ShowTabStrip = items.Count > 1;
            var multipleInstances = items.Select(i => i.Viewer.InstanceID).Distinct().Count() > 1;
            var sharedLabels = items.GroupBy(i => i.Viewer.TabLabel, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (page, viewer) in items)
            {
                var label = viewer.TabLabel;
                if (sharedLabels.Contains(label) && !string.IsNullOrEmpty(viewer.Qualifier))
                {
                    label += " · " + viewer.Qualifier;
                }
                if (multipleInstances && !string.IsNullOrEmpty(viewer.InstanceName))
                {
                    label += " · " + viewer.InstanceName;
                }
                page.Text = label;
                page.ToolTipText = viewer.Title.Replace(" | ", "\n");
            }
        }

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

        /// <summary>The window is named for the item in front, so it can be told apart on the taskbar.</summary>
        private void ShowTitle()
        {
            Text = tabs.SelectedTab?.Controls.Count > 0 && tabs.SelectedTab.Controls[0] is DetailControlBase viewer
                ? viewer.Title
                : "Detail";
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
