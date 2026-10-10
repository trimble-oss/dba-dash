using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Everything collected about one query hash or plan hash on an instance, on a tab each: the statements in Query Stats,
    /// the executions Running Queries caught, and Query Store.  Hosted on a tab of its own in <see cref="DetailForm"/>.
    ///
    /// <para>A query hash is the shape of a query - shared by every literal variant of it, and by the same query in
    /// several procedures - and a plan hash the shape of a plan.  Query Stats and Running Queries are read across the
    /// instance: a statement in a procedure is counted in the procedure's database, which isn't necessarily the database
    /// the link came from.  The database, where it's known, scopes Query Store, which is kept per database - without one
    /// Query Store is searched in every database.</para>
    /// </summary>
    public sealed class QueryHashDetailControl : DetailControlBase
    {
        public enum HashKind
        {
            QueryHash,
            PlanHash
        }

        public HashKind Kind { get; }

        /// <summary>The hash as a 0x hex string.</summary>
        public string Hash { get; }

        /// <summary>The database Query Store is searched in - every database when null or empty.</summary>
        public string DatabaseName { get; }

        private string KindName => Kind == HashKind.QueryHash ? "Query Hash" : "Plan Hash";

        public override string TabLabel => (Kind == HashKind.QueryHash ? "Query " : "Plan ") + Hash;

        public override string Qualifier => DatabaseName;

        public override string Title => string.Join(" | ", new[] { KindName + " " + Hash, DatabaseName, InstanceName }.Where(s => !string.IsNullOrEmpty(s)));

        /// <param name="kind">Whether <paramref name="hash"/> is a query hash or a plan hash.</param>
        /// <param name="instanceId">The instance.</param>
        /// <param name="hash">The hash as a hex string, with or without 0x.</param>
        /// <param name="databaseName">The database to search Query Store in, or null for every database.</param>
        /// <param name="queryStoreOn">Whether Query Store is on for <paramref name="databaseName"/>, where the caller knows.</param>
        /// <param name="tab">The tab to show first - see <see cref="DefaultTab"/> if null.</param>
        public QueryHashDetailControl(HashKind kind, int instanceId, string hash, string databaseName, bool? queryStoreOn, string tab)
            : base(instanceId, tab)
        {
            Kind = kind;
            Hash = NormalizeHash(hash);
            DatabaseName = string.IsNullOrWhiteSpace(databaseName) ? null : databaseName;
            this.queryStoreOn = queryStoreOn;
        }

        private readonly bool? queryStoreOn;
        private bool queryStatsCollected = true;

        /// <summary>
        /// Query Stats - it's in the repository, so it's there straight away and costs the monitored instance nothing.
        /// Query Store where the instance doesn't collect query stats, unless it's known to be off for the database.
        /// </summary>
        protected override string DefaultTab =>
            !queryStatsCollected && queryStoreOn != false && QueryStoreAvailable() ? QueryStoreTab : QueryStatsTab;

        /// <summary>The hash as the reports show it: 0x and lower case hex.</summary>
        public static string NormalizeHash(string hash)
        {
            hash = hash?.Trim() ?? string.Empty;
            if (hash.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hash = hash[2..];
            return "0x" + hash.ToLowerInvariant();
        }

        public bool IsSame(HashKind kind, int instanceId, string hash) =>
            kind == Kind && instanceId == InstanceID && NormalizeHash(hash) == Hash;

        protected override async Task ResolveAsync()
        {
            SetStatus(Title + (DatabaseName == null && QueryStoreAvailable() ? "  -  Query Store is searched in every database" : string.Empty));
            queryStatsCollected = await CommonData.IsCollectionEnabledAsync(InstanceID, "QueryStats");
        }

        private bool QueryStoreAvailable()
        {
            var context = CommonData.GetDBADashContext(InstanceID);
            return context.CanMessage && context.InstanceSupportsQueryStore;
        }

        protected override void BuildTabs()
        {
            AddTab(QueryStatsTab, LoadQueryStats);
            AddTab(RunningQueriesTab, LoadRunningQueries);
            if (QueryStoreAvailable())
            {
                AddTab(QueryStoreTab, LoadQueryStore);
            }
        }

        #region Tab loaders

        private Task LoadQueryStats(TabPage page)
        {
            var panel = new QueryStatsPanel { Dock = DockStyle.Fill, HostDateRange = DateRangeItem };
            page.Controls.Add(panel);
            if (Kind == HashKind.QueryHash)
            {
                panel.ShowQueryHash(InstanceID, Hash);
            }
            else
            {
                panel.ShowPlanHash(InstanceID, Hash);
            }
            OnDateRangeChanged(page, panel.RefreshData);
            return Task.CompletedTask;
        }

        private Task LoadRunningQueries(TabPage page)
        {
            var rq = new RunningQueries { Dock = DockStyle.Fill };
            page.Controls.Add(rq);

            // The filters carry the dates, so a new window means new filters
            void Show()
            {
                var filters = new RunningQueriesFilters
                {
                    InstanceID = InstanceID,
                    From = DateRangeItem.DateFromUtc,
                    To = DateRangeItem.DateToUtc,
                    Top = RunningQueriesTop
                };
                if (Kind == HashKind.QueryHash)
                {
                    filters.QueryHashString = Hash;
                }
                else
                {
                    filters.QueryPlanHashString = Hash;
                }
                rq.SetFilters(filters);
                rq.RefreshData();
            }

            Show();
            OnDateRangeChanged(page, Show);
            return Task.CompletedTask;
        }

        private Task LoadQueryStore(TabPage page)
        {
            var bytes = Hash.HexStringToByteArray();
            var qs = new QueryStoreTopQueries
            {
                Dock = DockStyle.Fill,
                HostDateRange = DateRangeItem,
                QueryHash = Kind == HashKind.QueryHash ? bytes : null,
                PlanHash = Kind == HashKind.PlanHash ? bytes : null
            };
            page.Controls.Add(qs);
            var context = CommonData.GetDBADashContext(InstanceID);
            context.DatabaseName = DatabaseName ?? string.Empty;
            qs.SetContext(context);
            qs.RefreshData();
            OnDateRangeChanged(page, qs.RefreshData);
            return Task.CompletedTask;
        }

        #endregion
    }
}
