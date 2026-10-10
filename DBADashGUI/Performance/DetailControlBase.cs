using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// One item on a tab of <see cref="DetailForm"/> - an object, a query hash or a plan hash - with what's collected about
    /// it on tabs of its own, a status bar, and a time window shared by its tabs.
    ///
    /// <para>The item is looked up first (<see cref="ResolveAsync"/>), since callers often know only part of it, and its
    /// tabs are built from what the lookup found (<see cref="BuildTabs"/>).  Each tab is loaded the first time it's
    /// selected.</para>
    ///
    /// <para>The time window is independent of the global time filter once the item opens, so items open side by side
    /// can look at different periods.  A change refreshes the tab in front, and the other loaded tabs when they're next
    /// selected - so a change doesn't run every tab's query (Query Store's on the monitored instance) for tabs nobody is
    /// looking at.</para>
    /// </summary>
    public abstract class DetailControlBase : UserControl
    {
        public const string QueryStatsTab = "Query Stats";
        public const string RunningQueriesTab = "Running Queries";
        public const string QueryStoreTab = "Query Store";

        /// <summary>The rows Running Queries tabs show at most, as its own date range filter does.</summary>
        protected const int RunningQueriesTop = 5000;

        private readonly ThemedTabControl tabs = new() { Dock = DockStyle.Fill };
        private readonly StatusStrip statusStrip = new();
        private readonly ToolStripStatusLabel lblStatus = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStrip toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly Dictionary<TabPage, Func<TabPage, Task>> loaders = new();
        private readonly HashSet<TabPage> loadedTabs = new();

        /// <summary>How to show a new time window on each loaded tab.</summary>
        private readonly Dictionary<TabPage, Action> dateRangeRefreshers = new();

        /// <summary>Loaded tabs not yet showing the current time window.</summary>
        private readonly HashSet<TabPage> staleTabs = new();

        private string initialTab;

        /// <summary>The time window shared by every tab - pass it to the controls the tabs host.</summary>
        protected DateRangeToolStripMenuItem DateRangeItem { get; } = new() { ToolTipText = "Time window for every tab" };

        public int InstanceID { get; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string InstanceName { get; protected set; }

        /// <summary>Short label for the item's tab in the window.</summary>
        public abstract string TabLabel { get; }

        /// <summary>Added to <see cref="TabLabel"/> where two items open have the same label - e.g. the database.</summary>
        public virtual string Qualifier => null;

        /// <summary>Full description of the item - used for the window title and the tab's tooltip.</summary>
        public abstract string Title { get; }

        /// <summary>Raised once the item has been looked up, which can fill in more of its <see cref="Title"/>.</summary>
        public event EventHandler TitleChanged;

        /// <param name="instanceId">The instance the item is on.</param>
        /// <param name="tab">The tab to show first - the first tab if null.</param>
        protected DetailControlBase(int instanceId, string tab)
        {
            InstanceID = instanceId;
            InstanceName = CommonData.GetDBADashContext(instanceId).InstanceName;
            initialTab = tab;

            if (DateRange.SelectedTimeSpan.HasValue)
            {
                DateRangeItem.SetTimeSpan(DateRange.SelectedTimeSpan.Value);
            }
            else
            {
                DateRangeItem.SetDateRangeUtc(DateRange.FromUTC, DateRange.ToUTC);
            }
            DateRangeItem.DateRangeChanged += (_, _) =>
            {
                staleTabs.UnionWith(dateRangeRefreshers.Keys);
                RefreshIfStale(tabs.SelectedTab);
            };
            toolStrip.Items.Add(DateRangeItem);

            statusStrip.Items.Add(lblStatus);
            Controls.Add(tabs);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);
        }

        /// <summary>
        /// Look up what the caller didn't know about the item, and say what it is in the status bar with
        /// <see cref="SetStatus"/>.  An exception is shown in the status bar and the tabs are built from what's known.
        /// </summary>
        protected virtual Task ResolveAsync()
        {
            SetStatus(Title);
            return Task.CompletedTask;
        }

        /// <summary>Add the item's tabs with <see cref="AddTab"/>, from what <see cref="ResolveAsync"/> found.</summary>
        protected abstract void BuildTabs();

        /// <summary>The tab to open on when the caller didn't ask for one - the first tab when null.</summary>
        protected virtual string DefaultTab => null;

        protected void SetStatus(string text, Color? color = null)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color ?? DBADashUser.SelectedTheme.ForegroundColor;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.ApplyTheme();
            SetStatus("Loading...");
            try
            {
                await ResolveAsync();
            }
            catch (Exception ex)
            {
                SetStatus("Unable to look up the details: " + ex.Message, DashColors.Fail);
            }
            if (IsDisposed) return;
            TitleChanged?.Invoke(this, EventArgs.Empty);
            BuildTabs();
            tabs.ApplyTheme();
            tabs.SelectedIndexChanged += async (_, _) =>
            {
                RefreshIfStale(tabs.SelectedTab);
                await LoadTab(tabs.SelectedTab);
            };
            SelectTab(initialTab ?? DefaultTab);
            initialTab = null;
            await LoadTab(tabs.SelectedTab);
        }

        protected void AddTab(string title, Func<TabPage, Task> loader)
        {
            var page = new TabPage(title) { Name = title };
            tabs.TabPages.Add(page);
            loaders.Add(page, loader);
        }

        /// <summary>How to refresh a loaded tab for a new time window - set by the tab's loader.</summary>
        protected void OnDateRangeChanged(TabPage page, Action refresh) => dateRangeRefreshers[page] = refresh;

        /// <summary>
        /// Bring the named tab to the front.  Does nothing if the tab isn't shown for this item.  Safe to call before the
        /// item has been looked up - the tab is selected once they're built.
        /// </summary>
        public void SelectTab(string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            if (loaders.Count == 0)
            {
                initialTab = title;
                return;
            }
            var page = tabs.TabPages[title];
            if (page != null) tabs.SelectedTab = page;
        }

        private void RefreshIfStale(TabPage page)
        {
            if (page == null || !staleTabs.Remove(page)) return;
            if (dateRangeRefreshers.TryGetValue(page, out var refresh)) refresh();
        }

        private async Task LoadTab(TabPage page)
        {
            if (page == null || loadedTabs.Contains(page)) return;
            loadedTabs.Add(page); // Up front, to prevent re-entry and a reload on later tab switches
            if (!loaders.TryGetValue(page, out var loader)) return;

            try
            {
                await loader(page);
                page.ApplyTheme();
            }
            catch (Exception ex)
            {
                loadedTabs.Remove(page); // Allow a retry by selecting the tab again
                if (page.IsDisposed) return;
                page.Controls.Clear();
                page.Controls.Add(new Label { Text = ex.Message, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                page.ApplyTheme();
            }
        }

        protected static string EscapeLike(string value) =>
            value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
    }
}
