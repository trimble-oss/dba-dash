using DBADashGUI.Performance;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class JobTimeline : UserControl, ISetContext, IRefreshData
    {
        private readonly string template = @"
<html>
    <head>
        <style>
            .tt {background-color:#ffffff;  color: #004f83 ; padding: 0px 0px 0px 0px;font-size: 15px;font-family: Arial, Helvetica, sans-serif; }
            .tt h1 {color: #ffffff; background-color:" + DashColors.Success.ToHexString() + @"; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            .ttf {background-color:#ffffff; color: #004f83 ; padding: 0px 0px 0px 0px;font-size: 15px;font-family: Arial, Helvetica, sans-serif; }
            .ttf h1 {color: #ffffff; background-color:" + DashColors.Fail.ToHexString() + @"; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            .ttw {background-color:#ffffff; color: #004f83 ; padding: 0px 0px 0px 0px;font-size: 15px;font-family: Arial, Helvetica, sans-serif; }
            .ttw h1 {color: #ffffff; background-color:" + DashColors.Warning.ToHexString() + @"; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;font-size: 15px }
            h1 {color: #ffffff; background-color:#0063a3; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;}
        </style>
        <script type=""text/javascript"" src=""https://www.gstatic.com/charts/loader.js""></script>
        <script type=""text/javascript"">
          google.charts.load('current', {'packages':['timeline']});
          google.charts.setOnLoadCallback(drawChart);

          function drawChart() {
            var container = document.getElementById('job-timeline');
            var chart = new google.visualization.Timeline(container);
            var dataTable = new google.visualization.DataTable();

            dataTable.addColumn({ type: 'string', id: 'Job' });
            dataTable.addColumn({ type: 'string', id: 'label' });
            dataTable.addColumn({ type: 'string', role: 'style' });
            dataTable.addColumn({ type: 'string', role: 'tooltip' });
            dataTable.addColumn({ type: 'date', id: 'Start' });
            dataTable.addColumn({ type: 'date', id: 'End' });
            dataTable.addRows([
              ##DATA##
            ]);

            var options = {
                timeline: {   colorByRowLabel: true,
                            rowLabelStyle: {color: '#004f83' }
                },
                backgroundColor: '#ffffff',
                alternatingRowStyle: false,
                height:##HEIGHT##,
                hAxis: {
                    format: '##DATEFORMAT##'
                }
            };

            google.visualization.events.addListener(chart, 'ready', function () {
                // find <rect> elements for outer rectangle formatting
                var rects = container.getElementsByTagName('rect');
                Array.prototype.forEach.call(rects, function(rect) {
                    if (rect.getAttribute('stroke') === '#9a9a9a') {
                    rect.setAttribute('stroke', '#004f83');
                    rect.setAttribute('stroke-width', '');
                    rect.setAttribute('stroke-dasharray', '1,1');
                    }
                });

                // find <path> elements for vertical/horizontal gridlines
                var paths = container.getElementsByTagName('path');
                Array.prototype.forEach.call(paths, function(path) {
                  // vertical
                  if ((path.getAttribute('stroke') === '#ffffff') || (path.getAttribute('stroke') === '#e6e6e6')) {
                    path.setAttribute('stroke', '#004f83');
                    path.setAttribute('stroke-dasharray', '1,1');
                  }
                  // horizontal
                  if(path.getAttribute('stroke') === '#b7b7b7'){
                    path.setAttribute('stroke', '#004f83');
                    path.setAttribute('stroke-width', '0.5');
                    path.setAttribute('stroke-dasharray', '1,1');
                    }
                });

                // find <text> elements for formatting axis labels
                var labels = container.getElementsByTagName('text');
                  Array.prototype.forEach.call(labels, function(label) {
                    if (label.getAttribute('text-anchor') === 'middle') {
                      label.setAttribute('fill', '#004f83');
                    }
                  });
                });
                chart.draw(dataTable,options);
          }
        </script>
    </head>
    <body>
        <h1>##SERVERNAME##</h1>
        <div id=""job-timeline""></div>
  </body>
</html>";

        private static readonly string NoDataHTMLTemplate = @"
<html>
    <head>
        <style>
            h1 {color: #ffffff; background-color:#0063a3; font-family: Arial, Helvetica, sans-serif; border: 1px solid #252a2e; text-align: center;}
            h3 {color: #0063a3; font-family: Arial, Helvetica, sans-serif;}
        </style>
    </head>
    <body>
        <h1>##SERVERNAME##</h1>
        <h3>No Data</h3>
    </body>
</html>";

        private string html;
        private static readonly string tempFilePrefix = "DBADashJobTimeline_";
        private DataTable dt;
        private DateTime from;
        private DateTime to;
        private DBADashContext context;
        private int categoryInstanceID;
        private string selectedCategory;

        private enum RunStatus
        {
            Failed = 0,
            Retry = 2,
            Succeeded = 1,
            Cancelled = 3,
            InProgress = 4
        }

        /// <summary>
        /// Get temp file name for writing HTML timeline out to disk.  Required if size is over 2MB
        /// </summary>
        private static string GetTempFilePath()
        {
            return Path.Combine(System.IO.Path.GetTempPath(), tempFilePrefix + Guid.NewGuid().ToString() + ".html"); // File name will be reused to avoid creating large numbers of temp files that might not get cleaned up
        }

        public JobTimeline()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Get DataTable with Job Timeline data.  Dates converted to app timezone
        /// </summary>
        private static DataTable GetJobTimelineData(int InstanceID, DateTime from, DateTime to, string category, Guid job_id, bool steps, bool outcome)
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
            RefreshCategories();
            await webCtrl.EnsureCoreWebView2Async(null);
            from = DateRange.FromUTC;
            to = DateRange.ToUTC;
            bool steps = stepsToolStripMenuItem.Checked || context.JobID != Guid.Empty;
            bool outcome = outcomeToolStripMenuItem.Checked || context.JobID != Guid.Empty;
            dt = GetJobTimelineData(context.InstanceID, from, to, selectedCategory, context.JobID, steps, outcome);
            DrawTimeline();
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

        /// <summary>
        /// Generate and load timeline HTML
        /// </summary>
        private async void DrawTimeline()
        {
            if (webCtrl == null || webCtrl.IsDisposed || webCtrl.Disposing || dt == null)
            {
                return;
            }
            await webCtrl.EnsureCoreWebView2Async(null);

            GenerateHTML();

            try
            {
                webCtrl.NavigateToString(html);
            }
            catch (Exception ex)
            {
                LoadHTMLFromDisk(ex); // NavigateToString might fail if size exceeds 2MB.  Try loading from disk instead.
            }
        }

        /// <summary>
        /// Get HTML for Job timeline
        /// </summary>
        private void GenerateHTML()
        {
            if (dt.Rows.Count == 0)
            {
                html = NoDataHTMLTemplate.Replace("##SERVERNAME##", context.InstanceName);
                return;
            }
            StringBuilder sb = new();
            DateTime appFrom = from.ToAppTimeZone();
            DateTime appTo = to.ToAppTimeZone();
            string previousName = string.Empty;
            int rowCount = 0;
            foreach (DataRow r in dt.Rows)
            {
                DateTime actualStart = (DateTime)r["RunDateTime"];
                DateTime actualEnd = (DateTime)r["FinishDateTime"];
                int Duration = (int)r["RunDurationSec"];
                string name = (string)r["name"];
                var runStatus = (RunStatus)r["run_status"];
                string step = (string)r["step_name"];
                string encodedName = HttpUtility.JavaScriptStringEncode(context.JobID == Guid.Empty ? name : step);
                if (encodedName != previousName)
                {
                    rowCount++;
                }

                // Get truncated start/end for drawing within the selected time period (for jobs that started before or finished after)
                DateTime start = actualStart < appFrom ? appFrom : actualStart;
                DateTime end = actualEnd > appTo ? appTo : actualEnd;
                bool isTruncated = actualStart < start || actualEnd > end; //Truncated if started before selected time period or finished after
                Color statusColor = GetStatusColor(runStatus, isTruncated);
                string toolTip = GetToolTip(name, step, actualStart, actualEnd, Duration, runStatus);

                sb.AppendFormat("[ '{0}', '{1}', '{2}', '{3}', new Date('{4}'), new Date('{5}') ],\n",
                    encodedName,
                    "",
                    statusColor.ToHexString(),
                    toolTip,
                    start.ToString("s", System.Globalization.CultureInfo.InvariantCulture),
                    end.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                previousName = encodedName;
            }
            html = template.Replace("##DATEFORMAT##", DateFormat).Replace("##SERVERNAME##", context.InstanceName).Replace("##HEIGHT##", ChartHeight(rowCount).ToString()).ToString();
            html = html.Replace("##DATA##", sb.ToString());
        }

        /// <summary>
        /// Get a HTML formatted tooltip for the job execution
        /// </summary>
        private static string GetToolTip(string name, string step, DateTime actualStart, DateTime actualEnd, int Duration, RunStatus status)
        {
            // Tooltip css class
            string ttClass = status switch
            {
                RunStatus.Succeeded => "tt",
                RunStatus.Retry => "ttw",
                RunStatus.InProgress => "ttw",
                _ => "ttf"
            };
            return String.Format("<span class=\"{5}\"><h1>{0}</h1>Step: {6}<br/>Start: {1}<br/>End: {2}<br/>Duration: {3}<br/>Status: {4}</span>",
                             HttpUtility.HtmlEncode(name),
                             actualStart.ToString(),
                             actualEnd.ToString(),
                             TimeSpan.FromSeconds(Duration).Humanize(4),
                             status.ToString(),
                             ttClass,
                             step);
        }

        /// <summary>
        /// Load HTML from disk instead of using NavigateToString.  Required if HTML is over 2MB.
        /// </summary>
        private void LoadHTMLFromDisk(Exception ex)
        {
            TryDeleteTempFiles(); // Cleanup previous temp files
            try
            {
                string tempFilePath = GetTempFilePath(); // Generate a unique file.  Setting source to same file doesn't refresh
                System.IO.File.WriteAllText(tempFilePath, html);
                webCtrl.Source = new Uri(tempFilePath);
            }
            catch (Exception ex2)
            {
                webCtrl.NavigateToString(String.Format("<html><body style='background-color:{0};color:#ffffff'>Error loading HTML:<br/>{1}</body></html>", DashColors.Fail.ToHexString(), HttpUtility.HtmlEncode(ex.ToString()) + "<br/>" + HttpUtility.HtmlEncode(ex2.ToString())));
            }
        }

        /// <summary>
        /// Remove temp file that is written to when NavigateToString fails (Can occur due to 2MB size limit)
        /// </summary>
        private static void TryDeleteTempFiles()
        {
            try
            {
                string pattern = tempFilePrefix + "*.html";
                foreach (string f in Directory.EnumerateFiles(System.IO.Path.GetTempPath(), pattern))
                {
                    File.Delete(f);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting temp file:" + ex.ToString());
            }
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
            tsIncludeSteps.Visible = context.JobID == Guid.Empty;
            RefreshData();
        }

        private void JobTimeLine_Resize(object sender, EventArgs e)
        {
            DrawTimeline();
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyHtmlToClipBoard(html);
        }

        private void Include_Steps(object sender, EventArgs e)
        {
            stepsToolStripMenuItem.Checked = stepsToolStripMenuItem == sender;
            outcomeToolStripMenuItem.Checked = outcomeToolStripMenuItem == sender;
            tsIncludeSteps.Text = ((ToolStripMenuItem)sender).Text;
            RefreshData();
        }
    }
}