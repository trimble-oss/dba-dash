using DBADashGUI.DBADashAlerts;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.DailyReport
{
    /// <summary>
    /// Builds and sends daily reports.  Used by the service on schedule and by the GUI's "send test" button.
    /// </summary>
    public static class DailyReportRunner
    {
        /// <summary>A built report, and the issues in it - recorded as the last run's once it's been sent</summary>
        public class BuiltReport
        {
            public DailyReportContent Content { get; init; }
            public List<ReportIssue> Issues { get; init; }
            /// <summary>Keys recording the run mustn't remove: the alerts couldn't be read, so the ones recorded aren't known to have gone</summary>
            public string KeepKeyPrefix { get; init; }
        }

        /// <summary>
        /// Loads the data for the report and renders it.  Nothing is recorded: the issues are compared with the last scheduled run but not
        /// saved, so a preview or Send now doesn't change what tomorrow's report calls new.  Throws if no instances are in scope.
        /// </summary>
        public static async Task<BuiltReport> BuildAsync(DailyReportConfig config, string repositoryConnectionString, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            var settings = config.Settings ?? new DailyReportSettings();
            var instanceIDs = DailyReportRepository.GetInstanceIDs(repositoryConnectionString, config);
            if (instanceIDs.Count == 0)
            {
                throw new InvalidOperationException($"No instances in scope for daily report '{config.ReportName}' ({config.ScopeDescription}).");
            }
            var instanceList = string.Join(",", instanceIDs);

            var summary = GetSummary(repositoryConnectionString, instanceList);
            AddDisplayNames(summary, repositoryConnectionString, instanceList);
            var objectIssues = DailyReportRepository.GetObjectIssues(repositoryConnectionString, instanceList);
            var alerts = settings.IncludeAlerts ? GetAlerts(repositoryConnectionString, instanceList, config.ReportName) : null;
            var keepKeyPrefix = settings.IncludeAlerts && alerts == null ? "alert|" : null;

            if (config.ReportType == DailyReportType.Changes)
            {
                // Checks only: no performance (it's judged over whole days), so no top queries, charts or AI summary either
                var changeIssues = DailyReportRenderer.IssueKeys(DailyReportRenderer.Classify(summary, objectIssues), null, alerts);
                // The comparison is the whole report here, so it isn't optional: a failure to read it throws (and the scheduled run retries) rather
                // than looking like a first run, which would record the current issues without sending what changed
                var changeHistory = config.DailyReportID > 0 ? IssueHistoryRepository.Compare(repositoryConnectionString, config.DailyReportID, changeIssues, nowUtc, update: false) : null;
                var changes = DailyReportRenderer.RenderChanges(config, nowUtc, config.ScopeDescription, summary, objectIssues, changeHistory, config.TimeZone, alerts);
                return new BuiltReport { Content = changes, Issues = changeIssues, KeepKeyPrefix = keepKeyPrefix };
            }

            PerformanceAnalysis performance = null;
            Func<int, string> formatHour = null;
            // Performance can cover fewer instances than the checks (e.g. production only) - never more
            var performanceIDs = config.IncludePerformance || config.IncludeWeekOverWeek
                ? DailyReportRepository.GetPerformanceInstanceIDs(repositoryConnectionString, settings, instanceIDs)
                : new List<int>();
            if (performanceIDs.Count == 0 && (config.IncludePerformance || config.IncludeWeekOverWeek))
            {
                Log.Warning("Daily report {ReportName}: no instances in the performance scope ({Scope})", config.ReportName, settings.PerformanceScopeDescription);
            }
            if (performanceIDs.Count > 0)
            {
                var baselineWeeks = config.IncludeWeekOverWeek ? settings.BaselineWeeks : 0;
                // Only certain hours of the day counted (e.g. 09:00-17:00 local), when the report is set to
                var includedHours = PerformanceData.HoursFor(PerformanceData.WindowEndFor(nowUtc), baselineWeeks, settings, config.TimeZone);
                var data = PerformanceDataRepository.Get(repositoryConnectionString, string.Join(",", performanceIDs), nowUtc, baselineWeeks, includedHours);
                formatHour = h => DailyReportRenderer.FormatHour(data.WindowStartUtc.AddHours(h), config.TimeZone);
                performance = PerformanceAnalyzer.Analyze(data, config.IncludePerformance, config.IncludeWeekOverWeek, config.ChangeThresholdPct, settings, formatHour);

                if (settings.IncludeTopQueries && performance.Flagged.Count > 0)
                {
                    AddTopQueries(performance, settings, repositoryConnectionString, data.WindowEndUtc, baselineWeeks, config.ReportName);
                }
                if (settings.IncludeActivity && performance.Flagged.Count > 0)
                {
                    AddActivity(performance, settings, repositoryConnectionString, data.WindowStartUtc, data.WindowEndUtc, config);
                }
            }

            var issues = DailyReportRenderer.IssueKeys(DailyReportRenderer.Classify(summary, objectIssues), performance, alerts);
            var history = config.DailyReportID > 0 ? CompareHistory(config, repositoryConnectionString, issues, nowUtc, update: false) : null;

            DailyReportAiSummary ai = null;
            if (settings.IncludeAISummary)
            {
                // The AI gets the plain text with queries named by hash rather than by their text, which can hold literal values
                var forAi = DailyReportRenderer.Render(config, nowUtc, config.ScopeDescription, summary, objectIssues, performance, config.TimeZone,
                    new DailyReportExtras { History = history, Alerts = alerts, ForAi = true, FormatHour = formatHour });
                ai = await DailyReportAiClient.SummariseAsync(repositoryConnectionString, config.ReportName, forAi.TextBody, cancellationToken);
                if (!ai.Success) Log.Warning("Daily report {ReportName} AI summary not available: {Error}", config.ReportName, ai.Error);
            }

            var content = DailyReportRenderer.Render(config, nowUtc, config.ScopeDescription, summary, objectIssues, performance, config.TimeZone,
                new DailyReportExtras { History = history, Alerts = alerts, AiSummary = ai, FormatHour = formatHour });
            return new BuiltReport { Content = content, Issues = issues, KeepKeyPrefix = keepKeyPrefix };
        }

        /// <summary>Long running requests on each listed instance (DBCC, backups...).  Left out, with a warning, if they can't be read</summary>
        private static void AddActivity(PerformanceAnalysis performance, DailyReportSettings settings, string connectionString, DateTime fromUtc, DateTime toUtc, DailyReportConfig config)
        {
            try
            {
                var listed = performance.Flagged.Take(settings.MaxPerformanceInstances).Select(f => f.InstanceID).ToList();
                var rows = DailyReportActivityRepository.Get(connectionString, listed, fromUtc, toUtc, performance.IncludedHours);
                DailyReportActivityAnalyzer.Apply(performance, rows, utc => DailyReportRenderer.FormatHour(utc, config.TimeZone));
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "Daily report {ReportName}: long running activity not available", config.ReportName);
            }
        }

        /// <summary>The procedures and queries behind each listed instance's findings.  Left out, with a warning, if they can't be read</summary>
        private static void AddTopQueries(PerformanceAnalysis performance, DailyReportSettings settings, string connectionString, DateTime windowEndUtc, int baselineWeeks, string reportName)
        {
            try
            {
                var listed = performance.Flagged.Take(settings.MaxPerformanceInstances).Select(f => f.InstanceID).ToList();
                var rows = TopQueryRepository.Get(connectionString, listed, windowEndUtc, baselineWeeks, performance.IncludedHours);
                TopQueryAnalyzer.Apply(performance, rows, settings.TopQueriesCount);
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "Daily report {ReportName}: top queries not available", reportName);
            }
        }

        /// <summary>Open alerts for the report, or null (no alerts section) if they can't be read - an optional part never fails the report</summary>
        private static List<DailyReportAlert> GetAlerts(string connectionString, string instanceList, string reportName)
        {
            try
            {
                return DailyReportAlertRepository.Get(connectionString, instanceList);
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "Daily report {ReportName}: alerts not available", reportName);
                return null;
            }
        }

        /// <summary>The comparison with the last scheduled run, or null (nothing marked new) if it can't be read</summary>
        private static IssueHistory CompareHistory(DailyReportConfig config, string connectionString, List<ReportIssue> issues, DateTime nowUtc, bool update, string keepKeyPrefix = null)
        {
            try
            {
                return IssueHistoryRepository.Compare(connectionString, config.DailyReportID, issues, nowUtc, update, keepKeyPrefix);
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "Daily report {ReportName}: issue history not {Action}", config.ReportName, update ? "recorded" : "available");
                return null;
            }
        }

        /// <summary>What happened on one channel: the address it went to and the mail server's reply, or why it wasn't sent</summary>
        public class ChannelSendResult
        {
            public string ChannelName { get; init; }
            public string Recipient { get; init; }
            public bool Sent { get; init; }
            /// <summary>The server's reply when sent (e.g. "2.0.0 OK queued as 1A2B"), otherwise the reason it wasn't</summary>
            public string Detail { get; init; }

            public override string ToString() => Sent
                ? $"{ChannelName}: accepted by the mail server for {Recipient}{(string.IsNullOrWhiteSpace(Detail) ? string.Empty : $" ({Detail.Trim()})")}"
                : $"{ChannelName}: not sent - {Detail}";
        }

        public class SendResult
        {
            public List<ChannelSendResult> Channels { get; } = new();
            public bool SentToAll => Channels.Count > 0 && Channels.All(c => c.Sent);
            public bool SentToAny => Channels.Any(c => c.Sent);
            public List<ChannelSendResult> Failures => Channels.Where(c => !c.Sent).ToList();

            /// <summary>One line per channel, as recorded on the report (LastSendResult)</summary>
            public override string ToString() => Channels.Count == 0
                ? "The report has no notification channels."
                : string.Join(Environment.NewLine, Channels);
        }

        /// <summary>
        /// Sends the content to every channel of the report.  Never throws: each channel's outcome is in the result, with the mail server's
        /// reply for the ones that were sent, so "the server accepted it" can be told apart from "it never left".
        /// </summary>
        public static async Task<SendResult> SendAsync(DailyReportConfig config, DailyReportContent content, string repositoryConnectionString)
        {
            var result = new SendResult();
            foreach (var channelRef in config.Channels)
            {
                try
                {
                    // Loaded now rather than trusting the report's copy: building the report can take minutes, and a disable window can start or end meanwhile
                    var channel = NotificationChannelBase.GetChannel(channelRef.NotificationChannelID, repositoryConnectionString);
                    if (IsInDisableWindow(channel.DisableFrom, channel.DisableTo, DateTime.UtcNow))
                    {
                        result.Channels.Add(new ChannelSendResult { ChannelName = channelRef.ChannelName, Detail = "the channel is in its disable window (Disable From / Disable To)" });
                        continue;
                    }
                    if (channel is not DBADash.Alert.EmailNotificationChannel email)
                    {
                        result.Channels.Add(new ChannelSendResult { ChannelName = channelRef.ChannelName, Detail = $"{channelRef.ChannelType} channels can't receive daily reports yet - use an Email channel" });
                        continue;
                    }

                    // Plain text only: no HTML part and no images, so every client shows the text as it is
                    var reply = await email.SendDailyReportAsync(content.Subject, content.PlainTextOnly ? null : content.HtmlBody, content.TextBody,
                        content.PlainTextOnly ? null : content.Images.Select(i => (i.ContentId, i.Png)));
                    Log.Information("Daily report {ReportName} sent to channel {ChannelName} ({Recipient}). Server reply: {Reply}",
                        config.ReportName, channelRef.ChannelName, email.ToEmail, reply);
                    result.Channels.Add(new ChannelSendResult { ChannelName = channelRef.ChannelName, Recipient = email.ToEmail, Sent = true, Detail = reply });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Daily report {ReportName} failed for channel {ChannelName}", config.ReportName, channelRef.ChannelName);
                    result.Channels.Add(new ChannelSendResult { ChannelName = channelRef.ChannelName, Detail = ex.Message });
                }
            }
            return result;
        }

        /// <summary>The same rule as alert notifications (Alert.Notifications_Get): disabled from DisableFrom (UTC) until DisableTo, or indefinitely</summary>
        public static bool IsInDisableWindow(DateTime? disableFrom, DateTime? disableTo, DateTime nowUtc) =>
            disableFrom <= nowUtc && (disableTo == null || disableTo >= nowUtc);

        /// <summary>
        /// Send now from the GUI: build, send and record the send (LastSentUTC, LastSendResult) - but not as the scheduled run, so today's
        /// scheduled report still goes out.  A full report's issue history isn't updated, so tomorrow's report still compares with the last
        /// scheduled one.  A changes report's is (see <see cref="RecordsHistoryOnSendNow"/>).
        /// </summary>
        public static async Task<(DailyReportContent Content, SendResult Result)> SendNowAsync(DailyReportConfig config, string repositoryConnectionString, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            // A Send now that records history (a changes report) takes the report's run lock, as a scheduled run does, so the two never overlap -
            // both build from the history and update it.  The scheduled run waits for the next check rather than being lost
            DateTime? lockUtc = null;
            if (RecordsHistoryOnSendNow(config))
            {
                lockUtc = DailyReportRepository.AcquireRunLock(repositoryConnectionString, config.DailyReportID)
                          ?? throw new InvalidOperationException("This report is being sent by the service right now.  Try again in a few minutes.");
            }
            try
            {
                var built = await BuildAsync(config, repositoryConnectionString, nowUtc, cancellationToken);
                var result = await SendAsync(config, built.Content, repositoryConnectionString);
                RecordSend(config, repositoryConnectionString, result);
                if (result.SentToAny && RecordsHistoryOnSendNow(config))
                {
                    CompareHistory(config, repositoryConnectionString, built.Issues, nowUtc, update: true, built.KeepKeyPrefix);
                }
                return (built.Content, result);
            }
            finally
            {
                if (lockUtc.HasValue)
                {
                    try
                    {
                        DailyReportRepository.ReleaseRunLock(repositoryConnectionString, config.DailyReportID, lockUtc.Value);
                    }
                    catch (SqlException ex)
                    {
                        // Not fatal: the lock goes stale after an hour.  Don't hide the send's own result or error
                        Log.Warning(ex, "Daily report {ReportName}: run lock not released", config.ReportName);
                    }
                }
            }
        }

        /// <summary>
        /// A changes report lists what changed since its last send, and a Send now is a send to the same recipients - so it becomes the point
        /// the next one compares with.  Otherwise sending it by hand would show the same changes every time (or "first run" until the
        /// first scheduled send).  A full report's history is what tomorrow's report marks as new, so a Send now leaves it alone.
        /// </summary>
        public static bool RecordsHistoryOnSendNow(DailyReportConfig config) => config.ReportType == DailyReportType.Changes && config.DailyReportID > 0;

        /// <summary>
        /// Scheduled run: build, send and record the result on the report.  Once the report has reached at least one channel its issues are
        /// recorded as the last run's, so the next report can say what's new.  A changes report with nothing to say isn't sent, but its issues
        /// are recorded - on its first run that's the starting point it compares with.  Returns the error text, or null on success.
        /// </summary>
        public static async Task<string> RunScheduledAsync(DailyReportConfig config, string repositoryConnectionString, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            string error;
            // Retry only when nothing was sent - a retry goes to every channel, so one that reached some recipients isn't repeated
            bool retry;
            try
            {
                var built = await BuildAsync(config, repositoryConnectionString, nowUtc, cancellationToken);
                if (built.Content.NothingToSend)
                {
                    // Recording where things stand is all this run does, so it isn't optional: if it fails, the run fails and is retried.  Swallowing
                    // it would leave a first run without its baseline, and the next run would quietly take its place
                    Log.Information("Daily report {ReportName}: no changes to send", config.ReportName);
                    IssueHistoryRepository.Compare(repositoryConnectionString, config.DailyReportID, built.Issues, nowUtc, update: true, built.KeepKeyPrefix);
                    error = null;
                    retry = false;
                }
                else
                {
                    var result = await SendAsync(config, built.Content, repositoryConnectionString);
                    RecordSend(config, repositoryConnectionString, result);
                    error = result.SentToAll ? null : string.Join(Environment.NewLine, result.Channels.Count == 0 ? new List<string> { result.ToString() } : result.Failures.Select(f => f.ToString()));
                    retry = !result.SentToAny;
                    if (result.SentToAny)
                    {
                        // Once sent, a failure to record the history can't unsend it - CompareHistory logs it and the run still counts as sent
                        CompareHistory(config, repositoryConnectionString, built.Issues, nowUtc, update: true, built.KeepKeyPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Daily report {ReportName} failed", config.ReportName);
                error = ex.Message;
                retry = true;
            }

            // The outcome is decided: a failure to record it mustn't reach the caller, which would mark the report for a retry and could send it again
            try
            {
                DailyReportRepository.SetRunResult(repositoryConnectionString, config.DailyReportID, error, retry);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Daily report {ReportName}: run result not recorded (error: {Error}, retry: {Retry})", config.ReportName, error, retry);
            }
            return error;
        }

        /// <summary>Records the send on the report for the GUI.  A report that hasn't been saved yet has nowhere to record it</summary>
        private static void RecordSend(DailyReportConfig config, string connectionString, SendResult result)
        {
            if (config.DailyReportID <= 0) return;
            try
            {
                DailyReportRepository.SetSendResult(connectionString, config.DailyReportID, result.SentToAny, result.ToString());
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "Daily report {ReportName}: send result not recorded", config.ReportName);
            }
        }

        /// <summary>
        /// Adds InstanceDisplayName to the summary rows.  The object-level rows are named by display name, so the summary
        /// rows must be too, or the same instance is reported under two names.
        /// </summary>
        private static void AddDisplayNames(DataTable summary, string connectionString, string instanceList)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("SELECT InstanceID, InstanceDisplayName FROM dbo.Instances WHERE InstanceID IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@InstanceIDs,','))", cn);
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceList);
            var names = new Dictionary<int, string>();
            cn.Open();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    if (!r.IsDBNull(1)) names[r.GetInt32(0)] = r.GetString(1);
                }
            }

            ApplyDisplayNames(summary, names);
        }

        /// <summary>
        /// Sets InstanceDisplayName on each summary row from the names by InstanceID.  Summary_Get returns Azure SQL DB as one row per logical
        /// server with no InstanceID - those are named by InstanceGroupName (the server), as the summary page shows them.
        /// </summary>
        internal static void ApplyDisplayNames(DataTable summary, IReadOnlyDictionary<int, string> names)
        {
            if (!summary.Columns.Contains("InstanceDisplayName")) summary.Columns.Add("InstanceDisplayName", typeof(string));
            var hasGroupName = summary.Columns.Contains("InstanceGroupName");
            foreach (DataRow row in summary.Rows)
            {
                if (row.IsNull("InstanceID"))
                {
                    row["InstanceDisplayName"] = hasGroupName && !row.IsNull("InstanceGroupName") ? row["InstanceGroupName"] : DBNull.Value;
                    continue;
                }
                row["InstanceDisplayName"] = names.TryGetValue(Convert.ToInt32(row["InstanceID"]), out var name) ? name : (object)DBNull.Value;
            }
        }

        private static DataTable GetSummary(string connectionString, string instanceList)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.Summary_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
            cmd.Parameters.AddWithValue("@InstanceIDs", instanceList);
            cmd.Parameters.AddWithValue("@ShowHidden", false);
            cmd.Parameters.AddWithValue("@ForceRefresh", false);
            // The summary is cached (dbo.Summary), but the object detail it's listed with is read live - a stale summary can flag a drive the
            // detail no longer has.  A summary up to a minute old is used, so reports that run together share one refresh
            cmd.Parameters.Add(new SqlParameter("@ForceRefreshDate", SqlDbType.DateTime) { Value = DateTime.UtcNow.AddMinutes(-1) });
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            cn.Open();
            da.Fill(dt);
            return dt;
        }
    }
}
