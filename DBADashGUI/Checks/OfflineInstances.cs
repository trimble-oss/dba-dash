using DBADashGUI.AgentJobs;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static DBADashGUI.AgentJobs.TimelineRow;

namespace DBADashGUI.Checks
{
    public partial class OfflineInstances : UserControl, IRefreshData, ISetContext
    {
        private DBADashContext CurrentContext;
        private string html;

        public static SystemReport OfflineInstancesReport => new()
        {
            ReportName = "Offline Instances",
            SchemaName = "dbo",
            ProcedureName = "OfflineInstances_Get",
            QualifiedProcedureName = "dbo.OfflineInstances_Get",
            CanEditReport = false,
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceIDs",
                        ParamType = "IDS"
                    },
                },
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceID", "Instance ID" },
                            { "InstanceDisplayName", "Instance" },
                            { "FirstFail", "First Fail" },
                            { "LastFail", "Last Fail" },
                            {"TimeSinceLastFail","Time Since Last Fail"},
                            { "LastCollection", "Last Collection" },
                            { "Duration", "Duration"},
                            { "TimeSinceLastCollection", "Time Since Last Collection" },
                            { "FailCount", "Fail Count" },
                            { "FirstMessage", "First Message" },
                            { "LastMessage", "Last Message" },
                        },
                        ResultName = "Server Services",
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                        {
                            new("InstanceID", new PersistedColumnLayout { Visible = false }),
                            new("InstanceDisplayName", new PersistedColumnLayout {  Visible = true }),
                            new("FirstFail", new PersistedColumnLayout {  Visible = true }),
                            new("LastFail", new PersistedColumnLayout {  Visible = true }),
                            new("TimeSinceLastFail", new PersistedColumnLayout {  Visible = true }),
                            new("LastCollection", new PersistedColumnLayout {  Visible = true }),
                            new ("Duration", new PersistedColumnLayout {  Visible = true }),
                            new ("TimeSinceLastCollection", new PersistedColumnLayout {  Visible = true }),
                            new("FailCount", new PersistedColumnLayout {  Visible =  true }),
                            new("FirstMessage", new PersistedColumnLayout {  Visible = true }),
                            new("LastMessage", new PersistedColumnLayout {  Visible = true })
                        },
                    }
                }
            },
        };

        public OfflineInstances()
        {
            InitializeComponent();
            customReportView1.PostGridRefresh += PostGridRefresh;
            customReportView1.Report = OfflineInstancesReport;
        }

        private async void PostGridRefresh(object sender, EventArgs e)
        {
            if (customReportView1.Grids.Count > 0)
            {
                customReportView1.Grids[0].AutoResizeColumnsWithMaxColumnWidth();
            }

            await RefreshTimeline();
        }

        private async Task<DataTable> GetOfflineTimeline()
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            await using var cmd = new SqlCommand("dbo.OfflineInstanceTimeline_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceIDs", CurrentContext.InstanceIDs.AsDataTable());
            cmd.Parameters.AddWithValue("@FromDate", DateRange.FromUTC);
            cmd.Parameters.AddWithValue("@ToDate", DateRange.ToUTC);
            await cn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        private async Task DrawTimeline()
        {
            await webView2Wrapper1.NavigateToLargeString(html);
        }

        private async Task RefreshTimeline()
        {
            var dt = await GetOfflineTimeline();
            if (dt.Rows.Count == 0)
            {
                html = JobTimeline.NoDataHTMLTemplate;
                html = JobTimeline.ReplaceColors(html.Replace("##SERVERNAME##", "Offline Instances"));
                await webView2Wrapper1.NavigateToLargeString(html);
                return;
            }
            StringBuilder sb = new();
            var appFrom = DateRange.FromUTC.ToAppTimeZone();
            var appTo = DateRange.ToUTC.ToAppTimeZone();
            var rowCount = dt.Rows.Cast<DataRow>().Select(row => row.Field<int>("InstanceID")).Distinct().Count();
            TimelineRow previousTlr = new();
            List<RunStatus> statuses = new();
            var lastInstance = "";
            var minStart = DateTime.MaxValue;
            var maxEnd = DateTime.MinValue;
            foreach (DataRow r in dt.Rows)
            {
                var instanceName = HttpUtility.HtmlEncode(r.Field<string>("InstanceDisplayName"));
                var firstFail = r.Field<DateTime>("FirstFail").ToAppTimeZone();
                var lastFail = r.Field<DateTime>("LastFail").ToAppTimeZone();
                var closed = r.IsNull("ClosedDate") ? DateHelper.AppNow : r.Field<DateTime>("ClosedDate").ToAppTimeZone();
                var start = firstFail < appFrom ? appFrom : firstFail;
                var end = lastFail > appTo ? appTo : lastFail;

                var message = HttpUtility.HtmlEncode(r.Field<string>("Message")).Replace(",", "<br>").Replace(Environment.NewLine, "<br>").Replace("\n", "<br>");
                if (start < minStart) minStart = start;
                if (end > maxEnd) maxEnd = end;
                lastInstance = instanceName;
                sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                    instanceName,
                    "",
                    DashColors.Fail.ToHexString(),
                    $"{instanceName} Offline<br>From:{firstFail:g}<br>To:{lastFail:g}<br>Duration: ({lastFail.Subtract(firstFail).Humanize(2)})<br>Message: {message}",
                    start.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    end.ToString("s", System.Globalization.CultureInfo.InvariantCulture));

                if (closed > end && end < appTo)
                {
                    start = end;
                    end = closed > appTo ? appTo : closed;
                    sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                        instanceName,
                        "",
                        DashColors.RedLight.ToHexString(),
                        $"{instanceName} Unknown<br>From:{start:g}<br>To:{end:g}<br>Duration: ({end.Subtract(start).Humanize(2)})",
                        start.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                        end.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            if (minStart.Subtract(appFrom).TotalMinutes > 1)
            {
                sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                    lastInstance,
                    "",
                    JobTimeline.ReplaceColors("##BODY_B_COLOR##"),
                    "",
                    appFrom.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    appFrom.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (appTo.Subtract(maxEnd).TotalMinutes > 1)
            {
                sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                    lastInstance,
                    "",
                    JobTimeline.ReplaceColors("##BODY_B_COLOR##"),
                    "",
                    appTo.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    appTo.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
            }

            html = JobTimeline.ReplaceColors(JobTimeline.Template.Replace("##DATEFORMAT##", DateFormat).Replace("##SERVERNAME##", "Offline Instances Timeline" + (CurrentContext.InstanceID > 0 ? $" for {CurrentContext.InstanceName}" : "")).Replace("##HEIGHT##", ChartHeight(rowCount).ToString()));
            html = html.Replace("##DATA##", sb.ToString());
            await webView2Wrapper1.NavigateToLargeString(html);
        }

        private static string DateFormat => DateRange.DurationMins < 1500 ? "HH:mm" : "MMM dd HH:mm";

        private static int ChartHeight(int rows)
        {
            return (42 * rows) + 100;
        }

        public void SetContext(DBADashContext _context)
        {
            CurrentContext = _context;
            customReportView1.SetContext(_context);
            RefreshData();
        }

        public void RefreshData()
        {
            customReportView1.RefreshData();
        }

        private void Copy_HTML(object sender, EventArgs e)
        {
            Clipboard.SetText(html);
        }

        private void Copy_Image(object sender, EventArgs e)
        {
            webView2Wrapper1.CopyImageToClipboard();
        }

        private async void OfflineInstances_Resize(object sender, EventArgs e)
        {
            if (this.IsTrulyVisible() && !string.IsNullOrEmpty(html))
            {
                await DrawTimeline();
            }
        }
    }
}