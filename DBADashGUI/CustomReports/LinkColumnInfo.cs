using DBADash;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.SchemaCompare.CodeEditor;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Controls where a drill-down report is displayed.
    /// </summary>
    public enum DrillDownMode
    {
        /// <summary>Open the drill-down report in a new window (default).</summary>
        NewWindow,

        /// <summary>Load the drill-down report in the existing window/control.</summary>
        ExistingWindow,

        /// <summary>Load the drill-down report in a child panel below the parent report.</summary>
        ChildPanel,
    }

    public abstract class LinkColumnInfo
    {
        public abstract void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender);
    }

    public class PlaceholderLinkInfo : LinkColumnInfo
    {
        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            // Do nothing. The link click is handled elsewhere
        }
    }

    public class UrlLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var url = row.Cells[TargetColumn].Value.DBNullToNull().ToString() ?? string.Empty;
            try
            {
                if (url.StartsWith("smb:")) // Convert to UNC path
                {
                    url = url.Replace("smb:", "").Replace("/", @"\");
                }
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
                {
                    var filePath = uri.LocalPath; // Converts file:// paths
                    CommonShared.OpenFolder(filePath);
                }
                else if (CommonShared.IsValidUrl(url))
                {
                    CommonShared.OpenURL(url);
                }
                else
                {
                    MessageBox.Show($"Invalid URL: {url}", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }

    public class TextLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        [JsonIgnore]
        public CodeEditorModes TextHandling { get; set; } = CodeEditorModes.None;

        [JsonProperty(nameof(TextHandling))]
        public string TextHandlingString
        {
            get => TextHandling.ToString();
            set
            {
                if (Enum.TryParse<CodeEditorModes>(value, out var mode))
                {
                    TextHandling = mode;
                }
            }
        }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var text = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (string.IsNullOrEmpty(text)) return;
            Common.ShowCodeViewer(text, row.Cells[TargetColumn].OwningColumn.HeaderText, TextHandling);
        }
    }

    public class QueryPlanLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var queryPlan = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (!Common.IsValidExecutionPlan(queryPlan))
            {
                MessageBox.Show($"Invalid execution plan\n{queryPlan}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Common.ShowQueryPlan(queryPlan);
        }
    }

    public class DeadlockGraphLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var dlGraph = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (!Common.IsValidDeadlockGraph(dlGraph))
            {
                MessageBox.Show($"Invalid deadlock graph\n{dlGraph}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // The report's context identifies the instance the graph came from, which is what lets the viewer
            // offer the plan and Query Store lookups for the statements in the graph.
            Common.ShowDeadlockGraph(dlGraph, context: context);
        }
    }

    /// <summary>
    /// Opens the deadlock viewer for a graph that the grid does not carry, fetching it by key when the link
    /// is clicked.
    ///
    /// The alternative, <see cref="DeadlockGraphLinkColumnInfo"/>, needs the graph in a cell.  That is right
    /// where a result returns a handful of rows, and wrong for the deadlocks report: a graph is the largest
    /// thing stored per deadlock, so returning one per row holds every graph in memory - tens of megabytes
    /// over a few thousand deadlocks - to serve the one the reader eventually opens.  This trades that for a
    /// single row seek per click.
    ///
    /// Used as a synthetic column (no matching column in the result), so the link shows its own header text
    /// on every row.
    /// </summary>
    public class DeadlockGraphLookupLinkColumnInfo : LinkColumnInfo
    {
        public string InstanceIDColumn { get; set; } = "InstanceID";

        public string EventTimeColumn { get; set; } = "EventTime";

        public string DeadlockHashColumn { get; set; } = "DeadlockHash";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            if (row.DataGridView == null) return;
            if (!row.DataGridView.Columns.Contains(InstanceIDColumn)
                || !row.DataGridView.Columns.Contains(EventTimeColumn)
                || !row.DataGridView.Columns.Contains(DeadlockHashColumn)) return;

            if (row.Cells[InstanceIDColumn].Value.DBNullToNull() is not int instanceId) return;
            if (row.Cells[EventTimeColumn].Value.DBNullToNull() is not DateTime eventTime) return;
            if (row.Cells[DeadlockHashColumn].Value.DBNullToNull() is not byte[] deadlockHash) return;

            // Datetime columns are converted to the app time zone when the result is loaded, so the value in
            // the cell is not what the key in the repository is.  Converting back is what the drill-down link
            // does with a mapped date parameter, and for the same reason.
            if (!context.Report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Contains(EventTimeColumn))
            {
                eventTime = eventTime.AppTimeZoneToUtc();
            }

            string graph;
            var cursor = sender?.Cursor;
            try
            {
                if (sender != null) sender.Cursor = Cursors.WaitCursor;
                graph = FetchGraph(instanceId, eventTime, deadlockHash);
            }
            finally
            {
                if (sender != null) sender.Cursor = cursor;
            }

            // No graph is an ordinary state, not an error: the deadlock can have been shredded without the XML
            // being kept, and old rows are purged on their own retention.
            if (string.IsNullOrEmpty(graph))
            {
                MessageBox.Show("The deadlock graph is no longer available for this deadlock.", "Deadlock Graph",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Common.IsValidDeadlockGraph(graph))
            {
                MessageBox.Show($"Invalid deadlock graph\n{graph}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Common.ShowDeadlockGraph(graph, context: context);
        }

        private static string FetchGraph(int instanceId, DateTime eventTime, byte[] deadlockHash)
        {
            using var connection = new SqlConnection(Common.ConnectionString);
            using var command = new SqlCommand("dbo.DeadlockGraph_Get", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };
            command.Parameters.AddWithValue("InstanceID", instanceId);
            command.Parameters.Add("EventTime", SqlDbType.DateTime2, 3).Value = eventTime;
            command.Parameters.Add("DeadlockHash", SqlDbType.Binary, 16).Value = deadlockHash;
            connection.Open();
            return command.ExecuteScalar() as string;
        }
    }

    /// <summary>
    /// Opens the Object Execution stats for the module a row names - the link the object name on Slow
    /// Queries gives, made available to a report grid.
    ///
    /// The name may be one, two or three part.  Deadlocks record the module the way the execution stack
    /// reports it, "Sales.dbo.usp_UpdateOrder", and the parts are what the lookup needs: the stats are
    /// filtered on the bare object name, and the database part is resolved to the repository's DatabaseID
    /// so a procedure that shares its name with one in another database is not reported as one object.
    ///
    /// The module's own database is used rather than the process's current database, because an EXEC
    /// across databases leaves the process in the caller's database while the object lives in the other.
    /// </summary>
    public class ObjectExecutionLinkColumnInfo : LinkColumnInfo
    {
        /// <summary>The column holding the object name, optionally schema and database qualified.</summary>
        public string TargetColumn { get; set; }

        public string InstanceIDColumn { get; set; } = "InstanceID";

        /// <summary>
        /// Optional column naming the database to use when the name in <see cref="TargetColumn"/> is not
        /// three part.  Without it an unqualified name is looked up across the instance.
        /// </summary>
        public string DatabaseNameColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            if (GetValue(row, TargetColumn) is not string name || string.IsNullOrWhiteSpace(name)) return;

            var parts = SplitName(name);
            if (parts.ObjectName == null) return;

            var instanceId = GetValue(row, InstanceIDColumn) as int? ?? context.InstanceID;
            var databaseName = parts.DatabaseName ?? GetValue(row, DatabaseNameColumn) as string;
            // -1 for a database that isn't in the repository - dropped, renamed, or never collected.  The
            // object name filter still applies, so the stats open on the instance instead of on nothing.
            var databaseId = CommonData.GetDatabaseID(instanceId, databaseName);

            var newContext = (DBADashContext)context.Clone();
            newContext.InstanceID = instanceId;
            newContext.DatabaseID = databaseId > 0 ? databaseId : 0;
            newContext.DatabaseName = databaseName;
            newContext.SchemaName = parts.SchemaName;
            newContext.ObjectName = parts.ObjectName;
            // The stats are looked up by name.  An ObjectID carried in from wherever the report was opened
            // belongs to a different object and would filter this one out entirely.
            newContext.ObjectID = 0;
            newContext.Type = SQLTreeItem.TreeType.StoredProcedure;

            var parent = sender?.ParentForm ?? Main.MainFormInstance;
            if (parent == null) return;
            Common.ShowObjectExecutionSummary(newContext, parent);
        }

        private static object GetValue(DataGridViewRow row, string columnName) =>
            string.IsNullOrEmpty(columnName) || row?.DataGridView?.Columns.Contains(columnName) != true
                ? null
                : row.Cells[columnName].Value.DBNullToNull();

        /// <summary>
        /// Splits a qualified name into its parts, counting back from the object name so that a one or two
        /// part name is handled the same way.  Brackets are trimmed; a name with a dot inside brackets is
        /// not something SQL Server reports here.
        /// </summary>
        private static (string DatabaseName, string SchemaName, string ObjectName) SplitName(string name)
        {
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
    }

    /// <summary>
    /// Where a drill-down reads the values it fills the target report's parameters from.
    ///
    /// A drill-down from a grid reads the clicked row.  A drill-down from a chart has no grid at all - a
    /// chart can be shown with its results hidden - so it reads the row of the result set the clicked
    /// point was drawn from instead.  Both answer the same question, which is all the drill-down needs.
    /// </summary>
    public interface IDrillDownSource
    {
        /// <summary>
        /// The value of a column, and the type it is held as - which is what decides whether a value needs
        /// converting back out of the app time zone.  False when the column isn't there or has no value.
        /// </summary>
        bool TryGetValue(string columnName, out object value, out Type valueType);
    }

    /// <summary>The clicked row of a grid.</summary>
    public class GridRowDrillDownSource : IDrillDownSource
    {
        private readonly DataGridViewRow row;

        public GridRowDrillDownSource(DataGridViewRow row) => this.row = row;

        public bool TryGetValue(string columnName, out object value, out Type valueType)
        {
            value = null;
            valueType = null;
            // A mapped column the grid doesn't have is the same situation as a null value - the parameter
            // stays at its default - and is what a proc that returns a different shape in some cases
            // produces.  Indexing it blind turns that into an exception on click.
            if (row?.DataGridView?.Columns.Contains(columnName) != true) return false;
            var cell = row.Cells[columnName];
            value = cell.Value.DBNullToNull();
            valueType = cell.ValueType;
            return value != null;
        }
    }

    /// <summary>The result set row behind a clicked chart point.</summary>
    public class DataRowDrillDownSource : IDrillDownSource
    {
        private readonly DataRow row;

        public DataRowDrillDownSource(DataRow row) => this.row = row;

        public bool TryGetValue(string columnName, out object value, out Type valueType)
        {
            value = null;
            valueType = null;
            if (row?.Table?.Columns.Contains(columnName) != true) return false;
            value = row[columnName].DBNullToNull();
            valueType = row.Table.Columns[columnName].DataType;
            return value != null;
        }
    }

    public abstract class BaseDrillDownLinkColumnInfo : LinkColumnInfo
    {
        public Dictionary<string, string> ColumnToParameterMap { get; set; } = new();

        /// <summary>
        /// Controls whether the drill-down opens in a new window or loads in the existing window.
        /// Default is <see cref="DrillDownMode.NewWindow"/>.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DrillDownMode DrillDownMode { get; set; } = DrillDownMode.NewWindow;

        /// <summary>
        /// Optional fixed grid filters to apply after the drill-down report loads.
        /// Key = result set index, Value = DataView RowFilter expression.
        /// </summary>
        public Dictionary<int, string> GridFilters { get; set; }

        /// <summary>
        /// Optional factory to build grid filters dynamically from the clicked row.
        /// Takes precedence over <see cref="GridFilters"/> when set.
        /// </summary>
        [JsonIgnore]
        public Func<DataGridViewRow, Dictionary<int, string>> GridFilterFactory { get; set; }

        /// <summary>
        /// Title template for the child panel. Column placeholders use {ColumnName} syntax,
        /// e.g. "{InstanceDisplayName} | {name}". Only used with <see cref="DrillDownMode.ChildPanel"/>.
        /// </summary>
        public string ChildPanelTitle { get; set; }

        /// <summary>
        /// Carries the filters the source report is showing into the report the drill-down opens - every
        /// parameter the user has set that the target report also declares.
        ///
        /// Without it a drill-down shows more than the row or point that was clicked counted.  A deadlock
        /// chart grouped by application while Processes is set to Victims Only counts deadlocks that
        /// application was rolled back in; a drill-down that passed on the application but not the victims
        /// setting would answer a different question - the deadlocks it merely took part in.
        ///
        /// Only for a target whose parameters of the same name mean the same thing, so it is off unless a
        /// report asks for it.  System parameters (instances, dates) are left alone: those come from the
        /// context and the date range, which the target picks up for itself.  A parameter this drill-down
        /// maps from a column is set afterwards, so the click always wins over the carried filter.
        /// </summary>
        public bool CarryUserParameters { get; set; }

        protected abstract CustomReport GetReport(DBADashContext context);

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender) =>
            Navigate(context, new GridRowDrillDownSource(row), selectedTableIndex, sender, row);

        /// <summary>
        /// Opens the drill-down report for a set of values.  The grid overload above is the usual caller;
        /// a chart drill-down calls this directly with the row behind the clicked point and no grid row,
        /// which only costs it <see cref="GridFilterFactory"/>.
        /// </summary>
        public void Navigate(DBADashContext context, IDrillDownSource values, int selectedTableIndex, ContainerControl sender, DataGridViewRow row = null)
        {
            var report = GetReport(context);
            if (report == null) return;

            var newContext = (DBADashContext)context.Clone();
            newContext.Report = report;
            var customParams = report.GetCustomSqlParameters();
            if (CarryUserParameters && sender is CustomReportView sourceView)
            {
                CarryFilters(sourceView.CurrentParameters, customParams);
            }
            foreach (var mapping in ColumnToParameterMap)
            {
                var param = customParams.FirstOrDefault(p => p.Param.ParameterName == mapping.Key);
                if (param == null) continue;
                // Skip missing columns and null values - the parameter stays at its default.
                if (!values.TryGetValue(mapping.Value, out var value, out var valueType)) continue;
                param.UseDefaultValue = false;
                if (valueType == typeof(DateTime) && !context.Report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Contains(mapping.Value))
                {
                    value = ((DateTime)value).AppTimeZoneToUtc();
                }
                if (string.Equals(mapping.Key, "@INSTANCEIDS", StringComparison.OrdinalIgnoreCase) && value is int intValue)
                {
                    value = (new HashSet<int>() { intValue }).AsDataTable();
                }
                param.Param.Value = value;
            }
            var filters = (row == null ? null : GridFilterFactory?.Invoke(row)) ?? GridFilters;

            var targetViewType = report.ViewType ?? typeof(CustomReportView);
            var ctrlClick = Control.ModifierKeys.HasFlag(Keys.Control);
            var useExistingWindow = DrillDownMode == DrillDownMode.ExistingWindow
                                    && sender is CustomReportView
                                    && !ctrlClick
                                    && targetViewType.IsAssignableFrom(sender.GetType()); // Fall back to new window if ViewType is incompatible
            var useChildPanel = DrillDownMode == DrillDownMode.ChildPanel
                                && sender is CustomReportView
                                && !ctrlClick;

            if (useChildPanel && sender is CustomReportView parentView)
            {
                string title = null;
                if (!string.IsNullOrEmpty(ChildPanelTitle))
                {
                    title = System.Text.RegularExpressions.Regex.Replace(ChildPanelTitle, @"\{(\w+)\}", m =>
                        values.TryGetValue(m.Groups[1].Value, out var v, out _) ? Convert.ToString(v) : string.Empty);
                }
                parentView.ShowChildReport(report, newContext, customParams, filters, title);
            }
            else if (useExistingWindow && sender is CustomReportView existingView)
            {
                existingView.PushNavigationState();
                existingView.Report = report;
                existingView.DrillDownGridFilters = filters;
                _ = existingView.SetContext(newContext, customParams);
            }
            else
            {
                CustomReportViewer customReportViewer = new() { Context = newContext, CustomParams = customParams, GridFilters = filters };
                customReportViewer.ShowSingleInstance();
            }
        }

        /// <summary>
        /// Copies the filters the source report is showing onto the parameters of the report being opened.
        ///
        /// A filter is carried when the target declares a parameter of the same name and the source has one
        /// the user actually set - a parameter still on its default says nothing to carry.  System
        /// parameters are skipped: the instances and the date range are set from context by the view that
        /// is about to run the report, and pinning them here would take the date range picker out of use.
        /// </summary>
        private static void CarryFilters(IEnumerable<CustomSqlParameter> sourceParams, List<CustomSqlParameter> targetParams)
        {
            foreach (var source in sourceParams ?? Enumerable.Empty<CustomSqlParameter>())
            {
                if (source.UseDefaultValue) continue;
                var name = source.Param.ParameterName;
                if (CustomReport.SystemParamNames.Contains(name.ToUpper())) continue;
                var target = targetParams.FirstOrDefault(p => p.Param.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (target == null) continue;
                target.Param.Value = source.Param.Value;
                target.UseDefaultValue = false;
            }
        }
    }

    public class DrillDownLinkColumnInfo : BaseDrillDownLinkColumnInfo
    {
        public string ReportProcedureName { get; set; }

        protected override CustomReport GetReport(DBADashContext context)
        {
            return context.Report is SystemReport
                ? CustomReports.SystemReports.FirstOrDefault(r => r.ProcedureName == ReportProcedureName)
                : CustomReports.GetCustomReports().FirstOrDefault(r => r.ProcedureName == ReportProcedureName);
        }
    }

    public class SystemDrillDownLinkColumnInfo : BaseDrillDownLinkColumnInfo
    {
        [JsonIgnore]
        public Func<SystemReport> ReportFactory { get; set; }

        protected override CustomReport GetReport(DBADashContext context)
        {
            return ReportFactory?.Invoke();
        }
    }

    /// <summary>
    /// Opens the Running Queries session detail for the snapshot a row refers to.  Used by the Killed Sessions
    /// report to link back to the exact snapshot (instance + session + snapshot date) the kill was actioned from.
    /// </summary>
    public class RunningQueriesSessionDetailLinkColumnInfo : LinkColumnInfo
    {
        public string InstanceColumn { get; set; } = "InstanceID";
        public string SessionIdColumn { get; set; } = "session_id";
        public string SnapshotDateColumn { get; set; } = "SnapshotDate";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var instanceId = row.Cells[InstanceColumn].Value.DBNullToNull();
            var sessionIdVal = row.Cells[SessionIdColumn].Value.DBNullToNull();
            var snapshotVal = row.Cells[SnapshotDateColumn].Value.DBNullToNull();
            if (instanceId == null || snapshotVal == null) return;

            var id = Convert.ToInt32(instanceId);
            var sessionId = sessionIdVal == null ? 0 : Convert.ToInt32(sessionIdVal);
            // SnapshotDate is displayed in the app timezone (auto-converted); convert back to UTC for the lookup.
            var snapshotUtc = ((DateTime)snapshotVal).AppTimeZoneToUtc();

            try
            {
                var ctx = CommonData.GetDBADashContext(id);
                if (!Performance.RunningQueries.ShowSessionDetail(id, sessionId, snapshotUtc, ctx))
                {
                    MessageBox.Show(
                        $"Session {sessionId} was not found in the snapshot at {snapshotVal}. The Running Queries snapshot may have been purged by retention.",
                        "Snapshot not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }

    /// <summary>
    /// Opens the whole Running Queries snapshot (all sessions) for the instance + snapshot date a row refers to.
    /// Used by the Killed Sessions report to link the snapshot date to the full snapshot.
    /// </summary>
    public class RunningQueriesLinkColumnInfo : LinkColumnInfo
    {
        public string InstanceColumn { get; set; } = "InstanceID";
        public string SnapshotDateColumn { get; set; } = "SnapshotDate";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var instanceId = row.Cells[InstanceColumn].Value.DBNullToNull();
            var snapshotVal = row.Cells[SnapshotDateColumn].Value.DBNullToNull();
            if (instanceId == null || snapshotVal == null) return;

            var id = Convert.ToInt32(instanceId);
            // SnapshotDate is displayed in the app timezone (auto-converted); convert back to UTC for the lookup.
            var snapshotUtc = ((DateTime)snapshotVal).AppTimeZoneToUtc();

            try
            {
                var viewer = new Performance.RunningQueriesViewer
                {
                    InstanceID = id,
                    SnapshotDateFrom = snapshotUtc,
                    SnapshotDateTo = snapshotUtc
                };
                viewer.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }

    public class NavigateTreeLinkColumnInfo : LinkColumnInfo
    {
        public string InstanceColumn { get; set; }
        public string DatabaseColumn { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Main.Tabs Tab { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var main = Main.MainFormInstance;
            if (main == null) return;
            var ownerSet = false;
            if (sender.ParentForm != main && sender.ParentForm is { Owner: null }) // Setting the owner keeps the window on top of the main form
            {
                sender.ParentForm.Owner = main;
                ownerSet = true;
            }

            var instanceId = row.Cells[InstanceColumn].Value.DBNullToNull() as int?;
            var instanceName = row.Cells[InstanceColumn].Value.DBNullToNull() as string;
            var args = new Main.InstanceSelectedEventArgs()
            {
                InstanceID = instanceId ?? 0,
                Instance = instanceName,
                Database = string.IsNullOrEmpty(DatabaseColumn) ? null : row.Cells[DatabaseColumn].Value.DBNullToNull() as string,
                Tab = Tab,
                SearchFromRoot = true
            };
            main.Instance_Selected(sender, args);
            Application.DoEvents(); // Complete actions that might be triggered by Instance_Selected before we unset the owner
            if (ownerSet) // Undo setting the owner
            {
                sender.ParentForm.Owner = null;
            }
        }
    }
}