using Humanizer;
using System;
using System.Globalization;
using System.Web;

namespace DBADashGUI.AgentJobs
{
    internal class TimelineRow
    {
        internal enum RunStatus
        {
            Failed = 0,
            Retry = 2,
            Succeeded = 1,
            Cancelled = 3,
            InProgress = 4
        }

        public DateTime ActualStart { get; set; }
        public DateTime ActualEnd { get; set; }

        // Get truncated start/end for drawing within the selected time period (for jobs that started before or finished after)
        public DateTime Start => ActualStart < AppFrom ? AppFrom : ActualStart;

        public DateTime End => ActualEnd > AppTo ? AppTo : ActualEnd;
        public int ExecutionCount { get; set; }
        public RunStatus Status { get; set; }
        public string EncodedName => HttpUtility.JavaScriptStringEncode(Name);
        public string EncodedToolTip => HttpUtility.JavaScriptStringEncode(ToolTip);
        public int Duration { get; set; }
        public string Name { get; set; }
        public string JobName { get; set; }
        public string Step { get; set; }
        public bool IsTruncated => ActualStart < Start || ActualEnd > End; //Truncated if started before selected time period or finished after
        public DateTime AppFrom { get; set; }
        public DateTime AppTo { get; set; }
        public int StepID { get; set; }

        public string TooltipCSS => Status switch
        {
            RunStatus.Succeeded => "tt",
            RunStatus.Retry => "ttw",
            RunStatus.InProgress => "ttw",
            _ => "ttf"
        };

        /// <summary>
        /// Get a HTML formatted tooltip for the job execution.
        /// A job that is still running has no finish time; show elapsed time instead of an end.
        /// </summary>
        public string ToolTip
        {
            get
            {
                var isRunning = Status == RunStatus.InProgress;
                return string.Format("<span class=\"{5}\"><h1>{0}</h1>Step {9}: {6}<br/>Start: {1}<br/>End: {2}<br/>{8}: {3}<br/>Executions: {7}<br/>Status: {4}</span>",
                             HttpUtility.HtmlEncode(JobName),
                             ActualStart.ToString(CultureInfo.CurrentCulture),
                             isRunning ? "(still running)" : ActualEnd.ToString(CultureInfo.CurrentCulture),
                             TimeSpan.FromSeconds(Duration).Humanize(4),
                             isRunning ? "In Progress" : Status.ToString(),
                             TooltipCSS,
                             HttpUtility.HtmlEncode(Step),
                             ExecutionCount.ToString(),
                             isRunning ? "Elapsed" : ExecutionCount == 1 ? "Duration" : "Avg Duration",
                             StepID.ToString());
            }
        }
    }
}