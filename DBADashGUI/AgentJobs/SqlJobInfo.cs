using DBADashGUI.Changes;
using DBADashGUI.Theme;
using DocumentFormat.OpenXml.Office2010.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public class SqlJobInfo
    {
        public string JobName { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public string Category { get; set; }
        public string Owner { get; set; }
        public List<SqlJobStep> Steps { get; set; } = new List<SqlJobStep>();
        public List<SqlJobSchedule> Schedules { get; set; } = new List<SqlJobSchedule>();

        public string NotifyEmailOperator { get; set; }

        public string NotifyPageOperator { get; set; }

        public int NotifyLevelEmail { get; set; }

        public int NotifyLevelPage { get; set; }

        public int NotifyLevelEventLog { get; set; }

        public int DeleteLevel { get; set; }

        public string DeleteLevelDescription => GetNotificationLevelDescription(DeleteLevel);

        public string NotifyLevelEventLogDescription => GetNotificationLevelDescription(NotifyLevelEventLog);

        public string NotifyLevelEmailDescription => GetNotificationLevelDescription(NotifyLevelEmail);

        public string NotifyLevelPageDescription => GetNotificationLevelDescription(NotifyLevelPage);

        private string GetNotificationLevelDescription(int level) => level switch
        {
            0 => "Never",
            1 => "On Success",
            2 => "On Failure",
            3 => "On Completion",
            _ => level.ToString(),
        };

        public DataTable GetSchedulesDataTable()
        {
            DataTable dt = new("JobSchedules");
            dt.Columns.Add("UID", typeof(Guid));
            dt.Columns.Add("Schedule", typeof(string));
            dt.Columns.Add("Enabled", typeof(bool));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Schedule Start", typeof(DateTime));
            dt.Columns.Add("Schedule End", typeof(DateTime));

            foreach (var schedule in Schedules)
            {
                dt.Rows.Add(schedule.ScheduleUid, schedule.Name, schedule.Enabled, schedule.GetFrequencyDescription(), schedule.ScheduleStartDateTime, schedule.ScheduleEndDateTime);
            }

            return dt;
        }

        public static SqlJobInfo GetJobInfo(DBADashContext context) => GetJobInfo(context.InstanceID, context.JobID);

        public static SqlJobInfo GetJobInfo(int instanceId, Guid jobId)
        {
            var history = JobDDLHistory.GetDDLHistory(instanceId, jobId);
            var id = history.Rows[0].Field<long>("DDLID");
            return GetJobInfo(id);
        }

        public static SqlJobInfo GetJobInfo(long ddlID)
        {
            var ddl = Common.DDL(ddlID);
            return SqlJobParser.ParseJobScript(ddl).First();
        }

        public static SqlJobInfo GetJobInfo(string jobDDL) => SqlJobParser.ParseJobScript(jobDDL).First();

        public static void ShowForm(SqlJobInfo jobInfo, string title = "")
        {
            var frm = new Form() { Text = string.IsNullOrEmpty(title) ? jobInfo.JobName : title, WindowState = FormWindowState.Maximized };
            var ctrl = new JobInfo() { Dock = DockStyle.Fill };
            frm.Controls.Add(ctrl);
            frm.ApplyTheme();
            frm.Load += (_, _) =>
            {
                ctrl.LoadJobInfo(jobInfo);
            };
            frm.ShowSingleInstance();
        }

        public void ShowForm(string title = "") => ShowForm(this, title);

        public static void ShowCompare(SqlJobInfo info1, SqlJobInfo info2, string title1, string title2, string formTitle)
        {
            var frm = new Form() { Text = formTitle, WindowState = FormWindowState.Maximized };
            var ctrl2 = new JobInfo() { Dock = DockStyle.Fill };
            var ctrl1 = new JobInfo() { Dock = DockStyle.Fill };
            var split = new SplitContainer() { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(ctrl1);
            split.Panel2.Controls.Add(ctrl2);
            frm.Controls.Add(split);
            split.SplitterDistance = frm.Width / 2;
            frm.ApplyTheme();
            frm.Load += (_, _) =>
            {
                ctrl1.LoadJobInfo(info1);
                ctrl2.LoadJobInfo(info2);
                ctrl1.CustomTitle = title1;
                ctrl2.CustomTitle = title2;
                Common.HighlightGridDifferences(ctrl1.InfoGrid, ctrl2.InfoGrid);
                Common.HighlightGridDifferences(ctrl1.StepsGrid, ctrl2.StepsGrid);
                Common.HighlightGridDifferences(ctrl1.ScheduleGrid, ctrl2.ScheduleGrid);
            };
            frm.ShowSingleInstance();
        }
    }
}