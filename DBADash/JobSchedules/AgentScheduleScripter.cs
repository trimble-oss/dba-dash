using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBADash.JobSchedules
{
    /// <summary>A schedule to move: as it is, as proposed, and the jobs that use it</summary>
    public sealed record ScheduleChange(AgentSchedule Original, AgentSchedule Proposed, IReadOnlyList<string> JobNames)
    {
        /// <summary>
        /// The jobs that keep the schedule (job_id).  When given, the script refuses to move it while any other job uses it,
        /// as that job would move too.  Jobs leaving it for a copy (<see cref="ScheduleSplit"/>) don't need listing.
        /// </summary>
        public IReadOnlyList<Guid> JobIds { get; init; } = Array.Empty<Guid>();
    }

    /// <summary>
    /// A job leaving a schedule it shares with other jobs for a copy of its own, so it can start at a different time:
    /// the copy has <paramref name="Proposed"/>'s times and the other jobs keep <paramref name="Original"/>.
    /// </summary>
    /// <param name="NewScheduleUid">The copy's schedule_uid, chosen up front so rolling back can find it</param>
    public sealed record ScheduleSplit(AgentSchedule Original, AgentSchedule Proposed, Guid JobId, string JobName, Guid NewScheduleUid)
    {
        public string NewName
        {
            get
            {
                var name = $"{Original.Name} ({JobName})";
                return name.Length <= 128 ? name : name[..128];
            }
        }
    }

    /// <param name="Script">Makes the changes - or, with @Rollback set to 1, puts them back</param>
    public sealed record ScheduleScript(string Script, IReadOnlyList<string> Skipped, int Changes);

    /// <summary>
    /// T-SQL to make schedule moves, and to undo them: one script, with @Rollback choosing which.
    ///
    /// <para>Each schedule is found by schedule_uid and updated with sp_update_schedule, which changes it for every job
    /// that uses it.  A job that leaves a shared schedule gets a copy of it (sp_add_schedule with a schedule_uid chosen
    /// here), attached in place of the shared one - rolling back attaches the shared one again and deletes the copy.</para>
    /// <para>Before anything is changed the script checks every schedule and job is still as DBA Dash last saw it (applying)
    /// or as applying left it (rolling back), and changes nothing if one isn't - so it doesn't overwrite a change made in
    /// the meantime, running it twice does nothing the second time, and rolling back changes that weren't made does
    /// nothing.  The changes run in one transaction, rolled back if any msdb procedure reports a failure (they RAISERROR
    /// and return non-zero, which XACT_ABORT doesn't catch).</para>
    /// <para>RAISERROR rather than THROW, so the script runs on SQL Server 2008 too.</para>
    /// </summary>
    public static class AgentScheduleScripter
    {
        public static ScheduleScript Build(IEnumerable<ScheduleChange> changes, string instanceName, DateTime generatedUtc) =>
            Build(changes, Array.Empty<ScheduleSplit>(), instanceName, generatedUtc);

        /// <param name="changes">The schedules to move.  Ones whose times don't change are left out.</param>
        /// <param name="splits">Jobs to give a copy of a shared schedule of their own</param>
        /// <param name="instanceName">The instance the script is for, named in the header</param>
        /// <param name="generatedUtc">When the script was made</param>
        public static ScheduleScript Build(IEnumerable<ScheduleChange> changes, IEnumerable<ScheduleSplit> splits, string instanceName, DateTime generatedUtc)
        {
            var skipped = new List<string>();
            var list = new List<ScheduleChange>();
            foreach (var c in changes)
            {
                if (c.Original.ActiveStartTime == c.Proposed.ActiveStartTime && c.Original.ActiveEndTime == c.Proposed.ActiveEndTime) continue;
                if (c.Original.Uid is null)
                {
                    skipped.Add($"{c.Original.Name} ({string.Join(", ", c.JobNames)}): no schedule_uid to find it by");
                    continue;
                }
                list.Add(c);
            }
            var splitList = new List<ScheduleSplit>();
            foreach (var s in splits)
            {
                if (s.Original.Uid is null)
                {
                    skipped.Add($"{s.Original.Name} ({s.JobName}): no schedule_uid to find it by");
                    continue;
                }
                splitList.Add(s);
            }
            return new ScheduleScript(Script(list, splitList, instanceName, generatedUtc, skipped), skipped, list.Count + splitList.Count);
        }

        private static string Script(List<ScheduleChange> changes, List<ScheduleSplit> splits, string instanceName, DateTime generatedUtc, List<string> skipped)
        {
            var sb = new StringBuilder();
            var what = Describe(changes.Count, splits.Count);
            sb.AppendLine("/*");
            sb.AppendLine($"\tOn {Comment(instanceName)}, as planned in DBA Dash (job timeline, Plan): {what}.");
            sb.AppendLine($"\tGenerated {generatedUtc:yyyy-MM-dd HH:mm:ss} UTC.  Run on {Comment(instanceName)}.");
            if (changes.Count > 0 || splits.Count > 0)
            {
                sb.AppendLine("\tRun it as it is to make the changes.  Keep it: run it again with @Rollback = 1 to put the schedules back as DBA Dash collected them.");
                sb.AppendLine("\tChecks each schedule and job is still as expected first - as collected, or as the changes left them when rolling back - and changes nothing if one isn't.");
                sb.AppendLine("\tA schedule shared by several jobs moves for all of them, so the script refuses to move one that a job it doesn't list uses.");
            }
            if (splits.Count > 0)
            {
                sb.AppendLine("\tA job given its own copy of a shared schedule has the copy attached in place of the shared one, which the other jobs keep.  Rolling back attaches the shared one again and deletes the copy.");
            }
            sb.AppendLine("\tTimes are server time (hhmmss).");
            foreach (var s in skipped) sb.AppendLine($"\tNot scripted: {Comment(s)}");
            sb.AppendLine("*/");
            if (changes.Count == 0 && splits.Count == 0)
            {
                sb.AppendLine("PRINT 'No schedule changes.'");
                return sb.ToString();
            }
            sb.AppendLine("DECLARE @Rollback BIT = 0 /* Change to 1 to undo the changes */");
            sb.AppendLine();
            sb.AppendLine("SET NOCOUNT ON");
            sb.AppendLine("SET XACT_ABORT ON");
            sb.AppendLine();

            // What each schedule should look like now, and the jobs that should use it
            sb.AppendLine("DECLARE @Schedules TABLE(");
            sb.AppendLine("\tschedule_uid UNIQUEIDENTIFIER PRIMARY KEY,");
            sb.AppendLine("\tname SYSNAME NOT NULL,");
            sb.AppendLine("\tfreq_type INT NOT NULL,");
            sb.AppendLine("\tfreq_interval INT NOT NULL,");
            sb.AppendLine("\tfreq_subday_type INT NOT NULL,");
            sb.AppendLine("\tfreq_subday_interval INT NOT NULL,");
            sb.AppendLine("\tfreq_relative_interval INT NOT NULL,");
            sb.AppendLine("\tfreq_recurrence_factor INT NOT NULL,");
            sb.AppendLine("\tactive_start_date INT NOT NULL,");
            sb.AppendLine("\tactive_end_date INT NOT NULL,");
            sb.AppendLine("\tenabled INT NOT NULL,");
            sb.AppendLine("\texpected_start_time INT NOT NULL,");
            sb.AppendLine("\texpected_end_time INT NOT NULL,");
            sb.AppendLine("\tnew_start_time INT NOT NULL,");
            sb.AppendLine("\tnew_end_time INT NOT NULL");
            sb.AppendLine(")");
            sb.AppendLine("DECLARE @ScheduleJobs TABLE(schedule_uid UNIQUEIDENTIFIER NOT NULL, job_id UNIQUEIDENTIFIER NOT NULL, PRIMARY KEY(schedule_uid, job_id))");
            sb.AppendLine("DECLARE @Splits TABLE(");
            sb.AppendLine("\tjob_id UNIQUEIDENTIFIER NOT NULL,");
            sb.AppendLine("\tjob_name SYSNAME NOT NULL,");
            sb.AppendLine("\tschedule_uid UNIQUEIDENTIFIER NOT NULL,");
            sb.AppendLine("\tschedule_name SYSNAME NOT NULL,");
            sb.AppendLine("\tnew_schedule_uid UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,");
            sb.AppendLine("\tnew_name SYSNAME NOT NULL,");
            sb.AppendLine("\tfreq_type INT NOT NULL,");
            sb.AppendLine("\tfreq_interval INT NOT NULL,");
            sb.AppendLine("\tfreq_subday_type INT NOT NULL,");
            sb.AppendLine("\tfreq_subday_interval INT NOT NULL,");
            sb.AppendLine("\tfreq_relative_interval INT NOT NULL,");
            sb.AppendLine("\tfreq_recurrence_factor INT NOT NULL,");
            sb.AppendLine("\tactive_start_date INT NOT NULL,");
            sb.AppendLine("\tactive_end_date INT NOT NULL,");
            sb.AppendLine("\tenabled INT NOT NULL,");
            sb.AppendLine("\texpected_start_time INT NOT NULL,");
            sb.AppendLine("\texpected_end_time INT NOT NULL,");
            sb.AppendLine("\tnew_start_time INT NOT NULL,");
            sb.AppendLine("\tnew_end_time INT NOT NULL");
            sb.AppendLine(")");
            sb.AppendLine();
            foreach (var c in changes)
            {
                var uid = c.Original.Uid.Value;
                sb.AppendLine($"/* {Comment(string.Join(", ", c.JobNames))}: {Time(c.Original.ActiveStartTime)} -> {Time(c.Proposed.ActiveStartTime)} */");
                sb.AppendLine("INSERT INTO @Schedules VALUES(" +
                              $"'{uid}', N'{Literal(c.Original.Name)}', {c.Original.FrequencyType}, {c.Original.FrequencyInterval}, " +
                              $"{c.Original.FrequencySubdayType}, {c.Original.FrequencySubdayInterval}, " +
                              $"{c.Original.FrequencyRelativeInterval}, {c.Original.FrequencyRecurrenceFactor}, {c.Original.ActiveStartDate}, {c.Original.ActiveEndDate}, {(c.Original.Enabled ? 1 : 0)}, " +
                              $"{c.Original.ActiveStartTime}, {c.Original.ActiveEndTime}, {c.Proposed.ActiveStartTime}, {c.Proposed.ActiveEndTime})");
                foreach (var jobId in c.JobIds.Distinct()) sb.AppendLine($"INSERT INTO @ScheduleJobs VALUES('{uid}', '{jobId}')");
            }
            foreach (var s in splits)
            {
                sb.AppendLine($"/* {Comment(s.JobName)}: own copy of {Comment(s.Original.Name)}, {Time(s.Original.ActiveStartTime)} -> {Time(s.Proposed.ActiveStartTime)} */");
                sb.AppendLine("INSERT INTO @Splits VALUES(" +
                              $"'{s.JobId}', N'{Literal(s.JobName)}', '{s.Original.Uid.Value}', N'{Literal(s.Original.Name)}', '{s.NewScheduleUid}', N'{Literal(s.NewName)}', " +
                              $"{s.Original.FrequencyType}, {s.Original.FrequencyInterval}, {s.Original.FrequencySubdayType}, {s.Original.FrequencySubdayInterval}, " +
                              $"{s.Original.FrequencyRelativeInterval}, {s.Original.FrequencyRecurrenceFactor}, {s.Original.ActiveStartDate}, {s.Original.ActiveEndDate}, {(s.Original.Enabled ? 1 : 0)}, " +
                              $"{s.Original.ActiveStartTime}, {s.Original.ActiveEndTime}, {s.Proposed.ActiveStartTime}, {s.Proposed.ActiveEndTime})");
            }
            if (changes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("/* Rolling back: the schedules should be at the new times, and go back to the original ones */");
                sb.AppendLine("IF @Rollback = 1");
                sb.AppendLine("\tUPDATE @Schedules SET expected_start_time = new_start_time, expected_end_time = new_end_time, new_start_time = expected_start_time, new_end_time = expected_end_time");
            }
            sb.AppendLine();

            sb.AppendLine("/* Everything must be as expected before anything is changed */");
            sb.AppendLine("DECLARE @Problems NVARCHAR(MAX)");
            sb.AppendLine("SELECT @Problems = ISNULL(@Problems + NCHAR(13) + NCHAR(10), N'') + Problem");
            sb.AppendLine("FROM (");
            sb.AppendLine("\tSELECT S.name +");
            sb.AppendLine("\t\tCASE WHEN SS.schedule_id IS NULL THEN N': not found'");
            sb.AppendLine("\t\tWHEN SS.active_start_time <> S.expected_start_time OR SS.active_end_time <> S.expected_end_time");
            sb.AppendLine("\t\t\tTHEN N': starts ' + CAST(SS.active_start_time AS NVARCHAR(10)) + N', expected ' + CAST(S.expected_start_time AS NVARCHAR(10)) +");
            sb.AppendLine("\t\t\tCASE WHEN @Rollback = 1 THEN N' - have the changes been made?' ELSE N' - changed since this script was generated, or already run?' END");
            sb.AppendLine("\t\tELSE N': how often or when it runs has changed since this script was generated' END AS Problem");
            sb.AppendLine("\tFROM @Schedules S");
            sb.AppendLine("\tLEFT JOIN msdb.dbo.sysschedules SS ON SS.schedule_uid = S.schedule_uid");
            sb.AppendLine("\tWHERE SS.schedule_id IS NULL");
            sb.AppendLine("\tOR SS.active_start_time <> S.expected_start_time");
            sb.AppendLine("\tOR SS.active_end_time <> S.expected_end_time");
            sb.AppendLine("\tOR SS.freq_type <> S.freq_type");
            sb.AppendLine("\tOR SS.freq_interval <> S.freq_interval");
            sb.AppendLine("\tOR SS.freq_subday_type <> S.freq_subday_type");
            sb.AppendLine("\tOR SS.freq_subday_interval <> S.freq_subday_interval");
            sb.AppendLine("\tOR SS.freq_relative_interval <> S.freq_relative_interval");
            sb.AppendLine("\tOR SS.freq_recurrence_factor <> S.freq_recurrence_factor");
            sb.AppendLine("\tOR SS.active_start_date <> S.active_start_date");
            sb.AppendLine("\tOR SS.active_end_date <> S.active_end_date");
            sb.AppendLine("\tOR SS.enabled <> S.enabled");
            sb.AppendLine("\tUNION ALL");
            sb.AppendLine("\tSELECT S.name + N': also used by job ' + J.name + N', which would move too'");
            sb.AppendLine("\tFROM @Schedules S");
            sb.AppendLine("\tJOIN msdb.dbo.sysschedules SS ON SS.schedule_uid = S.schedule_uid");
            sb.AppendLine("\tJOIN msdb.dbo.sysjobschedules JS ON JS.schedule_id = SS.schedule_id");
            sb.AppendLine("\tJOIN msdb.dbo.sysjobs J ON J.job_id = JS.job_id");
            sb.AppendLine("\tWHERE EXISTS(SELECT 1 FROM @ScheduleJobs SJ WHERE SJ.schedule_uid = S.schedule_uid)");
            sb.AppendLine("\tAND NOT EXISTS(SELECT 1 FROM @ScheduleJobs SJ WHERE SJ.schedule_uid = S.schedule_uid AND SJ.job_id = JS.job_id)");
            sb.AppendLine("\tAND NOT EXISTS(SELECT 1 FROM @Splits SP WHERE SP.schedule_uid = S.schedule_uid AND SP.job_id = JS.job_id)");
            if (splits.Count > 0)
            {
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\tSELECT SP.job_name + N': job not found'");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tWHERE NOT EXISTS(SELECT 1 FROM msdb.dbo.sysjobs J WHERE J.job_id = SP.job_id)");
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\tSELECT SP.schedule_name + N': not found'");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tWHERE NOT EXISTS(SELECT 1 FROM msdb.dbo.sysschedules SS WHERE SS.schedule_uid = SP.schedule_uid)");
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\t/* The copy is made from the shared schedule as collected, so it must still be that */");
                sb.AppendLine("\tSELECT SP.schedule_name + N': changed since this script was generated'");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tJOIN msdb.dbo.sysschedules SS ON SS.schedule_uid = SP.schedule_uid");
                sb.AppendLine("\tWHERE SS.freq_type <> SP.freq_type");
                sb.AppendLine("\tOR SS.freq_interval <> SP.freq_interval");
                sb.AppendLine("\tOR SS.freq_subday_type <> SP.freq_subday_type");
                sb.AppendLine("\tOR SS.freq_subday_interval <> SP.freq_subday_interval");
                sb.AppendLine("\tOR SS.freq_relative_interval <> SP.freq_relative_interval");
                sb.AppendLine("\tOR SS.freq_recurrence_factor <> SP.freq_recurrence_factor");
                sb.AppendLine("\tOR SS.active_start_date <> SP.active_start_date");
                sb.AppendLine("\tOR SS.active_end_date <> SP.active_end_date");
                sb.AppendLine("\tOR SS.enabled <> SP.enabled");
                sb.AppendLine("\t/* Its times too - unless this script moves it, when they're checked with the moves */");
                sb.AppendLine("\tOR (NOT EXISTS(SELECT 1 FROM @Schedules S WHERE S.schedule_uid = SP.schedule_uid)");
                sb.AppendLine("\t\tAND (SS.active_start_time <> SP.expected_start_time OR SS.active_end_time <> SP.expected_end_time))");
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\t/* The job uses the shared schedule before the changes, and not after */");
                sb.AppendLine("\tSELECT SP.job_name + CASE WHEN @Rollback = 1 THEN N': already uses ' ELSE N': no longer uses ' END + SP.schedule_name");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tJOIN msdb.dbo.sysschedules SS ON SS.schedule_uid = SP.schedule_uid");
                sb.AppendLine("\tWHERE @Rollback = CASE WHEN EXISTS(SELECT 1 FROM msdb.dbo.sysjobschedules JS WHERE JS.job_id = SP.job_id AND JS.schedule_id = SS.schedule_id) THEN 1 ELSE 0 END");
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\t/* Applying: the copy mustn't exist yet */");
                sb.AppendLine("\tSELECT SP.new_name + N': already exists - has this script already been run?'");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tWHERE @Rollback = 0");
                sb.AppendLine("\tAND EXISTS(SELECT 1 FROM msdb.dbo.sysschedules SS WHERE SS.schedule_uid = SP.new_schedule_uid)");
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\t/* Rolling back: the copy must be as the changes left it, and used by the job */");
                sb.AppendLine("\tSELECT SP.new_name + CASE WHEN SS.schedule_id IS NULL THEN N': not found - have the changes been made?' ELSE N': changed since the changes were made' END");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tLEFT JOIN msdb.dbo.sysschedules SS ON SS.schedule_uid = SP.new_schedule_uid");
                sb.AppendLine("\tWHERE @Rollback = 1");
                sb.AppendLine("\tAND (SS.schedule_id IS NULL");
                sb.AppendLine("\t\tOR SS.active_start_time <> SP.new_start_time");
                sb.AppendLine("\t\tOR SS.active_end_time <> SP.new_end_time");
                sb.AppendLine("\t\tOR SS.freq_type <> SP.freq_type");
                sb.AppendLine("\t\tOR SS.freq_interval <> SP.freq_interval");
                sb.AppendLine("\t\tOR SS.freq_subday_type <> SP.freq_subday_type");
                sb.AppendLine("\t\tOR SS.freq_subday_interval <> SP.freq_subday_interval");
                sb.AppendLine("\t\tOR SS.freq_relative_interval <> SP.freq_relative_interval");
                sb.AppendLine("\t\tOR SS.freq_recurrence_factor <> SP.freq_recurrence_factor");
                sb.AppendLine("\t\tOR SS.active_start_date <> SP.active_start_date");
                sb.AppendLine("\t\tOR SS.active_end_date <> SP.active_end_date");
                sb.AppendLine("\t\tOR SS.enabled <> SP.enabled)");
                sb.AppendLine("\tUNION ALL");
                sb.AppendLine("\tSELECT SP.job_name + N': no longer uses ' + SP.new_name");
                sb.AppendLine("\tFROM @Splits SP");
                sb.AppendLine("\tJOIN msdb.dbo.sysschedules SS ON SS.schedule_uid = SP.new_schedule_uid");
                sb.AppendLine("\tWHERE @Rollback = 1");
                sb.AppendLine("\tAND NOT EXISTS(SELECT 1 FROM msdb.dbo.sysjobschedules JS WHERE JS.job_id = SP.job_id AND JS.schedule_id = SS.schedule_id)");
            }
            sb.AppendLine(") P");
            sb.AppendLine();
            sb.AppendLine("IF @Problems IS NOT NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine("\tPRINT @Problems");
            sb.AppendLine("\tRAISERROR('Nothing was changed: schedules are not as expected (see messages).', 16, 1)");
            sb.AppendLine("\tRETURN");
            sb.AppendLine("END");
            sb.AppendLine();

            sb.AppendLine("DECLARE @rc INT, @schedule_id INT, @new_schedule_id INT, @owner SYSNAME");
            sb.AppendLine("BEGIN TRANSACTION");
            sb.AppendLine("IF @Rollback = 0");
            sb.AppendLine("BEGIN");
            AppendIndented(sb, apply =>
            {
                foreach (var s in splits) AppendSplit(apply, s, rollback: false);
                AppendUpdates(apply, changes, rollback: false);
            });
            sb.AppendLine("END");
            sb.AppendLine("ELSE");
            sb.AppendLine("BEGIN");
            // Rolling back puts the shared schedules back before the jobs return to them
            AppendIndented(sb, undo =>
            {
                AppendUpdates(undo, changes, rollback: true);
                foreach (var s in splits) AppendSplit(undo, s, rollback: true);
            });
            sb.AppendLine("END");
            sb.AppendLine("COMMIT");
            sb.AppendLine($"IF @Rollback = 0 PRINT 'Done: {Literal(what)}.' ELSE PRINT 'Rolled back: {Literal(what)}.'");
            sb.AppendLine("RETURN");
            sb.AppendLine();
            sb.AppendLine("Failed:");
            sb.AppendLine("IF @@TRANCOUNT > 0 ROLLBACK");
            sb.AppendLine("RAISERROR('Nothing was changed: a step failed (see messages).', 16, 1)");
            return sb.ToString();
        }

        /// <summary>What <paramref name="write"/> appends, a tab in - for inside a BEGIN/END</summary>
        private static void AppendIndented(StringBuilder sb, Action<StringBuilder> write)
        {
            var inner = new StringBuilder();
            write(inner);
            var lines = inner.ToString().Split(Environment.NewLine);
            // Each step starts with a blank line; drop the first, and the split's trailing empty string
            var first = lines.Length > 0 && lines[0].Length == 0 ? 1 : 0;
            var last = lines.Length > 0 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;
            for (var i = first; i < last; i++) sb.AppendLine(lines[i].Length == 0 ? string.Empty : "\t" + lines[i]);
        }

        private static void AppendUpdates(StringBuilder sb, List<ScheduleChange> changes, bool rollback)
        {
            foreach (var c in changes)
            {
                var to = rollback ? c.Original : c.Proposed;
                sb.AppendLine();
                sb.AppendLine($"/* {Comment(c.Original.Name)} */");
                AppendLookup(sb, "@schedule_id", c.Original.Uid.Value, c.Original.Name);
                // A move past midnight changes its days too
                var days = SameDays(c.Original, c.Proposed) ? string.Empty
                    : $", @freq_interval = {to.FrequencyInterval}, @active_start_date = {to.ActiveStartDate}, @active_end_date = {to.ActiveEndDate}";
                sb.AppendLine($"EXEC @rc = msdb.dbo.sp_update_schedule @schedule_id = @schedule_id, @active_start_time = {to.ActiveStartTime}, @active_end_time = {to.ActiveEndTime}{days}");
                sb.AppendLine("IF @rc <> 0 GOTO Failed");
            }
        }

        private static bool SameDays(AgentSchedule a, AgentSchedule b) =>
            a.FrequencyInterval == b.FrequencyInterval && a.ActiveStartDate == b.ActiveStartDate && a.ActiveEndDate == b.ActiveEndDate;

        private static void AppendSplit(StringBuilder sb, ScheduleSplit s, bool rollback)
        {
            var uid = s.Original.Uid.Value;
            sb.AppendLine();
            sb.AppendLine(rollback
                ? $"/* {Comment(s.JobName)}: back to {Comment(s.Original.Name)} */"
                : $"/* {Comment(s.JobName)}: own copy of {Comment(s.Original.Name)} */");
            sb.AppendLine("SET @owner = NULL");
            AppendLookup(sb, "@schedule_id", uid, s.Original.Name, ", @owner = SUSER_SNAME(owner_sid)");
            if (rollback)
            {
                AppendLookup(sb, "@new_schedule_id", s.NewScheduleUid, s.NewName);
                sb.AppendLine($"EXEC @rc = msdb.dbo.sp_attach_schedule @job_id = '{s.JobId}', @schedule_id = @schedule_id");
                sb.AppendLine("IF @rc <> 0 GOTO Failed");
                sb.AppendLine($"EXEC @rc = msdb.dbo.sp_detach_schedule @job_id = '{s.JobId}', @schedule_id = @new_schedule_id, @delete_unused_schedule = 1");
                sb.AppendLine("IF @rc <> 0 GOTO Failed");
                return;
            }
            var o = s.Original;
            sb.AppendLine("SET @new_schedule_id = NULL");
            sb.AppendLine($"EXEC @rc = msdb.dbo.sp_add_schedule @schedule_name = N'{Literal(s.NewName)}', @enabled = {(o.Enabled ? 1 : 0)}, " +
                          $"@freq_type = {o.FrequencyType}, @freq_interval = {s.Proposed.FrequencyInterval}, @freq_subday_type = {o.FrequencySubdayType}, @freq_subday_interval = {o.FrequencySubdayInterval}, " +
                          $"@freq_relative_interval = {o.FrequencyRelativeInterval}, @freq_recurrence_factor = {o.FrequencyRecurrenceFactor}, " +
                          $"@active_start_date = {s.Proposed.ActiveStartDate}, @active_end_date = {s.Proposed.ActiveEndDate}, " +
                          $"@active_start_time = {s.Proposed.ActiveStartTime}, @active_end_time = {s.Proposed.ActiveEndTime}, " +
                          $"@owner_login_name = @owner, @schedule_uid = '{s.NewScheduleUid}', @schedule_id = @new_schedule_id OUTPUT");
            sb.AppendLine("IF @rc <> 0 GOTO Failed");
            sb.AppendLine($"EXEC @rc = msdb.dbo.sp_attach_schedule @job_id = '{s.JobId}', @schedule_id = @new_schedule_id");
            sb.AppendLine("IF @rc <> 0 GOTO Failed");
            sb.AppendLine($"EXEC @rc = msdb.dbo.sp_detach_schedule @job_id = '{s.JobId}', @schedule_id = @schedule_id");
            sb.AppendLine("IF @rc <> 0 GOTO Failed");
        }

        /// <summary>
        /// Sets <paramref name="variable"/> to the schedule's schedule_id, and fails when it's gone - a SELECT that finds no row
        /// leaves the variable as it was, which would be the previous step's schedule
        /// </summary>
        private static void AppendLookup(StringBuilder sb, string variable, Guid uid, string name, string alsoSelect = "")
        {
            sb.AppendLine($"SET {variable} = NULL");
            sb.AppendLine($"SELECT {variable} = schedule_id{alsoSelect} FROM msdb.dbo.sysschedules WHERE schedule_uid = '{uid}'");
            sb.AppendLine($"IF {variable} IS NULL BEGIN PRINT N'{Literal(name)}: not found'; GOTO Failed; END");
        }

        private static string Describe(int moves, int splits)
        {
            var parts = new List<string>();
            if (moves > 0) parts.Add(moves == 1 ? "1 schedule moved" : $"{moves} schedules moved");
            if (splits > 0) parts.Add(splits == 1 ? "1 job given its own schedule" : $"{splits} jobs given their own schedule");
            return parts.Count == 0 ? "no changes" : string.Join(", ", parts);
        }

        private static string Time(int hhmmss) => $"{hhmmss / 10000:00}:{hhmmss / 100 % 100:00}:{hhmmss % 100:00}";

        private static string Literal(string value) => (value ?? string.Empty).Replace("'", "''");

        /// <summary>Safe inside a /* */ comment</summary>
        private static string Comment(string value) => (value ?? string.Empty).Replace("*/", "* /").Replace("/*", "/ *").Replace("\r", " ").Replace("\n", " ");
    }
}
