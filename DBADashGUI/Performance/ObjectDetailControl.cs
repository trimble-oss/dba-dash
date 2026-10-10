using DBADashGUI.Theme;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// Everything collected about one object - a stored procedure, function or trigger - on a tab each: its execution
    /// stats, its statements in Query Stats, the times Running Queries caught it, its slow queries, Query Store, and its
    /// schema history.  Hosted on a tab of its own in <see cref="DetailForm"/>.
    ///
    /// <para>Callers know different things about an object - a running query has the repository's ObjectID, a slow query
    /// only its database and bare name, a query stats row a qualified name - so the rest is resolved from the repository
    /// when the control loads.  A tab that needs something the lookup couldn't find (an object with no schema snapshot
    /// has no ObjectID) is left out rather than shown empty.</para>
    /// </summary>
    public sealed class ObjectDetailControl : DetailControlBase
    {
        public const string ObjectExecutionTab = "Object Execution";
        public const string SlowQueriesTab = "Slow Queries";
        public const string SchemaHistoryTab = "Schema History";

        public long ObjectID { get; private set; }
        public int DatabaseID { get; private set; }
        public string DatabaseName { get; private set; }
        public string SchemaName { get; private set; }
        public string ObjectName { get; private set; }
        private string typeDescription;

        public string QualifiedName => string.IsNullOrEmpty(SchemaName) ? ObjectName : SchemaName + "." + ObjectName;

        public override string TabLabel => QualifiedName ?? "Object";

        public override string Qualifier => DatabaseName;

        public override string Title => string.Join(" | ", new[] { QualifiedName, DatabaseName, InstanceName }.Where(s => !string.IsNullOrEmpty(s)));

        /// <param name="instanceId">The instance the object is on.</param>
        /// <param name="objectId">The repository's ObjectID (dbo.DBObjects), or zero if it isn't known.</param>
        /// <param name="databaseId">The repository's DatabaseID, or zero if it isn't known.</param>
        /// <param name="databaseName">The database name, used where <paramref name="databaseId"/> isn't known.</param>
        /// <param name="schemaName">The schema, or null if it isn't known.</param>
        /// <param name="objectName">The bare object name, or null if only <paramref name="objectId"/> is known.</param>
        /// <param name="tab">The tab to show first - the first tab if null.</param>
        public ObjectDetailControl(int instanceId, long objectId, int databaseId, string databaseName, string schemaName, string objectName, string tab)
            : base(instanceId, tab)
        {
            ObjectID = objectId;
            DatabaseID = databaseId;
            DatabaseName = databaseName;
            SchemaName = schemaName;
            ObjectName = objectName;
        }

        /// <summary>True if this is the object described - a part either side doesn't know can't tell the two apart.</summary>
        public bool IsSameObject(int instanceId, long objectId, int databaseId, string databaseName, string schemaName, string objectName)
        {
            if (objectId > 0 && ObjectID > 0) return objectId == ObjectID;
            if (instanceId != InstanceID || !SameName(objectName, ObjectName)) return false;
            return (databaseId <= 0 || DatabaseID <= 0 || databaseId == DatabaseID) &&
                   (string.IsNullOrEmpty(databaseName) || string.IsNullOrEmpty(DatabaseName) || SameName(databaseName, DatabaseName)) &&
                   (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(SchemaName) || SameName(schemaName, SchemaName));
        }

        private static bool SameName(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        /// <summary>Fill in what the caller didn't know about the object from the repository.</summary>
        protected override async Task ResolveAsync()
        {
            if (ObjectID <= 0 && DatabaseID <= 0 && !string.IsNullOrEmpty(DatabaseName))
            {
                var id = await Task.Run(() => CommonData.GetDatabaseID(InstanceID, DatabaseName));
                if (id > 0) DatabaseID = id;
            }

            var dt = await CommonData.GetDBObjectInfoAsync(ObjectID, InstanceID, DatabaseID, SchemaName, ObjectName);
            string note = null;
            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                ObjectID = row.Field<long>("ObjectID");
                DatabaseID = row.Field<int>("DatabaseID");
                DatabaseName = row.Field<string>("DatabaseName");
                SchemaName = row.Field<string>("SchemaName");
                ObjectName = row.Field<string>("ObjectName");
                InstanceName = row.Field<string>("Instance");
                typeDescription = row["TypeDescription"] as string;
                if (dt.Rows.Count > 1)
                {
                    note = $"{dt.Rows.Count} objects match the name {ObjectName} - showing the one in {DatabaseName}.";
                }
            }
            else
            {
                note = "The object isn't in the repository, so its schema history isn't available.";
            }

            var description = string.Join("  ", new[] { typeDescription, Title }.Where(s => !string.IsNullOrEmpty(s)));
            if (note == null)
            {
                SetStatus(description);
            }
            else
            {
                SetStatus(description + "  -  " + note, DashColors.Warning);
            }
        }

        protected override void BuildTabs()
        {
            var context = CommonData.GetDBADashContext(InstanceID);

            AddTab(ObjectExecutionTab, LoadObjectExecution);
            // The query stats are matched on the qualified name
            if (!string.IsNullOrEmpty(SchemaName))
            {
                AddTab(QueryStatsTab, LoadQueryStats);
            }
            // Matched on the name - unknown only when an ObjectID was given and the lookup failed
            if (!string.IsNullOrEmpty(ObjectName))
            {
                AddTab(RunningQueriesTab, LoadRunningQueries);
                AddTab(SlowQueriesTab, LoadSlowQueries);
            }
            if (context.CanMessage && context.InstanceSupportsQueryStore && !string.IsNullOrEmpty(DatabaseName))
            {
                AddTab(QueryStoreTab, LoadQueryStore);
            }
            if (ObjectID > 0)
            {
                AddTab(SchemaHistoryTab, LoadSchemaHistory);
            }
        }

        /// <summary>A context for the controls that take one, for this object.</summary>
        private DBADashContext ObjectContext()
        {
            var context = CommonData.GetDBADashContext(InstanceID);
            context.ObjectID = ObjectID;
            context.ObjectName = ObjectName;
            context.SchemaName = SchemaName;
            context.DatabaseID = Math.Max(DatabaseID, 0);
            context.DatabaseName = DatabaseName ?? string.Empty;
            // The type the object execution and query store controls filter to a single object for
            context.Type = SQLTreeItem.TreeType.StoredProcedure;
            return context;
        }

        #region Tab loaders

        private Task LoadObjectExecution(TabPage page)
        {
            var oes = new ObjectExecutionSummary { Dock = DockStyle.Fill, HostDateRange = DateRangeItem };
            page.Controls.Add(oes);
            oes.SetContext(ObjectContext());
            OnDateRangeChanged(page, oes.RefreshData);
            return Task.CompletedTask;
        }

        private Task LoadQueryStats(TabPage page)
        {
            var panel = new QueryStatsPanel { Dock = DockStyle.Fill, HostDateRange = DateRangeItem };
            page.Controls.Add(panel);
            panel.ShowObject(InstanceID, DatabaseID, QualifiedName);
            OnDateRangeChanged(page, panel.RefreshData);
            return Task.CompletedTask;
        }

        private Task LoadRunningQueries(TabPage page)
        {
            var rq = new RunningQueries { Dock = DockStyle.Fill };
            page.Controls.Add(rq);

            // The filters carry the dates, so a new window means new filters - which also takes the grid back from a
            // snapshot drilled into to the list for the window.
            void Show()
            {
                rq.SetFilters(new RunningQueriesFilters
                {
                    InstanceID = InstanceID,
                    // object_name is schema qualified and matched with LIKE.  Without a schema, any schema.
                    ObjectName = string.IsNullOrEmpty(SchemaName)
                        ? "%." + EscapeLike(ObjectName)
                        : EscapeLike(QualifiedName),
                    From = DateRangeItem.DateFromUtc,
                    To = DateRangeItem.DateToUtc,
                    Top = RunningQueriesTop
                });
                rq.RefreshData();
            }

            Show();
            OnDateRangeChanged(page, Show);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The capture records the bare object name, so the database is what keeps a procedure apart from one of the same
        /// name elsewhere on the instance.
        /// </summary>
        private Task LoadSlowQueries(TabPage page)
        {
            var sq = new SlowQueries { Dock = DockStyle.Fill, FixedObjectName = ObjectName, HostDateRange = DateRangeItem };
            page.Controls.Add(sq);
            sq.SetContext(ObjectContext());
            OnDateRangeChanged(page, sq.RefreshData);
            return Task.CompletedTask;
        }

        private Task LoadQueryStore(TabPage page)
        {
            var qs = new QueryStoreTopQueries { Dock = DockStyle.Fill, HostDateRange = DateRangeItem };
            page.Controls.Add(qs);
            qs.SetContext(ObjectContext());
            qs.RefreshData();
            OnDateRangeChanged(page, qs.RefreshData);
            return Task.CompletedTask;
        }

        private async Task LoadSchemaHistory(TabPage page)
        {
            var history = new ObjectSchemaHistory { Dock = DockStyle.Fill };
            page.Controls.Add(history);
            await history.LoadAsync(ObjectID);
        }

        #endregion
    }
}
