using DBADashGUI.Controls;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// The STATISTICS IO and TIME output from a query's messages as tables, rather than lines to add up by eye: the
    /// totals across the query at the top, I/O by table (or, ticked, by statement) and then time, rows and reads for
    /// each statement - each in DBA Dash's own grid, so they sort, filter, sum a selection and export like any other.
    /// </summary>
    public sealed class StatisticsViewerControl : UserControl
    {
        public StatisticsViewerControl(StatisticsReport report, string title)
        {
            Report = report;
            Title = title;

            // A warning card if there were errors, or the totals likely count time twice.
            var (headline, details, note) = Summary(report);
            var summary = new StatisticsCard(headline, details, note, report.ErrorCount > 0 || LikelyCountsTwice(report) || report.QueryDoesNotFit);

            var statements = Section("Statements", new GridViewerControl(StatisticsMessages.Statements(report), $"{title} - Statements"));

            Control body;
            if (report.HasIO)
            {
                var byTable = new GridViewerControl(StatisticsMessages.IOByTable(report), $"{title} - Table I/O") { Dock = DockStyle.Fill };
                var byStatement = new GridViewerControl(StatisticsMessages.IOByStatement(report), $"{title} - Table I/O by statement")
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };
                var split = new CheckBox { Text = "By statement", AutoSize = true, Dock = DockStyle.Right };
                split.CheckedChanged += (_, _) =>
                {
                    byStatement.Visible = split.Checked;
                    byTable.Visible = !split.Checked;
                };

                var io = Section("Table I/O", byTable, split);
                io.Controls.Add(byStatement);
                byStatement.BringToFront();

                var container = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    // Sized before the splitter is placed: SplitterDistance throws if it's beyond the container.
                    Size = new Size(1200, 800),
                    SplitterDistance = 420
                };
                container.Panel1.Controls.Add(io);
                container.Panel2.Controls.Add(statements);
                body = container;
            }
            else
            {
                body = statements;
            }

            Controls.Add(body);
            Controls.Add(summary);
            this.ApplyTheme();
        }

        public StatisticsReport Report { get; }

        public string Title { get; }

        /// <summary>A grid under a heading, with anything else for the heading's right-hand end.</summary>
        internal static Panel Section(string heading, Control content, Control headingExtra = null)
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 26, Padding = new Padding(4, 2, 6, 2) };
            header.Controls.Add(new Label
            {
                Text = heading,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold)
            });
            if (headingExtra != null)
            {
                // Added after the heading, so it's docked first and takes its space before the heading fills the rest.
                header.Controls.Add(headingExtra);
            }

            content.Dock = DockStyle.Fill;
            var section = new Panel { Dock = DockStyle.Fill };
            section.Controls.Add(content);
            section.Controls.Add(header);
            return section;
        }

        private const string Separator = "   ·   ";

        private const string Ticks = "CPU is counted in ticks of about 15 ms, so short statements show 0 ms.";

        /// <summary>
        /// The card's lines: the headline totals - CPU and elapsed time, logical and physical reads - then the rest:
        /// compile time, read-ahead reads, how many tables, statements and errors, and when the query completed - and a
        /// note on where the totals come from.
        /// </summary>
        public static (string Headline, string Details, string Note) Summary(StatisticsReport report)
        {
            var headline = new List<string>();
            var details = new List<string>();

            if (report.HasTime)
            {
                // Several calls: their times overlap in ways the messages don't show, so only bounds can be given.
                var range = LikelyCountsTwice(report);
                headline.Add($"CPU {Time(range ? report.LargestCpuMs : report.CpuMs, report.CpuMs)}");
                headline.Add($"Elapsed {Time(range ? report.LargestElapsedMs : report.ElapsedMs, report.ElapsedMs)}");
                details.Add(CompileDetail(report));
            }

            if (report.HasIO)
            {
                headline.Add($"Logical reads {report.Counter(StatisticsMessages.LogicalReads):N0}");
                headline.Add($"Physical reads {report.Counter(StatisticsMessages.PhysicalReads):N0}");
                details.Add($"Read-ahead reads {report.Counter(StatisticsMessages.ReadAheadReads):N0}");
                var tables = report.Statements.SelectMany(s => s.Tables).Select(t => t.Table).Distinct(StringComparer.Ordinal).Count();
                details.Add(Count(tables, "table"));
            }

            details.Add(Count(report.Statements.Count, "statement"));
            if (report.ErrorCount > 0) details.Add(Count(report.ErrorCount, "error"));
            if (!string.IsNullOrEmpty(report.CompletionTime)) details.Add(Completed(report.CompletionTime));

            return (string.Join(Separator, headline), string.Join(Separator, details), Note(report));
        }

        /// <summary>
        /// The compile time on top of the execution time - and, for a single statement, what it compiled as it ran, which
        /// is in its time already.  A call compiles everything inside its time, its own compile included.
        /// </summary>
        private static string CompileDetail(StatisticsReport report)
        {
            if (report.SingleIsCall)
            {
                return $"Compile CPU {report.CompileCpuMsInside:N0} ms, elapsed {report.CompileElapsedMsInside:N0} ms (in its time)";
            }

            var compile = $"Compile CPU {report.CompileCpuMs:N0} ms, elapsed {report.CompileElapsedMs:N0} ms";
            if (report.CompileCpuMsInside > 0)
            {
                compile += $" (+{report.CompileCpuMsInside:N0} ms CPU as it ran, in its time)";
            }
            return compile;
        }

        /// <summary>
        /// True when the time totals very likely count something twice: they're added up, and the query has calls in
        /// it - each prints a time of its own that includes everything it ran.
        /// </summary>
        public static bool LikelyCountsTwice(StatisticsReport report) =>
            report.HasTime && report.Total == null && report.Query is { Calls: > 0 };

        /// <summary>
        /// Where the time totals come from, and how far they can be trusted - and, where the query text was read when
        /// opened rather than as it ran, that it may not be the query that printed them.
        /// </summary>
        private static string Note(StatisticsReport report) =>
            report.QueryMayHaveChanged && report.HasTime
                ? TimeNote(report) + "  The query text was read from the editor when this was opened, not as the query ran, " +
                  "so if it has changed since, these totals may be wrong."
                : TimeNote(report);

        private static string TimeNote(StatisticsReport report)
        {
            if (!report.HasTime)
            {
                return "Statements are worked out from the order of the messages.  I/O is only printed for the statements " +
                       "that did it, so none of it is counted twice.";
            }

            var sum = report.SumOfStatementsCpuMs;
            var addingUp = sum != report.CpuMs ? $" (adding everything up would give {sum:N0} ms CPU)" : "";

            if (report.SingleStatement is { } single && report.SingleIsCall)
            {
                return $"The query is a single call, {single}.  Its own time - the last and longest, marked in Statements - " +
                       "is the total.  It already includes everything the call ran - its statements, nested procedures and " +
                       $"dynamic SQL, and their compiles - so those aren't added again{addingUp}.  {Ticks}";
            }

            if (report.SingleStatement is { } statement)
            {
                return report.TotalIncludesOthers
                    ? $"The query is a single statement, {statement}.  Its own time - the last and longest, marked in " +
                      "Statements - is the total, and includes the triggers it fired, so they aren't added again" +
                      $"{addingUp}.  Its own compile, before it ran, is on top.  {Ticks}"
                    : $"The query is a single statement, {statement}.  Its compile, before it ran, is on top.  {Ticks}";
            }

            if (report.QueryDoesNotFit)
            {
                return $"The query text is a single statement, {report.Query.SingleStatement}, but the messages don't fit it - " +
                       "other statements did something too, so they're from some other query: the text may have changed, or " +
                       $"another part been selected, since it ran.  The totals add up everything printed instead.  {Ticks}";
            }

            if (report.Query is { } query)
            {
                var statements = Count(query.Statements, "statement");
                if (query.Calls > 0)
                {
                    return $"The query makes {Count(query.Calls, "call")} - procedures, sp_executesql or dynamic SQL - and " +
                           "each prints a time of its own that includes everything it ran, its compiles too.  The messages " +
                           "don't say which statements ran inside which call, so the times are a range: at least the " +
                           "longest single time printed, at most everything added up, which counts some time twice.  Run " +
                           "one call at a time, with only SET statements around it, to have its own time taken as the " +
                           $"total.  {Ticks}";
                }

                if (query.Modifications > 0)
                {
                    return $"The totals add up the query's {statements}.  One that changes data and fires a trigger prints the " +
                           "trigger's time as well as its own, which includes it - so if triggers fired, they're counted " +
                           $"twice.  {Ticks}";
                }

                return $"The totals add up the query's {statements}, and it calls nothing that prints times of its own.  {Ticks}";
            }

            return "The totals add up everything printed, worked out from the order of the messages.  A procedure call, " +
                   "dynamic SQL or a trigger prints a time of its own that includes what it ran, so a query with any of " +
                   $"them counts that time twice.  {Ticks}";
        }

        /// <summary>A time in ms, or a range of them where the least and most differ.</summary>
        private static string Time(long least, long most) =>
            least == most ? $"{most:N0} ms" : $"{least:N0} to {most:N0} ms";

        private static string Count(int count, string noun) => $"{count:N0} {noun}{(count == 1 ? "" : "s")}";

        /// <summary>
        /// SSMS's completion time to the second, kept to one line - non-breaking spaces, so a wrapped card doesn't split
        /// it - or as SSMS printed it if it doesn't parse.
        /// </summary>
        private static string Completed(string completionTime)
        {
            var text = DateTimeOffset.TryParse(completionTime, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var completed)
                ? completed.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture)
                : completionTime;
            return ("Completed " + text).Replace(' ', ' ');
        }

    }
}
