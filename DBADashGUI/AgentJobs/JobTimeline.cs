using DBADashGUI.Performance;
using Humanizer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Automation;
using System.Windows.Forms;
using static DBADashGUI.AgentJobs.TimelineRow;
using static System.Net.WebRequestMethods;

namespace DBADashGUI.AgentJobs
{
    public partial class JobTimeline : UserControl, ISetContext, IRefreshData
    {
        private readonly string template = @"
<html>
    <head>
        <style>
            .tt { padding: 0px 0px 0px 0px; }
            .tt h1 {color: ##SUCCESS_F_COLOR##; background-color:##SUCCESS_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            .ttf { padding: 0px 0px 0px 0px; }
            .ttf h1 {color: ##FAIL_F_COLOR##;; background-color:##FAIL_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            .ttw { padding: 0px 0px 0px 0px;}
            .ttw h1 {color: ##WARNING_F_COLOR##; background-color:##WARNING_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            h1 {color: ##TITLE_F_COLOR##; background-color:##TITLE_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;}
            body {background-color: ##BODY_B_COLOR##; color: ##BODY_F_COLOR## }
            div.google-visualization-tooltip { background-color: ##TOOLTIP_B_COLOR##; color: ##TOOLTIP_F_COLOR##; font-size: 15px;font-family: Arial, Helvetica, sans-serif;  }
        </style>
        <script type=""text/javascript"" src=""https://www.gstatic.com/charts/loader.js""></script>
        <script type=""text/javascript"">
            google.charts.load('current', {
                'packages': ['timeline']
            });
            google.charts.setOnLoadCallback(drawChart);

            function drawChart() {
                var container = document.getElementById('job-timeline');
                var chart = new google.visualization.Timeline(container);
                var dataTable = new google.visualization.DataTable();

                dataTable.addColumn({
                    type: 'string',
                    id: 'Job'
                });
                dataTable.addColumn({
                    type: 'string',
                    id: 'label'
                });
                dataTable.addColumn({
                    type: 'string',
                    role: 'style'
                });
                dataTable.addColumn({
                    type: 'string',
                    role: 'tooltip'
                });
                dataTable.addColumn({
                    type: 'date',
                    id: 'Start'
                });
                dataTable.addColumn({
                    type: 'date',
                    id: 'End'
                });
                dataTable.addRows([
                    ##DATA##
                ]);

                var options = {
                    timeline: {
                        colorByRowLabel: true,
                        rowLabelStyle: {
                            color: '##LABEL_COLOR##'
                        }
                    },
                    backgroundColor: '##CHART_B_COLOR##',
                    alternatingRowStyle: false,
                    height: ##HEIGHT##,
                    hAxis: {
                        format: '##DATEFORMAT##'
                    },
                    tooltip: { isHtml: true },
                };

                google.visualization.events.addListener(chart, 'ready', function() {
                    // find <rect> elements for outer rectangle formatting
                    var rects = container.getElementsByTagName('rect');
                    Array.prototype.forEach.call(rects, function(rect) {
                        if (rect.getAttribute('stroke') === '#9a9a9a') {
                            rect.setAttribute('stroke', '##GRID_COLOR##');
                            rect.setAttribute('stroke-width', '');
                            rect.setAttribute('stroke-dasharray', '1,1');
                        }
                    });

                    // find <path> elements for vertical/horizontal gridlines
                    var paths = container.getElementsByTagName('path');
                    Array.prototype.forEach.call(paths, function(path) {
                        path.setAttribute('stroke', '##GRID_COLOR##');
                        path.setAttribute('stroke-dasharray', '1,1');
                        path.setAttribute('stroke-width', '0.5');
                    });

                    // find <text> elements for formatting axis labels
                    var labels = container.getElementsByTagName('text');
                    Array.prototype.forEach.call(labels, function(label) {
                        label.setAttribute('fill', '##LABEL_COLOR##');
                    });
                });

                chart.draw(dataTable, options);
            }
        </script>
    </head>
    <body>
        <h1>##SERVERNAME##</h1>
        <div id=""job-timeline""></div>
  </body>
</html>";

        private const string NoDataHTMLTemplate = @"
<html>
    <head>
        <style>
            h1 {color: ##TITLE_F_COLOR##; background-color:##TITLE_B_COLOR##; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;}
            h3 {font-family: Arial, Helvetica, sans-serif;}
            body {background-color: ##BODY_B_COLOR##; color: ##BODY_F_COLOR## }
        </style>
    </head>
    <body>
        <h1>##SERVERNAME##</h1>
        <h3>No Data</h3>
    </body>
</html>";

        private string html;
        private DataTable dt;
        private DateTime from;
        private DateTime to;
        private DBADashContext context;
        private int categoryInstanceID;
        private string selectedCategory;
        private int mins = -1;
        public bool IsActive { get; set; }

        private bool IncludeSteps => stepsToolStripMenuItem.Checked || bothToolStripMenuItem.Checked || context.JobID != Guid.Empty;
        private bool IncludeOutcome => outcomeToolStripMenuItem.Checked || bothToolStripMenuItem.Checked || context.JobID != Guid.Empty;

        private int DateGrouping
        {
            get => (int)tsDateGroup.Tag;
            set
            {
                tsDateGroup.Tag = value;
                tsDateGroup.Text = DateHelper.DateGroupString(value);
            }
        }

        public JobTimeline()
        {
            InitializeComponent();
            DateHelper.AddDateGroups(tsDateGroup, TsDateGroup_Click);
        }

        private void TsDateGroup_Click(object sender, EventArgs e)
        {
            var ts = (ToolStripMenuItem)sender;
            DateGrouping = Convert.ToInt32(ts.Tag);
            RefreshData();
        }

        /// <summary>
        /// Get DataTable with Job Timeline data.  Dates converted to app timezone
        /// </summary>
        private static DataTable GetJobTimelineData(int InstanceID, DateTime from, DateTime to, string category, Guid job_id, bool steps, bool outcome, int dateGroup)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobTimeline_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("@FromDate", from);
                cmd.Parameters.AddWithValue("@ToDate", to);
                cmd.Parameters.AddWithValue("@IncludeSteps", steps);
                cmd.Parameters.AddWithValue("@IncludeOutcome", outcome);
                cmd.Parameters.AddGuidIfNotEmpty("@job_id", job_id);
                cmd.Parameters.AddStringIfNotNullOrEmpty("category", category);
                cmd.Parameters.AddWithValue("@DateGroupingMin", dateGroup);

                DataTable dt = new();
                da.Fill(dt);
                DateHelper.ConvertUTCToAppTimeZone(ref dt);
                return dt;
            }
        }

        private static DataTable GetJobCategories(int InstanceID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.JobCategories_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@InstanceID", InstanceID);
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
        }

        /// <summary>
        /// Refresh job categories menu if context has changed
        /// </summary>
        private void RefreshCategories()
        {
            if (context.InstanceID != categoryInstanceID)
            {
                string allCategoriesText = "ALL Categories";
                selectedCategory = string.Empty;
                tsCategories.Text = allCategoriesText;
                tsCategories.DropDownItems.Clear();
                var tsALL = new ToolStripMenuItem() { Text = allCategoriesText, Tag = string.Empty, Checked = true };
                tsALL.Click += Category_Selected;
                tsCategories.DropDownItems.Add(tsALL);
                tsCategories.DropDownItems.Add(new ToolStripSeparator());
                DataTable dtCategories = GetJobCategories(context.InstanceID);
                foreach (DataRow row in dtCategories.Rows)
                {
                    string category = (string)row["category"];
                    var tsCat = new ToolStripMenuItem() { Text = category, Tag = category };
                    tsCat.Click += Category_Selected;
                    tsCategories.DropDownItems.Add(tsCat);
                }
                categoryInstanceID = context.InstanceID;
            }
        }

        private void Category_Selected(object sender, EventArgs e)
        {
            string selected = (string)((ToolStripMenuItem)sender).Tag;
            string selectedText = ((ToolStripMenuItem)sender).Text;
            tsCategories.Text = selectedText;
            tsCategories.CheckSingleItem((ToolStripMenuItem)sender);
            selectedCategory = selected;
            RefreshData();
        }

        public async void RefreshData()
        {
            if (mins != DateRange.DurationMins)
            {
                DateGrouping = DateHelper.DateGrouping(DateRange.DurationMins, 1440);
                mins = DateRange.DurationMins;
            }
            RefreshCategories();
            from = DateRange.FromUTC;
            to = DateRange.ToUTC;

            dt = GetJobTimelineData(context.InstanceID, from, to, selectedCategory, context.JobID, IncludeSteps, IncludeOutcome, DateGrouping);
            await DrawTimeline();
        }

        /// <summary>
        /// Return a color based on job RunStatus
        /// </summary>
        private static Color GetStatusColor(RunStatus status, bool isTruncated)
        {
            if (isTruncated) /* Use a lighter colour to indicate job started before or finished after selected time period */
            {
                return status switch
                {
                    RunStatus.Succeeded => DashColors.GreenPale,
                    RunStatus.Retry => DashColors.YellowPale,
                    RunStatus.InProgress => DashColors.BluePale,
                    _ => DashColors.RedPale
                };
            }
            else
            {
                return status switch
                {
                    RunStatus.Succeeded => DashColors.Success,
                    RunStatus.Retry => DashColors.Warning,
                    RunStatus.InProgress => DashColors.TrimbleBlueDark,
                    _ => DashColors.Fail
                };
            }
        }

        // <summary>
        // Generate and load timeline HTML
        // </summary>
        private async Task DrawTimeline()
        {
            GenerateHTML();
            await WebView2Wrapper1.NavigateToLargeString(html);
        }

        /// <summary>
        /// Get HTML for Job timeline
        /// </summary>
        private void GenerateHTML()
        {
            if (dt.Rows.Count == 0)
            {
                html = ReplaceColors(NoDataHTMLTemplate.Replace("##SERVERNAME##", context.InstanceName));
                return;
            }
            StringBuilder sb = new();
            DateTime appFrom = from.ToAppTimeZone();
            DateTime appTo = to.ToAppTimeZone();
            int rowCount = 0;
            TimelineRow previousTlr = new();
            List<TimelineRow.RunStatus> statuses = new();
            foreach (DataRow r in dt.Rows)
            {
                TimelineRow tlr = new()
                {
                    ActualStart = (DateTime)r["RunDateTime"],
                    ActualEnd = (DateTime)r["FinishDateTime"],
                    Duration = (int)r["RunDurationSec"],
                    JobName = (string)r["name"],
                    Status = (RunStatus)r["run_status"],
                    Step = (string)r["step_name"],
                    ExecutionCount = (int)r["Executions"],
                    AppFrom = appFrom,
                    AppTo = appTo,
                    StepID = (int)r["step_id"]
                };
                if (context.JobID == Guid.Empty)
                {
                    tlr.Name = tlr.JobName + (IncludeSteps ? " | " + tlr.StepID.ToString() : "");
                }
                else
                {
                    tlr.Name = tlr.StepID + " : " + tlr.Step;
                }

                if (tlr.Name != previousTlr.Name)
                {
                    rowCount++;
                    statuses.Clear();
                    statuses.Add(tlr.Status);
                }
                else if (tlr.Start < previousTlr.End && !statuses.Contains(tlr.Status))
                {
                    rowCount++;
                    statuses.Add(tlr.Status);
                }

                Color statusColor = GetStatusColor(tlr.Status, tlr.IsTruncated);

                sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                    tlr.EncodedName,
                    "",
                    statusColor.ToHexString(),
                    tlr.ToolTip,
                    tlr.Start.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    tlr.End.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                previousTlr = tlr;
            }
            html = ReplaceColors(template.Replace("##DATEFORMAT##", DateFormat).Replace("##SERVERNAME##", context.InstanceName).Replace("##HEIGHT##", ChartHeight(rowCount).ToString()));
            html = html.Replace("##DATA##", sb.ToString());
        }

        private static string ReplaceColors(string html)
        {
            var theme = DBADashUser.SelectedTheme;
            return html.Replace("##WARNING_B_COLOR##", theme.WarningBackColor.ToHexString())
                .Replace("##WARNING_F_COLOR##", theme.WarningForeColor.ToHexString())
                .Replace("##FAIL_B_COLOR##", theme.CriticalBackColor.ToHexString())
                .Replace("##FAIL_F_COLOR##", theme.CriticalForeColor.ToHexString())
                .Replace("##SUCCESS_B_COLOR##", theme.SuccessBackColor.ToHexString())
                .Replace("##SUCCESS_F_COLOR##", theme.SuccessForeColor.ToHexString())
                .Replace("##TITLE_F_COLOR##", theme.TimelineTitleForeColor.ToHexString())
                .Replace("##TITLE_B_COLOR##", theme.TimelineTitleBackColor.ToHexString())
                .Replace("##BODY_B_COLOR##", theme.TimelineBodyBackColor.ToHexString())
                .Replace("##BODY_F_COLOR##", theme.TimelineBodyForeColor.ToHexString())
                .Replace("##LABEL_COLOR##", theme.TimelineLabelColor.ToHexString())
                .Replace("##CHART_B_COLOR##", theme.TimelineChartBackColor.ToHexString())
                .Replace("##GRID_COLOR##", theme.TimelineGridColor.ToHexString())
                .Replace("##TOOLTIP_B_COLOR##", theme.TimelineToolTipBackColor.ToHexString())
                .Replace("##TOOLTIP_F_COLOR##", theme.TimelineToolTipForeColor.ToHexString());
        }

        private static int ChartHeight(int rows)
        {
            return (42 * rows) + 100;
        }

        /// <summary>
        /// Use a suitable date format based on duration.
        /// </summary>
        private static string DateFormat => DateRange.DurationMins < 1500 ? "HH:mm" : "MMM dd HH:mm";

        public void SetContext(DBADashContext context)
        {
            this.context = context;
            IsActive = true;
            tsIncludeSteps.Visible = context.JobID == Guid.Empty;
            RefreshData();
        }

        private async void JobTimeLine_Resize(object sender, EventArgs e)
        {
            if (IsActive)
            {
                await DrawTimeline();
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void Include_Steps(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            tsIncludeSteps.CheckSingleItem(item);
            stepsToolStripMenuItem.Checked = stepsToolStripMenuItem == sender;
            outcomeToolStripMenuItem.Checked = outcomeToolStripMenuItem == sender;
            tsIncludeSteps.Text = item.Text;
            RefreshData();
        }

        private void Copy_HTML(object sender, EventArgs e)
        {
            Common.CopyHtmlToClipBoard(html);
        }

        private void Copy_Image(object sender, EventArgs e)
        {
            WebView2Wrapper1.CopyImageToClipboard();
        }

        private void WebView2_SetupCompleted()
        {
            this.Invoke(RefreshData);
        }
    }
}