using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DBADash.DailyReport
{
    /// <summary>An image embedded in the email, referenced from the HTML as cid:ContentId</summary>
    public class DailyReportImage
    {
        public string ContentId { get; init; }
        public byte[] Png { get; init; }
    }

    public class DailyReportContent
    {
        public string Subject { get; init; }
        public string HtmlBody { get; init; }
        public string TextBody { get; init; }
        public int InstanceCount { get; init; }
        public int CriticalCount { get; init; }
        public int WarningCount { get; init; }
        public int NewCount { get; init; }
        public int ResolvedCount { get; init; }
        public List<DailyReportImage> Images { get; init; } = new();
        /// <summary>Sent as plain text only (DailyReportSettings.PlainTextOnly): the HTML and images aren't sent</summary>
        public bool PlainTextOnly { get; init; }
        /// <summary>Issues that got worse (warning to critical) and better (critical to warning) since the last send</summary>
        public int WorseCount { get; init; }
        public int BetterCount { get; init; }
        /// <summary>
        /// A changes report with nothing to say: nothing changed since its last send, or it's the first run, which only records where things
        /// stand.  A scheduled run doesn't send it; Send now and a preview still show it.
        /// </summary>
        public bool NothingToSend { get; init; }

        /// <summary>
        /// What the recipients will see, as a page for a preview: the HTML with its images inline, or the plain text.  bannerHtml, if given,
        /// goes at the top of the page (e.g. "Preview only - nothing was sent").
        /// </summary>
        public string PreviewHtml(string bannerHtml = null)
        {
            if (PlainTextOnly)
            {
                return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>" + WebUtility.HtmlEncode(Subject) + "</title></head><body style=\"margin:16px\">" +
                       (bannerHtml ?? string.Empty) +
                       "<pre style=\"font-family:Consolas,'Courier New',monospace;font-size:13px;white-space:pre-wrap\">" + WebUtility.HtmlEncode(TextBody) + "</pre></body></html>";
            }
            var html = HtmlWithInlineImages();
            return string.IsNullOrEmpty(bannerHtml) ? html : html.Replace(DailyReportRenderer.BodyMarker, bannerHtml, StringComparison.Ordinal);
        }

        /// <summary>The HTML with the images inline as data URIs, for a preview in a browser (an email uses cid: references and attached images)</summary>
        public string HtmlWithInlineImages()
        {
            var html = HtmlBody;
            foreach (var image in Images)
            {
                html = html.Replace($"cid:{image.ContentId}", "data:image/png;base64," + Convert.ToBase64String(image.Png), StringComparison.Ordinal);
            }
            return html;
        }
    }

    /// <summary>
    /// One problem on one database, drive or job, from dbo.DailyReport_Issues_Get.
    /// CheckGroup is the report section it's listed under, e.g. "Backups - Full".
    /// </summary>
    public class DailyReportIssue
    {
        public string CheckGroup { get; init; }
        public int Status { get; init; }
        public string Instance { get; init; }
        public string ObjectName { get; init; }
        public string Detail { get; init; }
        public bool IsCritical => Status == 1;
    }

    /// <summary>One critical or warning check on one instance, from the status columns of the summary (Summary_Get)</summary>
    public class SummaryIssue
    {
        public string Instance { get; init; }
        public string Column { get; init; }
        public string Check { get; init; }
        public int Status { get; init; }
        public string Detail { get; init; }
        public bool IsCritical => Status == 1;
    }

    /// <summary>The report's checks, sorted into the sections they're listed in</summary>
    public class ClassifiedIssues
    {
        /// <summary>Problems on a database, drive or job</summary>
        public List<DailyReportIssue> Objects { get; init; } = new();
        /// <summary>Object level checks the summary flags on an instance with no object rows behind them</summary>
        public List<DailyReportIssue> InstanceOnly { get; init; } = new();
        /// <summary>Every other summary check</summary>
        public List<SummaryIssue> Other { get; init; } = new();
    }

    /// <summary>What a render adds to the report's data: comparison with the last run, the AI summary, and how it's being rendered</summary>
    public class DailyReportExtras
    {
        public IssueHistory History { get; init; }
        public DailyReportAiSummary AiSummary { get; init; }
        /// <summary>Open alerts in scope, or null when the report doesn't include alerts</summary>
        public List<DailyReportAlert> Alerts { get; init; }
        /// <summary>The text sent to the AI service: no images, and queries named without their statement text</summary>
        public bool ForAi { get; init; }
        /// <summary>Formats an hour offset from the start of the performance window as a local time of day</summary>
        public Func<int, string> FormatHour { get; init; }
    }

    /// <summary>
    /// Builds the HTML and plain text of a daily report.
    ///
    /// The HTML is laid out for email clients rather than browsers: nested tables with inline styles and bgcolor attributes, no CSS classes,
    /// flexbox or grid, so it looks the same in Outlook (Word's renderer), Gmail and Apple Mail.  A header band, a row of summary tiles, then
    /// a card per section: object problems (backups by full/diff/log, drive space, agent jobs) grouped by type with their items in an aligned
    /// table, other summary checks by check name, performance by instance, and what was resolved since the last report.
    ///
    /// The plain text carries the same content, with underlined headings, for clients that don't show HTML and for the AI summary.
    /// Colours follow the Trimble Modus palette (https://modus-v1.trimble.com/foundations/color-palette/).
    /// </summary>
    public static class DailyReportRenderer
    {
        /// <summary>Objects listed per section before "And N others"</summary>
        public const int MaxItemsPerSection = 10;
        /// <summary>Server names listed on a line before "and N others"</summary>
        public const int MaxNamesPerLine = 10;
        /// <summary>Width of the email body.  Narrower clients shrink it - every table is width:100% with this as its max-width</summary>
        public const int BodyWidth = 760;

        /// <summary>Summary columns that the object-level sections cover, and the section each one feeds</summary>
        private static readonly Dictionary<string, string> ColumnGroups = new(StringComparer.OrdinalIgnoreCase)
        {
            ["FullBackupStatus"] = "Backups - Full",
            ["DiffBackupStatus"] = "Backups - Diff",
            ["LogBackupStatus"] = "Backups - Log",
            ["DriveStatus"] = "Drive space",
            ["JobStatus"] = "Agent jobs"
        };

        /// <summary>Order of the object-level sections, and what their items are called</summary>
        private static readonly (string Group, string Singular, string Plural)[] Sections =
        {
            ("Backups - Full", "database", "databases"),
            ("Backups - Diff", "database", "databases"),
            ("Backups - Log", "database", "databases"),
            ("Drive space", "drive", "drives"),
            ("Agent jobs", "job", "jobs")
        };

        // Trimble Modus palette
        private const string TrimbleGray = "#252a2e";
        private const string Gray6 = "#6a6e79";
        private const string Gray1 = "#cbcdd6";
        private const string Gray0 = "#e0e1e9";
        private const string GrayLight = "#f1f1f6";
        private const string GrayFaint = "#f8f8fb";
        private const string TrimbleBlue = "#0063a3";
        private const string TrimbleBlueDark = "#004f83";
        private const string BluePale = "#dcedf9";
        private const string RedDark = "#ab1f26";
        private const string Red = "#da212c";
        private const string RedPale = "#fbd4d7";
        private const string YellowDark = "#e49325";
        private const string YellowPale = "#fff5e4";
        private const string GreenDark = "#006638";
        private const string Green = "#1e8a44";
        private const string GreenPale = "#e0eccf";
        private const string White = "#ffffff";
        private const string Font = "'Segoe UI',Roboto,Helvetica,Arial,sans-serif";
        private const string MonoFont = "Consolas,'Courier New',monospace";

        /// <summary>Right after the body tag, where a preview puts its banner</summary>
        public const string BodyMarker = "<!--dr-body-->";

        /// <summary>
        /// The style block in the head.  The base rule gives every cell the report font, as Outlook doesn't pass a font down into nested
        /// tables and falls back to Times New Roman.  The rest is for phones.  Everything is laid out with inline styles for a desktop width; under 620px these classes wrap the tiles three to a
        /// row and stack table rows: the dr-i cells (badge, name) flow on one line and the dr-b cells (detail, age, figures) go underneath.
        /// Every cell in a stacking row has one of the two, as a row that mixes them with table cells wouldn't stack.  Clients that ignore
        /// a style block - desktop Outlook - are wide anyway.
        /// </summary>
        private const string HeadStyle =
            "<style type=\"text/css\">:root{color-scheme:light;supported-color-schemes:light}" +
            "body,table,td,div,p,span{font-family:" + Font + "}" +
            "@media only screen and (max-width:620px){" +
            ".dr-pad{padding-left:14px!important;padding-right:14px!important}" +
            ".dr-tile{display:inline-block!important;width:33.3%!important;box-sizing:border-box;padding:0 3px 6px 3px!important}" +
            "tr.dr-row{display:block!important;padding:6px 0!important;border-bottom:1px solid " + GrayLight + "}" +
            ".dr-i{display:inline-block!important;width:auto!important;border:0!important;padding:0 6px 0 4px!important;vertical-align:top!important}" +
            ".dr-b{display:block!important;width:auto!important;border:0!important;padding:2px 4px 0 4px!important;text-align:left!important;white-space:normal!important}" +
            "}</style>";

        /// <summary>The summary can flag an object-level check on an instance with no object rows (e.g. the object detail isn't there).
        /// Those are kept as instance-only entries in their section, so they aren't dropped and the headline still counts them.</summary>
        public static ClassifiedIssues Classify(DataTable summary, IEnumerable<DailyReportIssue> objectIssues)
        {
            var objects = (objectIssues ?? Enumerable.Empty<DailyReportIssue>())
                .Where(i => i.Status == 1 || i.Status == 2)
                .ToList();
            var summaryIssues = GetIssues(summary);

            var covered = objects.Select(i => (i.CheckGroup, i.Instance)).ToHashSet();
            var instanceOnly = new List<DailyReportIssue>();
            foreach (var s in summaryIssues.Where(i => ColumnGroups.ContainsKey(i.Column)))
            {
                var group = ColumnGroups[s.Column];
                if (covered.Contains((group, s.Instance))) continue;
                instanceOnly.Add(new DailyReportIssue { CheckGroup = group, Status = s.Status, Instance = s.Instance, ObjectName = string.Empty, Detail = s.Detail });
            }

            return new ClassifiedIssues
            {
                Objects = objects,
                InstanceOnly = instanceOnly,
                Other = summaryIssues.Where(i => !ColumnGroups.ContainsKey(i.Column)).ToList()
            };
        }

        /// <summary>Every issue the report lists, identified the same way on every run, for the comparison with the last run</summary>
        public static List<ReportIssue> IssueKeys(ClassifiedIssues issues, PerformanceAnalysis performance, IEnumerable<DailyReportAlert> alerts = null)
        {
            var keys = new List<ReportIssue>();
            // Every open alert, including acknowledged ones and those in a blackout: they aren't listed, but one that's acknowledged hasn't gone
            if (alerts != null) keys.AddRange(alerts.Select(a => new ReportIssue(a.Key, $"Alert: {a.AlertKey} on {a.Instance}", a.HistoryStatus)));
            keys.AddRange(issues.Objects.Select(i => new ReportIssue(IssueHistory.ObjectKey(i.CheckGroup, i.Instance, i.ObjectName), $"{i.CheckGroup}: {i.ObjectName} on {i.Instance}", i.Status)));
            keys.AddRange(issues.InstanceOnly.Select(i => new ReportIssue(IssueHistory.ObjectKey(i.CheckGroup, i.Instance, string.Empty), $"{i.CheckGroup}: {i.Instance}", i.Status)));
            keys.AddRange(issues.Other.Select(i => new ReportIssue(IssueHistory.CheckKey(i.Column, i.Instance), $"{i.Check} on {i.Instance}", i.Status)));
            if (performance != null)
            {
                foreach (var r in performance.Flagged)
                {
                    keys.AddRange(r.Findings.Select(f => new ReportIssue(IssueHistory.PerformanceKey(f.Metric, r.Instance), $"Performance - {f.Label} on {r.Instance}")));
                }
            }
            return keys;
        }

        public static DailyReportContent Render(DailyReportConfig config, DateTime generatedUtc, string scopeDescription,
            DataTable summary, IEnumerable<DailyReportIssue> objectIssues, PerformanceAnalysis performance, string timeZoneDisplay, DailyReportExtras extras = null)
        {
            extras ??= new DailyReportExtras();
            var settings = config.Settings ?? new DailyReportSettings();
            var issues = Classify(summary, objectIssues);
            var history = extras.History ?? IssueHistory.None(generatedUtc);
            var objects = issues.Objects;
            var instanceOnly = issues.InstanceOnly;
            var other = issues.Other;

            var instanceCount = summary?.Rows.Count ?? 0;
            var allIssueStatuses = objects.Select(i => i.Status).Concat(instanceOnly.Select(i => i.Status)).Concat(other.Select(i => i.Status)).ToList();
            var critical = allIssueStatuses.Count(s => s == 1);
            var warning = allIssueStatuses.Count - critical;
            var instancesWithIssues = objects.Select(i => i.Instance)
                .Concat(instanceOnly.Select(i => i.Instance))
                .Concat(other.Select(i => i.Instance))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var alerts = extras.Alerts;
            var listedAlerts = alerts?.Where(a => a.NeedsAttention).OrderBy(a => a.Priority).ThenBy(a => a.TriggerUtc).ToList();
            var allKeys = IssueKeys(issues, performance, alerts);
            // Acknowledged alerts and those in a blackout aren't listed, so they aren't counted as new either
            var newCount = history.IsFirstRun ? 0 : allKeys.Count(k => history.IsNew(k.Key)
                && !(alerts != null && alerts.Any(a => !a.NeedsAttention && string.Equals(a.Key, k.Key, StringComparison.OrdinalIgnoreCase))));
            // Alerts left in the history from before they were switched off aren't resolved
            var resolved = settings.IncludeResolved && !history.IsFirstRun
                ? history.Resolved.Where(r => alerts != null || !r.Key.StartsWith("alert|", StringComparison.Ordinal)).ToList()
                : new List<ResolvedIssue>();
            var hasHistory = extras.History != null && !history.IsFirstRun;

            var subject = BuildSubject(config.ReportName, critical, warning, performance?.Flagged.Count ?? 0, newCount, listedAlerts?.Count ?? 0);
            var generatedText = FormatTime(generatedUtc, timeZoneDisplay);

            var html = new StringBuilder();
            var text = new StringBuilder();
            var images = new List<DailyReportImage>();

            AppendDocumentStart(subject, "DBA Dash daily report", config.ReportName, new[] { generatedText, scopeDescription, Plural(instanceCount, "instance") }, html, text);

            // Overall status stripe and headline
            var statusColor = critical > 0 ? Red : warning > 0 ? YellowDark : Green;
            html.Append($"<tr><td height=\"4\" bgcolor=\"{statusColor}\" style=\"background:{statusColor};height:4px;line-height:4px;font-size:0\">&nbsp;</td></tr>");
            var headline = critical == 0 && warning == 0
                ? "All checks OK"
                : $"{critical} critical and {Count(warning, "warning issue", "warning issues")} on {Plural(instancesWithIssues, "instance")}";
            var headlineColor = critical > 0 ? RedDark : warning > 0 ? TrimbleGray : GreenDark;
            html.Append($"<tr><td class=\"dr-pad\" style=\"padding:18px 24px 4px 24px\"><div style=\"font-size:17px;font-weight:bold;color:{headlineColor}\">{Enc(headline)}</div>");
            text.AppendLine(headline);
            var changeLine = ChangeLine(extras.History, newCount, resolved.Count);
            if (changeLine != null)
            {
                html.Append($"<div style=\"font-size:13px;color:{Gray6};margin-top:3px\">{Enc(changeLine)}</div>");
                text.AppendLine(changeLine);
            }
            html.Append("</td></tr>");
            text.AppendLine();

            // Summary tiles
            var tiles = new List<(string Value, string Label, string Color)>
            {
                (critical.ToString(CultureInfo.InvariantCulture), "Critical", critical > 0 ? Red : Gray1),
                (warning.ToString(CultureInfo.InvariantCulture), "Warning", warning > 0 ? YellowDark : Gray1)
            };
            if (hasHistory)
            {
                tiles.Add((newCount.ToString(CultureInfo.InvariantCulture), "New", newCount > 0 ? TrimbleBlue : Gray1));
                if (settings.IncludeResolved) tiles.Add((resolved.Count.ToString(CultureInfo.InvariantCulture), "Resolved", resolved.Count > 0 ? Green : Gray1));
            }
            if (listedAlerts != null)
            {
                tiles.Add((listedAlerts.Count.ToString(CultureInfo.InvariantCulture), "Alerts", listedAlerts.Any(a => a.Status == 1) ? Red : listedAlerts.Count > 0 ? YellowDark : Gray1));
            }
            if (performance != null)
            {
                tiles.Add((performance.Flagged.Count.ToString(CultureInfo.InvariantCulture), "Performance", performance.Flagged.Count > 0 ? TrimbleBlueDark : Gray1));
            }
            AppendTiles(tiles, html);

            // AI summary
            if (!extras.ForAi && settings.IncludeAISummary && extras.AiSummary != null)
            {
                AppendAiSummary(extras.AiSummary, html, text);
            }

            // Object level sections: backups, drives, jobs
            foreach (var (group, singular, plural) in Sections)
            {
                var groupObjects = objects.Where(i => i.CheckGroup == group).ToList();
                var groupInstances = instanceOnly.Where(i => i.CheckGroup == group).ToList();
                if (groupObjects.Count == 0 && groupInstances.Count == 0) continue;
                AppendObjectSection(group, singular, plural, groupObjects, groupInstances, history, html, text);
            }

            // Other checks, grouped by check name
            if (other.Count > 0)
            {
                AppendOtherChecks(other, history, html, text);
            }
            if (objects.Count == 0 && instanceOnly.Count == 0 && other.Count == 0)
            {
                SectionStart("Critical and warning checks", null, html, text);
                html.Append($"<div style=\"font-size:13px;color:{GreenDark}\">&#10003; No critical or warning checks.</div>");
                text.AppendLine("No critical or warning checks.");
                SectionEnd(html, text);
            }

            if (alerts != null)
            {
                AppendAlerts(alerts, history, generatedUtc, extras.ForAi, html, text);
            }

            // Performance: over the thresholds and/or worse than usual
            if (performance != null)
            {
                AppendPerformance(performance, history, extras, settings.PlainTextOnly, images, html, text);
            }

            if (resolved.Count > 0)
            {
                AppendResolved(resolved, generatedUtc, html, text);
            }

            AppendDocumentEnd(html, text);

            return new DailyReportContent
            {
                Subject = subject,
                HtmlBody = html.ToString(),
                TextBody = text.ToString(),
                InstanceCount = instanceCount,
                CriticalCount = critical,
                WarningCount = warning,
                NewCount = newCount,
                ResolvedCount = resolved.Count,
                Images = images,
                PlainTextOnly = settings.PlainTextOnly
            };
        }

        /// <summary>One check on one object or instance, as a changes report lists it</summary>
        /// <summary>Label: the badge text when it isn't Critical or Warning - an alert's priority</summary>
        private record ChangeItem(string Key, int Status, string Name, string Server, string Detail, string Label = null, string Category = null, int? HistoryStatus = null)
        {
            /// <summary>What history compares: an alert's priority band, otherwise the status</summary>
            public int Compared => HistoryStatus ?? Status;
        }

        /// <summary>
        /// Every check and alert the report covers, with the key it's compared by.  Alerts that are acknowledged or in a blackout are left
        /// out, as they're being dealt with - but they're still in the history (see IssueKeys), so acknowledging one doesn't make it "resolved"
        /// </summary>
        private static List<ChangeItem> ChangeItems(ClassifiedIssues issues, IEnumerable<DailyReportAlert> alerts)
        {
            static string Detail(string group, string detail) => string.IsNullOrEmpty(detail) ? group : $"{group}: {detail}";
            var items = new List<ChangeItem>();
            items.AddRange(issues.Objects.Select(i => new ChangeItem(IssueHistory.ObjectKey(i.CheckGroup, i.Instance, i.ObjectName), i.Status, i.ObjectName, i.Instance, Detail(i.CheckGroup, i.Detail), Category: i.CheckGroup)));
            items.AddRange(issues.InstanceOnly.Select(i => new ChangeItem(IssueHistory.ObjectKey(i.CheckGroup, i.Instance, string.Empty), i.Status, i.CheckGroup, i.Instance, i.Detail, Category: i.CheckGroup)));
            items.AddRange(issues.Other.Select(i => new ChangeItem(IssueHistory.CheckKey(i.Column, i.Instance), i.Status, i.Check, i.Instance, i.Detail, Category: i.Check)));
            if (alerts != null)
            {
                items.AddRange(alerts.Where(a => a.NeedsAttention)
                    .Select(a => new ChangeItem(a.Key, a.Status, a.AlertKey, a.Instance, Detail("Alert", ShortMessage(a.Message)), a.PriorityName, "Alerts", a.HistoryStatus)));
            }
            return items;
        }

        /// <summary>
        /// Everything still open, in brief: a line per check (and one for alerts) with its counts and how many servers, in the full report's
        /// section order, then the other checks worst first, then alerts.  No items - the full report has those.
        ///   Backups - Full    2 critical, 15 warning    3 servers
        ///   Alerts            1 Critical, 1 High        2 servers
        /// </summary>
        private static void AppendOpenSummary(List<ChangeItem> items, StringBuilder html, StringBuilder text)
        {
            SectionStart("Still open", null, html, text);
            if (items.Count == 0)
            {
                html.Append($"<div style=\"font-size:13px;color:{GreenDark};margin-top:8px\">&#10003; Nothing critical or warning is open.</div>");
                text.AppendLine("Nothing critical or warning is open.");
                SectionEnd(html, text);
                return;
            }

            var sectionOrder = Sections.Select((s, i) => (s.Group, i)).ToDictionary(x => x.Group, x => x.i, StringComparer.OrdinalIgnoreCase);
            var rows = items.GroupBy(i => i.Category ?? i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var isAlerts = g.All(i => i.Label != null);
                    // Alerts by priority name, checks by critical and warning
                    var counts = isAlerts
                        ? g.GroupBy(i => i.Label).OrderBy(x => x.Min(i => i.Status)).ThenBy(x => Array.IndexOf(new[] { "Critical", "High", "Medium", "Low" }, x.Key))
                            .Select(x => $"{x.Count()} {x.Key}").ToList()
                        : new[] { (Count: g.Count(i => i.Status == 1), Word: "critical"), (Count: g.Count(i => i.Status != 1), Word: "warning") }
                            .Where(x => x.Count > 0).Select(x => $"{x.Count} {x.Word}").ToList();
                    return new
                    {
                        Category = g.Key,
                        Counts = string.Join(", ", counts),
                        Critical = g.Any(i => i.Status == 1),
                        Servers = g.Select(i => i.Server).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                        Order = isAlerts ? 3 : sectionOrder.ContainsKey(g.Key) ? 0 : g.Any(i => i.Status == 1) ? 1 : 2,
                        SectionIndex = sectionOrder.TryGetValue(g.Key, out var index) ? index : 0
                    };
                })
                .OrderBy(r => r.Order).ThenBy(r => r.SectionIndex).ThenBy(r => r.Category, StringComparer.OrdinalIgnoreCase)
                .ToList();

            html.Append(Spacer(6) + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"font-size:13px;border-collapse:collapse\">");
            foreach (var r in rows)
            {
                var cell = $"padding:4px 8px;border-bottom:1px solid {GrayLight};vertical-align:top";
                html.Append($"<tr class=\"dr-row\"><td class=\"dr-i\" width=\"40%\" style=\"{cell};width:40%;font-weight:bold\">{Enc(r.Category)}</td>");
                html.Append($"<td class=\"dr-i\" style=\"{cell};color:{(r.Critical ? RedDark : TrimbleGray)}\">{Enc(r.Counts)}</td>");
                html.Append($"<td class=\"dr-b\" width=\"90\" align=\"right\" style=\"{cell};width:90px;color:{Gray6};font-size:12px;white-space:nowrap\">{Enc(Plural(r.Servers, "server"))}</td></tr>");
                text.AppendLine($"  - {r.Category}: {r.Counts} on {Plural(r.Servers, "server")}");
            }
            html.Append("</table>");
            SectionEnd(html, text);
        }

        /// <summary>An alert's message on one line, cut to a length that fits a table cell</summary>
        private static string ShortMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;
            var oneLine = Regex.Replace(message.Trim(), @"\s+", " ");
            return oneLine.Length <= 160 ? oneLine : oneLine[..157] + "...";
        }

        /// <summary>
        /// A changes report: the checks that are new, worse (warning to critical), better (critical to warning) or resolved since the report's
        /// last send, with how many are still open for context.  Performance isn't included: it's judged over whole days.  The first run, with
        /// nothing to compare with, and a run where nothing changed have NothingToSend set - the scheduled run records where things stand
        /// without sending.
        /// </summary>
        public static DailyReportContent RenderChanges(DailyReportConfig config, DateTime generatedUtc, string scopeDescription,
            DataTable summary, IEnumerable<DailyReportIssue> objectIssues, IssueHistory history, string timeZoneDisplay, IEnumerable<DailyReportAlert> alerts = null)
        {
            var settings = config.Settings ?? new DailyReportSettings();
            history ??= IssueHistory.None(generatedUtc);
            var items = ChangeItems(Classify(summary, objectIssues), alerts);
            var firstRun = history.IsFirstRun;

            static IEnumerable<ChangeItem> Ordered(IEnumerable<ChangeItem> list) => list
                .OrderBy(i => i.Status).ThenBy(i => i.Server, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
            var added = firstRun ? new List<ChangeItem>() : Ordered(items.Where(i => history.IsNew(i.Key))).ToList();
            var worse = Ordered(items.Where(i => history.IsWorse(i.Key, i.Compared))).ToList();
            var better = Ordered(items.Where(i => history.IsBetter(i.Key, i.Compared))).ToList();
            // Performance findings can be in the history from a time the report was a full report, and alerts from before they were switched
            // off - they aren't this report's to resolve
            var resolved = firstRun ? new List<ResolvedIssue>() : history.Resolved
                .Where(r => !r.Key.StartsWith("perf|", StringComparison.Ordinal) && (alerts != null || !r.Key.StartsWith("alert|", StringComparison.Ordinal)))
                .ToList();
            var changes = added.Count + worse.Count + better.Count + resolved.Count;
            var checks = items.Where(i => i.Label == null).ToList();
            var critical = checks.Count(i => i.Status == 1);
            var warning = checks.Count - critical;
            var openAlerts = items.Count - checks.Count;

            var parts = new List<string>();
            if (added.Count > 0) parts.Add($"{added.Count} new");
            if (worse.Count > 0) parts.Add($"{worse.Count} worse");
            if (better.Count > 0) parts.Add($"{better.Count} better");
            if (resolved.Count > 0) parts.Add($"{resolved.Count} resolved");
            var subject = $"[DBA Dash] {config.ReportName}: {(parts.Count == 0 ? "no changes" : string.Join(", ", parts))}";

            var since = history.PreviousRunUtc.HasValue && !firstRun ? SinceText(history.PreviousRunUtc.Value, generatedUtc, config.TimeZone) : null;
            var html = new StringBuilder();
            var text = new StringBuilder();
            AppendDocumentStart(subject, "DBA Dash changes", config.ReportName,
                new[] { FormatTime(generatedUtc, timeZoneDisplay), scopeDescription, since == null ? null : $"Changes since {since}" }, html, text);

            // Stripe: red or amber when something new or worse is critical or a warning, green when things only got better
            var arrivals = added.Concat(worse).ToList();
            var stripe = arrivals.Any(i => i.Status == 1) ? Red : arrivals.Count > 0 ? YellowDark : changes > 0 ? Green : Gray1;
            html.Append($"<tr><td height=\"4\" bgcolor=\"{stripe}\" style=\"background:{stripe};height:4px;line-height:4px;font-size:0\">&nbsp;</td></tr>");
            var headline = firstRun
                ? "First run: this records where things stand. From the next one, the report lists what changed."
                : changes == 0 ? "No changes since the last report" : $"{string.Join(", ", parts)} since the last report";
            var headlineColor = arrivals.Any(i => i.Status == 1) ? RedDark : TrimbleGray;
            html.Append($"<tr><td class=\"dr-pad\" style=\"padding:18px 24px 4px 24px\"><div style=\"font-size:17px;font-weight:bold;color:{headlineColor}\">{Enc(headline)}</div>");
            var openParts = new List<string>();
            if (critical > 0) openParts.Add($"{critical} critical");
            if (warning > 0) openParts.Add(Count(warning, "warning", "warnings"));
            if (alerts != null && openAlerts > 0) openParts.Add(Count(openAlerts, "alert", "alerts"));
            var open = openParts.Count == 0 ? "Nothing critical or warning open." : $"Open now: {string.Join(", ", openParts)}.";
            html.Append($"<div style=\"font-size:13px;color:{Gray6};margin-top:3px\">{Enc(open)}</div></td></tr>");
            text.AppendLine(headline);
            text.AppendLine(open);
            text.AppendLine();

            if (!firstRun)
            {
                AppendTiles(new List<(string Value, string Label, string Color)>
                {
                    (added.Count.ToString(CultureInfo.InvariantCulture), "New", added.Count > 0 ? TrimbleBlue : Gray1),
                    (worse.Count.ToString(CultureInfo.InvariantCulture), "Worse", worse.Count > 0 ? Red : Gray1),
                    (better.Count.ToString(CultureInfo.InvariantCulture), "Better", better.Count > 0 ? Green : Gray1),
                    (resolved.Count.ToString(CultureInfo.InvariantCulture), "Resolved", resolved.Count > 0 ? Green : Gray1)
                }, html);
            }

            void Section(string title, List<ChangeItem> list, Func<ChangeItem, string> age)
            {
                if (list.Count == 0) return;
                SectionStart(title, list.Count.ToString(CultureInfo.InvariantCulture), html, text);
                html.Append(Spacer(8));
                AppendItemTable(list.Select(i => (i.Status, i.Label, false, i.Name, i.Server, i.Detail, age(i))), list.Count, html, text);
                SectionEnd(html, text);
            }
            // An alert's band is coarser than its priority, so it says escalated rather than what it was
            Section("New", added, _ => null);
            Section("Got worse", worse, i => i.Label == null ? "was warning" : "escalated");
            Section("Better", better, i => i.Label == null ? "was critical" : "de-escalated");
            if (resolved.Count > 0) AppendResolved(resolved, generatedUtc, html, text);
            if (settings.IncludeOpenSummary) AppendOpenSummary(items, html, text);

            AppendDocumentEnd(html, text);
            return new DailyReportContent
            {
                Subject = subject,
                HtmlBody = html.ToString(),
                TextBody = text.ToString(),
                InstanceCount = summary?.Rows.Count ?? 0,
                CriticalCount = critical,
                WarningCount = warning,
                NewCount = added.Count,
                WorseCount = worse.Count,
                BetterCount = better.Count,
                ResolvedCount = resolved.Count,
                PlainTextOnly = settings.PlainTextOnly,
                NothingToSend = firstRun || changes == 0
            };
        }

        /// <summary>"10:00" when the time is today in the time zone, otherwise "2026-11-12 10:00"</summary>
        private static string SinceText(DateTime sinceUtc, DateTime nowUtc, string timeZone)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                var since = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc), tz);
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
                return since.Date == now.Date ? since.ToString("HH:mm", CultureInfo.InvariantCulture) : since.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return $"{sinceUtc:yyyy-MM-dd HH:mm} UTC";
            }
        }

        /// <summary>
        /// The document up to the first section: a whole document, so the style block is in the head (Gmail drops one in the body), the page
        /// background, the centred body and the header band - kind ("DBA Dash daily report"), the report's name, and the details joined by dots.
        /// color-scheme stops clients that support it darkening the report's colours unpredictably.
        /// </summary>
        private static void AppendDocumentStart(string subject, string kind, string reportName, IEnumerable<string> details, StringBuilder html, StringBuilder text)
        {
            var detailList = details.Where(d => !string.IsNullOrEmpty(d)).ToList();
            html.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            html.Append("<meta name=\"color-scheme\" content=\"light\"><meta name=\"supported-color-schemes\" content=\"light\">");
            html.Append($"<title>{Enc(subject)}</title>{HeadStyle}</head>");
            html.Append($"<body style=\"margin:0;padding:0;background:{GrayLight}\">{BodyMarker}");
            // The outer table is the background in clients that drop the body's style
            html.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{GrayLight}\" style=\"background:{GrayLight};font-family:{Font};color:{TrimbleGray}\">");
            html.Append("<tr><td align=\"center\" style=\"padding:20px 8px\">");
            // The body's border is on a wrapping cell, as Outlook drops a table's border
            html.Append($"<table role=\"presentation\" width=\"{BodyWidth}\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width:100%;max-width:{BodyWidth}px\">");
            html.Append($"<tr><td bgcolor=\"{White}\" style=\"background:{White};border:1px solid {Gray0}\">");
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");

            html.Append($"<tr><td class=\"dr-pad\" bgcolor=\"{TrimbleBlueDark}\" style=\"background:{TrimbleBlueDark};padding:20px 24px\">");
            html.Append($"<div style=\"font-size:11px;letter-spacing:1px;text-transform:uppercase;color:{BluePale}\">{Enc(kind)}</div>");
            html.Append($"<div style=\"font-size:22px;font-weight:bold;color:{White};margin-top:2px\">{Enc(reportName)}</div>");
            html.Append($"<div style=\"font-size:12px;color:{BluePale};margin-top:6px\">{string.Join(" &nbsp;&middot;&nbsp; ", detailList.Select(Enc))}</div>");
            html.Append("</td></tr>");
            text.AppendLine(reportName);
            text.AppendLine(new string('=', Math.Max(reportName.Length, 10)));
            text.AppendLine(string.Join(" | ", detailList));
            text.AppendLine();
        }

        private static void AppendDocumentEnd(StringBuilder html, StringBuilder text)
        {
            const string footer = "Sent by DBA Dash daily report. Change the schedule, scope or recipients in DBA Dash.";
            html.Append($"<tr><td class=\"dr-pad\" style=\"padding:20px 24px 22px 24px;border-top:1px solid {Gray0};font-size:11px;color:{Gray6}\">{footer}</td></tr>");
            html.Append("</table></td></tr></table></td></tr></table></body></html>");
            text.AppendLine("--");
            text.AppendLine(footer);
        }

        /// <summary>
        /// "3 new since the last report · 2 resolved", or on the first run a note that there's nothing to compare with yet.
        /// Nothing when the render has no history at all (the comparison wasn't made).
        /// </summary>
        private static string ChangeLine(IssueHistory history, int newCount, int resolvedCount)
        {
            if (history == null) return null;
            if (history.IsFirstRun) return "Issues are marked as new or ongoing from the next scheduled report.";
            var parts = new List<string> { newCount == 0 ? "Nothing new since the last report" : $"{newCount} new since the last report" };
            if (resolvedCount > 0) parts.Add($"{resolvedCount} resolved");
            return string.Join(" · ", parts);
        }

        /// <summary>A row of equal tiles, a big number over a small label, each with a coloured top edge.  Grey when the count is 0</summary>
        private static void AppendTiles(List<(string Value, string Label, string Color)> tiles, StringBuilder html)
        {
            var width = 100 / tiles.Count;
            html.Append("<tr><td class=\"dr-pad\" style=\"padding:12px 18px 6px 18px\">");
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>");
            foreach (var (value, label, color) in tiles)
            {
                var valueColor = color == Gray1 ? Gray6 : color;
                html.Append($"<td class=\"dr-tile\" width=\"{width}%\" valign=\"top\" style=\"padding:0 6px\">");
                // Borders on the cell rather than the table: Outlook's Word engine drops a table's borders
                html.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
                html.Append($"<tr><td bgcolor=\"{GrayFaint}\" style=\"background:{GrayFaint};border:1px solid {Gray0};border-top:3px solid {color};padding:10px 12px\"><div style=\"font-size:24px;line-height:28px;font-weight:bold;color:{valueColor}\">{Enc(value)}</div>");
                html.Append($"<div style=\"font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:{Gray6}\">{Enc(label)}</div></td></tr></table>");
                html.Append("</td>");
            }
            html.Append("</tr></table></td></tr>");
        }

        private static void AppendAiSummary(DailyReportAiSummary ai, StringBuilder html, StringBuilder text)
        {
            html.Append("<tr><td class=\"dr-pad\" style=\"padding:12px 24px 4px 24px\">");
            if (ai.Success)
            {
                var source = string.IsNullOrWhiteSpace(ai.Model) ? "AI summary" : $"AI summary ({ai.Model})";
                html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
                html.Append($"<tr><td bgcolor=\"{GrayFaint}\" style=\"background:{GrayFaint};border:1px solid {Gray0};border-left:4px solid {TrimbleBlue};padding:12px 16px\">");
                html.Append($"<div style=\"font-size:11px;font-weight:bold;letter-spacing:0.5px;text-transform:uppercase;color:{TrimbleBlue};margin-bottom:6px\">&#10022; {Enc(source)}</div>");
                text.AppendLine(source.ToUpperInvariant());
                text.AppendLine(new string('-', source.Length));
                var inList = false;
                foreach (var block in AiSummaryFormat.Parse(ai.Summary))
                {
                    if (block.IsBullet)
                    {
                        // A table rather than <ul>: Outlook's list indents and margins can't be relied on
                        if (!inList) html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0\">");
                        inList = true;
                        html.Append($"<tr><td width=\"16\" valign=\"top\" style=\"width:16px;padding:0 0 7px 2px;font-size:14px;line-height:21px;color:{TrimbleBlue}\">&#8226;</td>");
                        html.Append($"<td valign=\"top\" style=\"padding:0 0 7px 0;font-size:14px;line-height:21px;color:{TrimbleGray}\">{AiSummaryFormat.ToHtml(block.Text)}</td></tr>");
                        text.AppendLine($"  - {AiSummaryFormat.ToText(block.Text)}");
                        continue;
                    }
                    if (inList) html.Append("</table>");
                    inList = false;
                    html.Append($"<p style=\"margin:0 0 8px 0;font-size:14px;line-height:21px;color:{TrimbleGray}\">{AiSummaryFormat.ToHtml(block.Text)}</p>");
                    text.AppendLine(AiSummaryFormat.ToText(block.Text));
                }
                if (inList) html.Append("</table>");
                html.Append(Spacer(4));
                html.Append($"<div style=\"font-size:11px;color:{Gray6}\">Written by AI from the figures below - check them before acting on it.</div>");
                html.Append("</td></tr></table>");
                text.AppendLine("(Written by AI from the figures below - check them before acting on it.)");
            }
            else
            {
                var note = $"AI summary not available: {ai.Error}";
                html.Append($"<div style=\"font-size:12px;color:{Gray6}\">{Enc(note)}</div>");
                text.AppendLine(note);
            }
            html.Append("</td></tr>");
            text.AppendLine();
        }

        /// <summary>A section's heading row: title on the left, an optional count pill on the right, a rule underneath</summary>
        private static void SectionStart(string title, string pill, StringBuilder html, StringBuilder text)
        {
            html.Append("<tr><td class=\"dr-pad\" style=\"padding:22px 24px 0 24px\">");
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>");
            html.Append($"<td style=\"padding:0 0 6px 0;border-bottom:2px solid {Gray0};font-size:16px;font-weight:bold;color:{TrimbleBlueDark}\">{Enc(title)}</td>");
            if (!string.IsNullOrEmpty(pill))
            {
                html.Append($"<td align=\"right\" style=\"padding:0 0 6px 0;border-bottom:2px solid {Gray0};white-space:nowrap\"><span style=\"background:{GrayLight};color:{Gray6};font-size:11px;font-weight:bold;padding:2px 8px;border-radius:10px\">{Enc(pill)}</span></td>");
            }
            html.Append("</tr></table>");
            text.AppendLine(title.ToUpperInvariant());
            text.AppendLine(new string('-', title.Length));
        }

        private static void SectionEnd(StringBuilder html, StringBuilder text)
        {
            html.Append("</td></tr>");
            text.AppendLine();
        }

        /// <summary>
        /// One section for a type of object problem:
        ///   Backups - Full
        ///   35 databases across 7 servers reporting critical (3 new):
        ///   Servers: LAB2022 (30), LAB2005 (5)
        ///   - [Critical] [New] DB1 on LAB2022 - no full backup recorded
        ///   - [Critical] DB2 on LAB2022 - last full backup 9 days ago (for 4 days)
        ///   ...
        ///   And 25 others.
        /// New items are listed first within their severity.  Servers flagged without any database detail are named on their own line.
        /// </summary>
        private static void AppendObjectSection(string group, string singular, string plural, List<DailyReportIssue> objects,
            List<DailyReportIssue> instanceOnly, IssueHistory history, StringBuilder html, StringBuilder text)
        {
            var newItems = objects.Count(i => history.IsNew(IssueHistory.ObjectKey(group, i.Instance, i.ObjectName)));
            var pill = objects.Count == 0 ? null : $"{Count(objects.Count, singular, plural)}{(newItems > 0 ? $" · {newItems} new" : string.Empty)}";
            SectionStart(group, pill, html, text);

            if (objects.Count > 0)
            {
                var critical = objects.Count(i => i.IsCritical);
                var warning = objects.Count - critical;
                var severityWord = critical > 0 && warning > 0 ? "reporting critical and warning" : critical > 0 ? "reporting critical" : "reporting warning";
                var perServer = objects.GroupBy(i => i.Instance, StringComparer.OrdinalIgnoreCase)
                    .Select(g => (Server: g.Key, Count: g.Count()))
                    .OrderByDescending(s => s.Count)
                    .ThenBy(s => s.Server, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var summaryLine = $"{Count(objects.Count, singular, plural)} across {Plural(perServer.Count, "server")} {severityWord}{(newItems > 0 ? $" ({newItems} new)" : string.Empty)}:";
                var serverList = string.Join(", ", perServer.Take(MaxNamesPerLine).Select(s => $"{s.Server} ({s.Count})"));
                if (perServer.Count > MaxNamesPerLine) serverList += $", and {perServer.Count - MaxNamesPerLine} others";
                var serverLine = $"Servers: {serverList}";

                // The count and new count are in the heading's pill, so the HTML only adds which servers
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:10px 0 6px 0\">{Enc($"{Plural(perServer.Count, "server")}: {serverList}")}</div>");
                text.AppendLine(summaryLine);
                text.AppendLine(serverLine);

                var rows = objects.Select(i =>
                    {
                        var key = IssueHistory.ObjectKey(group, i.Instance, i.ObjectName);
                        return (Item: i, IsNew: history.IsNew(key), Age: history.IsNew(key) ? null : history.AgeText(key));
                    })
                    .OrderBy(r => r.Item.Status)
                    .ThenByDescending(r => r.IsNew)
                    .ThenBy(r => r.Item.Instance, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.Item.ObjectName, StringComparer.OrdinalIgnoreCase)
                    .Select(r => (r.Item.Status, (string)null, r.IsNew, Name: r.Item.ObjectName, Server: r.Item.Instance, r.Item.Detail, r.Age));
                AppendItemTable(rows, objects.Count, html, text);
            }

            if (instanceOnly.Count > 0)
            {
                var names = instanceOnly.OrderBy(i => i.Status).ThenBy(i => i.Instance, StringComparer.OrdinalIgnoreCase)
                    .Select(i => i.Instance).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(n => history.IsNew(IssueHistory.ObjectKey(group, n, string.Empty)) ? $"{n} (new)" : n)
                    .ToList();
                var line = $"{Plural(names.Count, "server")} flagged with no {singular} detail: {JoinNames(names)}";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:8px 0 0 0\">{Enc(line)}</div>");
                text.AppendLine(line);
            }
            SectionEnd(html, text);
        }

        /// <summary>
        /// Items as an aligned table - severity | name and server | detail | how long - with "And N others" under it.
        /// Plain text: - [Critical] [New] DB1 on LAB2022 - no full backup recorded (for 2 days)
        /// </summary>
        private static void AppendItemTable(IEnumerable<(int Status, string Label, bool IsNew, string Name, string Server, string Detail, string Age)> rows, int total,
            StringBuilder html, StringBuilder text)
        {
            var shown = 0;
            html.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"font-size:13px;border-collapse:collapse\">");
            foreach (var row in rows.Take(MaxItemsPerSection))
            {
                var bg = shown % 2 == 1 ? GrayFaint : White;
                var cell = $"padding:6px 8px;border-bottom:1px solid {GrayLight};vertical-align:top";
                html.Append($"<tr class=\"dr-row\" bgcolor=\"{bg}\" style=\"background:{bg}\">");
                html.Append($"<td class=\"dr-i\" width=\"76\" style=\"{cell};width:76px;white-space:nowrap\">{SeverityBadge(row.Status, row.Label)}</td>");
                html.Append($"<td class=\"dr-i\" width=\"38%\" style=\"{cell};width:38%\"><b>{Enc(row.Name)}</b>{(row.IsNew ? " " + NewBadge : string.Empty)}<div style=\"font-size:11px;color:{Gray6}\">{Enc(row.Server)}</div></td>");
                html.Append($"<td class=\"dr-b\" style=\"{cell};color:{TrimbleGray}\">{Enc(row.Detail)}</td>");
                html.Append($"<td class=\"dr-b\" width=\"80\" align=\"right\" style=\"{cell};width:80px;color:{Gray6};font-size:12px;white-space:nowrap\">{Enc(row.Age ?? string.Empty)}</td>");
                html.Append("</tr>");

                var detail = string.IsNullOrEmpty(row.Detail) ? string.Empty : " - " + row.Detail;
                var age = string.IsNullOrEmpty(row.Age) ? string.Empty : $" ({row.Age})";
                text.AppendLine($"  - [{row.Label ?? (row.Status == 1 ? "Critical" : "Warning")}]{(row.IsNew ? " [New]" : string.Empty)} {row.Name} on {row.Server}{detail}{age}");
                shown++;
            }
            html.Append("</table>");
            if (total > shown)
            {
                html.Append($"<div style=\"font-size:12px;color:{Gray6};padding:6px 8px 0 8px\">And {total - shown} others.</div>");
                text.AppendLine($"  And {total - shown} others.");
            }
        }

        /// <summary>
        /// Other summary checks, one row per check name, worst first, with new instances marked:
        ///   Database State - critical on LAB2005 (new), DESKTOP1 - warning on LAB2019
        /// </summary>
        private static void AppendOtherChecks(List<SummaryIssue> other, IssueHistory history, StringBuilder html, StringBuilder text)
        {
            string Name(SummaryIssue i) => history.IsNew(IssueHistory.CheckKey(i.Column, i.Instance)) ? $"{i.Instance} (new)" : i.Instance;

            var byCheck = other.GroupBy(i => i.Check, StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    Check = g.Key,
                    Critical = g.Where(i => i.IsCritical).GroupBy(i => i.Instance, StringComparer.OrdinalIgnoreCase).Select(x => x.First())
                        .OrderBy(i => i.Instance, StringComparer.OrdinalIgnoreCase).Select(Name).ToList(),
                    Warning = g.Where(i => !i.IsCritical).GroupBy(i => i.Instance, StringComparer.OrdinalIgnoreCase).Select(x => x.First())
                        .OrderBy(i => i.Instance, StringComparer.OrdinalIgnoreCase).Select(Name).ToList()
                })
                .OrderByDescending(c => c.Critical.Count > 0)
                .ThenBy(c => c.Check, StringComparer.OrdinalIgnoreCase)
                .ToList();

            SectionStart("Other checks", Count(byCheck.Count, "check", "checks"), html, text);
            html.Append(Spacer(8) + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"font-size:13px;border-collapse:collapse\">");
            var n = 0;
            foreach (var c in byCheck)
            {
                var parts = new List<string>();
                if (c.Critical.Count > 0) parts.Add($"critical on {JoinNames(c.Critical)}");
                if (c.Warning.Count > 0) parts.Add($"warning on {JoinNames(c.Warning)}");
                text.AppendLine($"  - {c.Check} - {string.Join("; ", parts)}");

                var bg = n++ % 2 == 1 ? GrayFaint : White;
                var cell = $"padding:6px 8px;border-bottom:1px solid {GrayLight};vertical-align:top";
                html.Append($"<tr class=\"dr-row\" bgcolor=\"{bg}\" style=\"background:{bg}\"><td class=\"dr-b\" width=\"170\" style=\"{cell};font-weight:bold\">{Enc(c.Check)}</td><td class=\"dr-b\" style=\"{cell}\">");
                if (c.Critical.Count > 0) html.Append($"<div>{SeverityBadge(1)} {Enc(JoinNames(c.Critical))}</div>");
                if (c.Warning.Count > 0) html.Append($"<div style=\"{(c.Critical.Count > 0 ? "margin-top:4px" : string.Empty)}\">{SeverityBadge(2)} {Enc(JoinNames(c.Warning))}</div>");
                html.Append("</td></tr>");
            }
            html.Append("</table>");
            SectionEnd(html, text);
        }

        /// <summary>
        /// Open alerts that need attention, highest priority first, then oldest - with how long each has been open and whether it's new since
        /// the last report.  Acknowledged ones and those in a blackout are counted under the table, not listed.
        /// For the AI the message is left out: alert messages are free text - a custom SQL alert's can hold anything - and the AI is sent
        /// names and figures only.
        /// </summary>
        private static void AppendAlerts(List<DailyReportAlert> alerts, IssueHistory history, DateTime generatedUtc, bool forAi, StringBuilder html, StringBuilder text)
        {
            var listed = alerts.Where(a => a.NeedsAttention).OrderBy(a => a.Priority).ThenBy(a => a.TriggerUtc).ToList();
            var acknowledged = alerts.Count(a => a.IsAcknowledged);
            var blackout = alerts.Count(a => !a.IsAcknowledged && a.IsBlackout);
            SectionStart("Open alerts", listed.Count == 0 ? null : listed.Count.ToString(CultureInfo.InvariantCulture), html, text);
            if (listed.Count == 0)
            {
                html.Append($"<div style=\"font-size:13px;color:{GreenDark};margin-top:8px\">&#10003; No open alerts need attention.</div>");
                text.AppendLine("No open alerts need attention.");
            }
            else
            {
                html.Append(Spacer(8));
                AppendItemTable(listed.Select(a => (a.Status, a.PriorityName, history.IsNew(a.Key), a.AlertKey, a.Instance, forAi ? null : ShortMessage(a.Message), AlertAge(a.TriggerUtc, generatedUtc))),
                    listed.Count, html, text);
            }
            var notListed = new List<string>();
            if (acknowledged > 0) notListed.Add($"{acknowledged} acknowledged");
            if (blackout > 0) notListed.Add($"{blackout} in a blackout");
            if (notListed.Count > 0)
            {
                var note = $"Not listed: {string.Join(" and ", notListed)}.";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};padding:6px 8px 0 8px\">{Enc(note)}</div>");
                text.AppendLine($"  {note}");
            }
            SectionEnd(html, text);
        }

        /// <summary>"for 25 min", "for 5 hours", "for 3 days"</summary>
        internal static string AlertAge(DateTime triggerUtc, DateTime nowUtc)
        {
            var age = nowUtc - triggerUtc;
            if (age.TotalMinutes < 60) return $"for {Math.Max(1, (int)age.TotalMinutes)} min";
            if (age.TotalHours < 48) return $"for {Plural((int)age.TotalHours, "hour")}";
            return $"for {Plural((int)age.TotalDays, "day")}";
        }

        private static void AppendResolved(List<ResolvedIssue> resolved, DateTime generatedUtc, StringBuilder html, StringBuilder text)
        {
            SectionStart("Resolved since the last report", resolved.Count.ToString(CultureInfo.InvariantCulture), html, text);
            html.Append(Spacer(8) + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"font-size:13px;border-collapse:collapse\">");
            foreach (var r in resolved.Take(MaxItemsPerSection))
            {
                var days = Math.Max(0, (int)Math.Floor((generatedUtc - r.FirstSeenUtc).TotalDays + 0.01));
                var age = days >= 1 ? $" (had been reported for {Plural(days, "day")})" : string.Empty;
                var cell = $"padding:5px 8px;border-bottom:1px solid {GrayLight};vertical-align:top";
                html.Append($"<tr class=\"dr-row\"><td class=\"dr-i\" width=\"22\" style=\"{cell};color:{Green};font-weight:bold\">&#10003;</td><td class=\"dr-i\" style=\"{cell}\">{Enc(r.Description)}</td>");
                html.Append($"<td class=\"dr-b\" align=\"right\" style=\"{cell};color:{Gray6};font-size:12px;white-space:nowrap\">{(days >= 1 ? Enc($"had been reported for {Plural(days, "day")}") : string.Empty)}</td></tr>");
                text.AppendLine($"  - {r.Description}{age}");
            }
            html.Append("</table>");
            if (resolved.Count > MaxItemsPerSection)
            {
                var more = $"And {resolved.Count - MaxItemsPerSection} others.";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};padding:6px 8px 0 8px\">{Enc(more)}</div>");
                text.AppendLine($"  {more}");
            }
            SectionEnd(html, text);
        }

        /// <summary>
        /// Critical (1) and warning (2) status values from every *Status column of the summary.
        /// The instance name is the display name when the summary has one (it's the name the GUI shows), otherwise the Instance column.
        /// </summary>
        public static List<SummaryIssue> GetIssues(DataTable summary)
        {
            var issues = new List<SummaryIssue>();
            if (summary == null) return issues;

            var statusColumns = summary.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName.EndsWith("Status", StringComparison.Ordinal) && c.DataType != typeof(string))
                .ToList();

            foreach (DataRow row in summary.Rows)
            {
                var instance = InstanceName(row);
                foreach (var col in statusColumns)
                {
                    if (row.IsNull(col)) continue;
                    var status = Convert.ToInt32(row[col], CultureInfo.InvariantCulture);
                    if (status != 1 && status != 2) continue;

                    var detailColumn = col.ColumnName.Substring(0, col.ColumnName.Length - "Status".Length) + "StatusDescription";
                    string detail = null;
                    if (summary.Columns.Contains(detailColumn) && !row.IsNull(detailColumn))
                    {
                        detail = row[detailColumn].ToString();
                    }

                    issues.Add(new SummaryIssue
                    {
                        Instance = instance,
                        Column = col.ColumnName,
                        Check = FriendlyName(col.ColumnName),
                        Status = status,
                        Detail = detail
                    });
                }
            }
            return issues;
        }

        /// <summary>Display name, then Instance, then the InstanceID, so a row is never listed without a name</summary>
        private static string InstanceName(DataRow row)
        {
            foreach (var column in new[] { "InstanceDisplayName", "Instance" })
            {
                if (row.Table.Columns.Contains(column) && !row.IsNull(column) && !string.IsNullOrWhiteSpace(row[column].ToString()))
                {
                    return row[column].ToString().Trim();
                }
            }
            return row.Table.Columns.Contains("InstanceID") && !row.IsNull("InstanceID") ? $"InstanceID {row["InstanceID"]}" : "(unnamed instance)";
        }

        /// <summary>DriveStatus -&gt; Drive, LogFreeSpaceStatus -&gt; Log Free Space</summary>
        public static string FriendlyName(string columnName)
        {
            var name = columnName.EndsWith("Status", StringComparison.Ordinal) ? columnName.Substring(0, columnName.Length - "Status".Length) : columnName;
            return Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
        }

        /// <param name="performanceInstances">Instances listed in the performance section (over a threshold or worse than usual)</param>
        /// <param name="newIssues">Issues that weren't in the last report</param>
        public static string BuildSubject(string reportName, int critical, int warning, int performanceInstances, int newIssues = 0, int alerts = 0)
        {
            var parts = new List<string>();
            if (critical > 0) parts.Add($"{critical} critical");
            if (warning > 0) parts.Add($"{warning} warning");
            if (parts.Count == 0) parts.Add(performanceInstances > 0 || alerts > 0 ? "checks OK" : "all OK");
            if (performanceInstances > 0) parts.Add($"performance issues on {Plural(performanceInstances, "instance")}");
            if (alerts > 0) parts.Add(Plural(alerts, "alert"));
            if (newIssues > 0) parts.Add($"{newIssues} new");
            return $"[DBA Dash] {reportName}: {string.Join(", ", parts)}";
        }

        /// <summary>
        /// Exception based: a fleet line, then a card for each instance over a threshold or worse than usual, e.g.
        ///   SQL01 [Critical]
        ///     - CPU: 94% in the busiest hour (14:00) (3h 10m at 80%+, day average 45%) - usual 60% (+57%) - new
        ///     - Critical waits: 310 ms/s (RESOURCE_SEMAPHORE +280 ms/s) - usual 20 ms/s (+1450%) - for 3 days
        ///     Behind it (CPU, most above usual):
        ///     - Sales.dbo.usp_Orders - CPU 1h 10m (usual 5m), 12,040 executions (usual 11,900)
        /// then, in the HTML, a chart of the instance's first finding by hour.  Instances with nothing to report are counted, not listed.
        /// </summary>
        private static void AppendPerformance(PerformanceAnalysis p, IssueHistory history, DailyReportExtras extras, bool plainTextOnly, List<DailyReportImage> images,
            StringBuilder html, StringBuilder text)
        {
            var settings = p.Settings ?? new DailyReportSettings();
            SectionStart("Performance (last 24 hours)", p.Flagged.Count == 0 ? null : Count(p.Flagged.Count, "instance", "instances"), html, text);

            if (settings.PerformanceApplyToTagID != -1 && !string.IsNullOrEmpty(settings.PerformanceScopeDescription))
            {
                var scopeNote = $"Instances in the performance scope: {settings.PerformanceScopeDescription}.";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:8px 0 0 0\">{Enc(scopeNote)}</div>");
                text.AppendLine(scopeNote);
            }
            if (settings.HasPerformanceHours)
            {
                var hoursNote = $"Only {settings.PerformanceHoursText} is counted (the report's time zone), in the last 24 hours and the weeks it's compared with. The charts shade the other hours.";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:8px 0 0 0\">{Enc(hoursNote)}</div>");
                text.AppendLine(hoursNote);
            }
            if (p.ComparedWithBaseline)
            {
                var note = $"Compared with the median of the same 24 hours in the previous {Plural(p.BaselineWeeks, "week")}. Changes of {p.ChangeThresholdPct}% or more are reported.";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:8px 0 0 0\">{Enc(note)}</div>");
                text.AppendLine(note);
            }

            // Overview
            var overview = new List<string>();
            if (p.CriticalInstances > 0) overview.Add($"{p.CriticalInstances} critical");
            if (p.WarningInstances > 0) overview.Add($"{p.WarningInstances} warning");
            if (p.WorseThanUsualInstances > 0) overview.Add($"{p.WorseThanUsualInstances} worse than usual");
            var overviewLine = overview.Count == 0
                ? $"No performance issues on {Plural(p.OkCount, "instance")}."
                : $"{string.Join(", ", overview)}. {Plural(p.OkCount, "other instance")} within thresholds{(p.ComparedWithBaseline ? " and usual levels" : string.Empty)}.";
            if (p.BetterThanUsualCount > 0) overviewLine += $" {Plural(p.BetterThanUsualCount, "instance")} better than usual.";
            if (p.OverThresholdAsUsual.Count > 0) overviewLine += $" Over a threshold but within the usual range, so not listed: {JoinNames(p.OverThresholdAsUsual)}.";
            html.Append($"<div style=\"font-size:13px;font-weight:bold;margin:8px 0 0 0;color:{(overview.Count == 0 ? GreenDark : TrimbleGray)}\">{(overview.Count == 0 ? "&#10003; " : string.Empty)}{Enc(overviewLine)}</div>");
            text.AppendLine(overviewLine);

            // Fleet totals for things every busy system has some of, so they're not listed per instance unless they've changed
            var fleet = new List<string>();
            if (p.Deadlocks > 0) fleet.Add($"{Count(p.Deadlocks, "deadlock", "deadlocks")} on {Plural(p.DeadlockInstances, "instance")}");
            if (p.BlockingInstances > 0) fleet.Add($"blocking on {Plural(p.BlockingInstances, "instance")}");
            if (p.SlowQueries > 0) fleet.Add($"{Count(p.SlowQueries, "slow query", "slow queries")} on {Plural(p.SlowQueryInstances, "instance")}{(p.FailedSlowQueries > 0 ? $" ({p.FailedSlowQueries:#,0} failed or aborted)" : string.Empty)}");
            if (fleet.Count > 0)
            {
                var fleetLine = "Last 24 hours: " + string.Join(" · ", fleet);
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:3px 0 0 0\">{Enc(fleetLine)}</div>");
                text.AppendLine(fleetLine.Replace(" · ", "; "));
            }

            var formatHour = extras.FormatHour ?? (h => p.WindowStartUtc.AddHours(h).ToString("HH:mm", CultureInfo.InvariantCulture));
            var chartsOk = settings.IncludeCharts && !extras.ForAi && !plainTextOnly;
            foreach (var instance in p.Flagged.Take(settings.MaxPerformanceInstances))
            {
                text.AppendLine();
                AppendInstanceCard(instance, p, history, extras, chartsOk, formatHour, images, html, text);
            }
            if (p.Flagged.Count > settings.MaxPerformanceInstances)
            {
                var more = $"And {p.Flagged.Count - settings.MaxPerformanceInstances} other instances.";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:8px 0 0 0\">{Enc(more)}</div>");
                text.AppendLine(more);
            }

            if (p.NoData.Count > 0 || p.NoBaseline.Count > 0) text.AppendLine();
            if (p.NoData.Count > 0)
            {
                var line = $"No performance data in the last 24 hours: {JoinNames(p.NoData)}";
                html.Append($"<div style=\"font-size:12px;margin:12px 0 0 0\">{SeverityBadge(2)} {Enc(line)}</div>");
                text.AppendLine(line);
            }
            if (p.NoBaseline.Count > 0)
            {
                var line = $"Not compared, no data in the previous {Plural(p.BaselineWeeks, "week")} yet: {JoinNames(p.NoBaseline)}";
                html.Append($"<div style=\"font-size:12px;color:{Gray6};margin:6px 0 0 0\">{Enc(line)}</div>");
                text.AppendLine(line);
            }
            SectionEnd(html, text);
        }

        /// <summary>One instance in the performance section: a card with a severity edge, its findings, what's behind them and a chart</summary>
        private static void AppendInstanceCard(InstancePerformanceResult instance, PerformanceAnalysis p, IssueHistory history, DailyReportExtras extras,
            bool chartsOk, Func<int, string> formatHour, List<DailyReportImage> images, StringBuilder html, StringBuilder text)
        {
            var edge = instance.Severity switch
            {
                PerformanceSeverity.Critical => Red,
                PerformanceSeverity.Warning => YellowDark,
                _ => TrimbleBlue
            };
            // A spacer rather than a margin, and the borders on the cells, as Outlook ignores both on a table
            html.Append(Spacer(14));
            var sides = $"border-left:4px solid {edge};border-right:1px solid {Gray0}";
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\">");
            html.Append($"<tr><td bgcolor=\"{GrayFaint}\" style=\"background:{GrayFaint};padding:8px 12px;{sides};border-top:1px solid {Gray0};border-bottom:1px solid {Gray0}\">");
            html.Append($"<span style=\"font-size:15px;font-weight:bold;color:{TrimbleGray}\">{Enc(instance.Instance)}</span> &nbsp;{PerformanceBadge(instance.Severity)}");
            html.Append("</td></tr>");
            text.AppendLine($"{instance.Instance} [{SeverityText(instance.Severity)}]");

            // Findings: measure | figure and context | usual and change
            html.Append($"<tr><td style=\"padding:4px 12px 8px 12px;{sides};border-bottom:1px solid {Gray0}\">");
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"font-size:13px;border-collapse:collapse\">");
            foreach (var f in instance.Findings)
            {
                var main = f.ValueText + (string.IsNullOrEmpty(f.Context) ? string.Empty : $"; {f.Context}");
                var usual = f.BaselineText == null ? null : $"usual {f.BaselineText}";
                var age = history.AgeText(IssueHistory.PerformanceKey(f.Metric, instance.Instance));
                text.AppendLine($"  - {f.Label}: {main}{(usual == null ? string.Empty : $" - {usual}")}{(f.ChangeText == null ? string.Empty : $" ({f.ChangeText})")}{(age == null ? string.Empty : $" - {age}")}");

                var cell = $"padding:7px 6px;border-bottom:1px solid {GrayLight};vertical-align:top";
                html.Append($"<tr class=\"dr-row\"><td class=\"dr-i\" width=\"130\" style=\"{cell};font-weight:bold;white-space:nowrap\">{Enc(f.Label)}</td>");
                html.Append($"<td class=\"dr-b\" style=\"{cell}\">{Enc(f.ValueText)}");
                if (!string.IsNullOrEmpty(f.Context)) html.Append($"<div style=\"font-size:12px;color:{Gray6}\">{Enc(f.Context)}</div>");
                html.Append("</td>");
                html.Append($"<td class=\"dr-b\" align=\"right\" style=\"{cell};white-space:nowrap\">");
                if (f.ChangeText != null) html.Append($"<span style=\"color:{RedDark};font-weight:bold\">{Enc(f.ChangeText)}</span>");
                if (usual != null) html.Append($"<div style=\"font-size:12px;color:{Gray6}\">{Enc(usual)}</div>");
                if (age == "new") html.Append($"<div style=\"margin-top:2px\">{NewBadge}</div>");
                else if (age != null) html.Append($"<div style=\"font-size:11px;color:{Gray6}\">{Enc(age)}</div>");
                html.Append("</td></tr>");
            }
            html.Append("</table>");

            if (instance.TopQueries.Count > 0)
            {
                var heading = $"Behind it ({instance.TopQueriesMeasure}, {(instance.TopQueriesByIncrease ? "most above usual" : "highest")}):";
                text.AppendLine($"    {heading}");
                html.Append($"<div style=\"font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:0.5px;color:{Gray6};margin:10px 0 4px 0\">{Enc(heading.TrimEnd(':'))}</div>");
                html.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{GrayFaint}\" style=\"background:{GrayFaint};font-size:12px;border-collapse:collapse\">");
                foreach (var q in instance.TopQueries)
                {
                    var name = extras.ForAi ? q.MetadataName : q.DisplayName;
                    var figures = TopQueryAnalyzer.Describe(q, instance.TopQueriesMeasure);
                    text.AppendLine($"    - {name} - {figures}");
                    html.Append($"<tr><td style=\"padding:6px 8px;border-bottom:1px solid {White}\"><div style=\"font-family:{MonoFont};color:{TrimbleBlueDark};word-break:break-all\">{Enc(name)}</div>");
                    html.Append($"<div style=\"color:{Gray6};margin-top:1px\">{Enc(figures)}</div></td></tr>");
                }
                html.Append("</table>");
            }

            // Long running requests the query stats don't show - DBCC, backups, index maintenance
            if (instance.Activity.Count > 0)
            {
                var heading = $"Long running ({(instance.ActivityMeasure == "duration" ? "longest" : $"most {instance.ActivityMeasure}")}):";
                text.AppendLine($"    {heading}");
                html.Append($"<div style=\"font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:0.5px;color:{Gray6};margin:10px 0 4px 0\">{Enc(heading.TrimEnd(':'))}</div>");
                html.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"{GrayFaint}\" style=\"background:{GrayFaint};font-size:12px;border-collapse:collapse\">");
                foreach (var a in instance.Activity)
                {
                    var title = DailyReportActivityAnalyzer.Title(a, extras.ForAi);
                    var detail = DailyReportActivityAnalyzer.Describe(a);
                    text.AppendLine($"    - {title} - {detail}");
                    html.Append($"<tr><td style=\"padding:6px 8px;border-bottom:1px solid {White}\"><div style=\"font-family:{MonoFont};color:{TrimbleBlueDark};word-break:break-all\">{Enc(title)}</div>");
                    html.Append($"<div style=\"color:{Gray6};margin-top:1px\">{Enc(detail)}</div></td></tr>");
                }
                html.Append("</table>");
            }

            if (chartsOk && instance.Findings.Count > 0 && instance.Data != null)
            {
                AppendChart(instance, p, formatHour, images, html);
            }
            html.Append("</td></tr></table>");
        }

        /// <summary>
        /// A chart of the instance's first (worst) finding that has an hourly series.  Charts are a nice to have: one that can't be drawn
        /// (no native Skia library on this machine, say) is logged and left out rather than failing the report.
        /// </summary>
        private static void AppendChart(InstancePerformanceResult instance, PerformanceAnalysis p, Func<int, string> formatHour, List<DailyReportImage> images, StringBuilder html)
        {
            foreach (var finding in instance.Findings)
            {
                try
                {
                    var series = PerformanceChartSeries.From(instance, finding, p.Thresholds ?? new PerformanceThresholdValues(), p.BaselineWeeks, formatHour, p.IncludedHours);
                    if (!series.HasData) continue;
                    var png = PerformanceChart.RenderPng(series);
                    var id = $"chart{images.Count + 1}@dbadash";
                    images.Add(new DailyReportImage { ContentId = id, Png = png });
                    html.Append($"<div style=\"margin:12px 0 2px 0\"><img src=\"cid:{Enc(id)}\" width=\"{PerformanceChart.Width}\" height=\"{PerformanceChart.Height}\" " +
                                $"alt=\"{Enc($"{finding.Label} by hour for {instance.Instance}")}\" style=\"display:block;width:100%;max-width:{PerformanceChart.Width}px;height:auto;border:0\"></div>");
                }
                catch (Exception ex)
                {
                    // Missing native Skia library, or anything else the drawing hits - the report goes out without the chart
                    Log.Warning(ex, "Daily report chart for {Instance} could not be drawn", instance.Instance);
                }
                return;
            }
        }

        private static string SeverityText(PerformanceSeverity s) => s switch
        {
            PerformanceSeverity.Critical => "Critical",
            PerformanceSeverity.Warning => "Warning",
            _ => "Worse than usual"
        };

        private static string Badge(string text, string background, string color, string border = null) =>
            $"<span style=\"display:inline-block;background:{background};color:{color};{(border == null ? string.Empty : $"border:1px solid {border};")}padding:1px 7px;border-radius:3px;font-size:11px;line-height:16px;font-weight:bold;white-space:nowrap\">{Enc(text)}</span>";

        /// <summary>Critical and warning as for checks; worse than usual is Blue Pale with Trimble Blue Dark text</summary>
        private static string PerformanceBadge(PerformanceSeverity s) => s switch
        {
            PerformanceSeverity.Critical => SeverityBadge(1),
            PerformanceSeverity.Warning => SeverityBadge(2),
            _ => Badge("Worse than usual", BluePale, TrimbleBlueDark)
        };

        /// <summary>Status badge: Critical is Red Pale with Red Dark text; Warning is Yellow Pale with a Yellow Dark border (Yellow is never used as text)</summary>
        private static string SeverityBadge(int status, string label = null) => status == 1
            ? Badge(label ?? "Critical", RedPale, RedDark)
            : Badge(label ?? "Warning", YellowPale, TrimbleGray, YellowDark);

        /// <summary>Trimble Blue with white text, so it stands out from the severity badges</summary>
        private static readonly string NewBadge = Badge("New", TrimbleBlue, White);

        /// <summary>Comma separated names, capped at MaxNamesPerLine with "and N others"</summary>
        private static string JoinNames(List<string> names)
        {
            var shown = string.Join(", ", names.Take(MaxNamesPerLine));
            return names.Count > MaxNamesPerLine ? $"{shown}, and {names.Count - MaxNamesPerLine} others" : shown;
        }

        private static string Count(int count, string singular, string plural) => count == 1 ? $"1 {singular}" : $"{count} {plural}";

        private static string Plural(int count, string word) => count == 1 ? $"{count} {word}" : $"{count} {word}s";

        /// <summary>Local time of day of a UTC time, for "busiest hour (14:00)".  UTC if the time zone isn't known here</summary>
        public static string FormatHour(DateTime utc, string timeZone)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz).ToString("HH:mm", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return utc.ToString("HH:mm", CultureInfo.InvariantCulture) + " UTC";
            }
        }

        private static string FormatTime(DateTime utc, string timeZone)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
                return $"{local:yyyy-MM-dd HH:mm} ({timeZone})";
            }
            catch (Exception)
            {
                return $"{utc:yyyy-MM-dd HH:mm} UTC";
            }
        }

        /// <summary>Vertical space that Outlook keeps, where it ignores a table's margin</summary>
        private static string Spacer(int px) => $"<div style=\"height:{px}px;line-height:{px}px;font-size:0\">&nbsp;</div>";

        private static string Enc(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
