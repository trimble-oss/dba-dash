using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// The Query Stats report narrowed to one object or one query shape, for the object and session detail windows.
    ///
    /// <para>The report follows the global time filter in the main window, which belongs to the tree rather than to a
    /// detail window opened from it - so the panel has a date range of its own, set from the global one when it opens,
    /// as the Object Execution tab's is.</para>
    ///
    /// <para>The switch to the charts report is not offered: the charts can't be filtered to an object or a query shape,
    /// so they would show the whole instance under a window that is about one object.</para>
    /// </summary>
    public sealed class QueryStatsPanel : UserControl
    {
        private readonly CustomReportView view = new() { Dock = DockStyle.Fill, AllowSwitchReport = false };

        private readonly DateRangeToolStripMenuItem dateRange = new()
        {
            ToolTipText = "Date range for the query stats"
        };

        public QueryStatsPanel()
        {
            Controls.Add(view);
            view.ToolStrip.Items.Add(dateRange);
            view.BeforeRefresh += (_, _) =>
            {
                var range = HostDateRange ?? dateRange;
                view.SetParameterValue("@FromDate", range.DateFromUtc);
                view.SetParameterValue("@ToDate", range.DateToUtc);
            };
            dateRange.DateRangeChanged += (_, _) =>
            {
                if (view.CurrentContext != null) view.RefreshData();
            };
            if (DateRange.SelectedTimeSpan.HasValue)
            {
                dateRange.SetTimeSpan(DateRange.SelectedTimeSpan.Value);
            }
            else
            {
                dateRange.SetDateRangeUtc(DateRange.FromUTC, DateRange.ToUTC);
            }
        }

        /// <summary>Use a window of its own instead of the global one.  Call before the panel shows its data.</summary>
        public void SetDateRangeUtc(DateTime fromUtc, DateTime toUtc) => dateRange.SetDateRangeUtc(fromUtc, toUtc);

        private DateRangeToolStripMenuItem hostDateRange;

        /// <summary>
        /// A date range owned by the window hosting the panel, used instead of the panel's own - e.g. the object detail
        /// window's, which all its tabs share.  The panel's own picker is hidden.  The host calls <see cref="RefreshData"/>
        /// when it changes.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public DateRangeToolStripMenuItem HostDateRange
        {
            get => hostDateRange;
            set
            {
                hostDateRange = value;
                dateRange.Visible = value == null;
            }
        }

        public void RefreshData()
        {
            if (view.CurrentContext != null) view.RefreshData();
        }

        /// <summary>The statements of one object.</summary>
        /// <param name="instanceId">The instance the object is on.</param>
        /// <param name="databaseId">The object's database, or zero where it isn't known.</param>
        /// <param name="qualifiedObjectName">schema.name - the report matches the qualified name.</param>
        public void ShowObject(int instanceId, int databaseId, string qualifiedObjectName) =>
            Show(instanceId, databaseId, ("@ObjectName", qualifiedObjectName), ("@GroupBy", "Statement"));

        /// <summary>
        /// The statements of one query shape on an instance - every procedure and ad hoc batch the query ran in.  Not
        /// narrowed to a database: a statement in a procedure is counted in the procedure's database, which isn't
        /// necessarily the database the session was in.
        /// </summary>
        /// <param name="queryHash">The query hash as a 0x hex string.</param>
        public void ShowQueryHash(int instanceId, string queryHash) =>
            Show(instanceId, 0, ("@QueryHash", queryHash.Trim()));

        /// <summary>
        /// The statements that ran under one plan shape on an instance - usually one, more where statements in several
        /// procedures compile to the same plan.  Not narrowed to a database, as <see cref="ShowQueryHash"/> isn't.
        /// </summary>
        /// <param name="planHash">The plan hash as a 0x hex string.</param>
        public void ShowPlanHash(int instanceId, string planHash) =>
            Show(instanceId, 0, ("@PlanHash", planHash.Trim()));

        private void Show(int instanceId, int databaseId, params (string Name, object Value)[] parameters)
        {
            var report = QueryStatsReport.Instance;
            var context = CommonData.GetDBADashContext(instanceId);
            // An Azure database is an instance of its own, so the whole instance is the database - see Main.GetQueryStatsTabContext
            context.DatabaseID = context.Type == SQLTreeItem.TreeType.AzureDatabase ? 0 : Math.Max(databaseId, 0);
            context.Report = report;
            view.Report = report;

            var customParams = report.GetCustomSqlParameters();
            foreach (var (name, value) in parameters)
            {
                var param = customParams.FirstOrDefault(p => p.Param.ParameterName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (param == null) continue;
                param.Param.Value = value;
                param.UseDefaultValue = false;
            }

            _ = view.SetContext(context, customParams);
            this.ApplyTheme();
        }
    }
}
