#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBADash.PerformanceAnalysis
{
    /// <summary>
    /// The prompt a performance analysis is answered from: the instructions, then the payload.
    ///
    /// Shared between the AI service, which builds it on every turn, and the GUI, which offers it to copy.
    /// A reader without access to the AI service - or who would rather use a tool of their own - can paste
    /// it into any assistant and get the same question the service would have asked, so the two must not
    /// drift apart.
    ///
    /// Most of the guidance is about weighing figures rather than finding them.  A model handed a table of
    /// rates and baseline ratios will report the biggest ratio, which is usually a wait that went from
    /// nothing to almost nothing; what a DBA wants is the figure that accounts for the time, and the other
    /// sections that agree with it.
    /// </summary>
    public static class PerformanceAnalysisPrompt
    {
        public static string Build(
            bool latchesAndSpinlocks,
            string instance,
            string window,
            string? baseline,
            IEnumerable<string> notes,
            IEnumerable<(string Title, string Content)> sections)
        {
            var prompt = new StringBuilder();

            if (latchesAndSpinlocks)
            {
                LatchesAndSpinlocks(prompt);
            }
            else
            {
                General(prompt);
            }

            prompt.AppendLine();
            prompt.AppendLine(
                "How to weigh the figures: rates are per second of elapsed time, so judge them by their absolute size " +
                "against the instance - wait ms per second per core, CPU against the core count - before their change.  " +
                "A ratio against the baseline matters only where the figure itself does: 0.1 to 0.5 ms/sec is 5x and " +
                "irrelevant.  Where sections have time series, tie peaks together by time.  A section that says it was " +
                "not collected or has no data tells you nothing either way - say so where the missing data matters, " +
                "rather than reading its absence as good news.  Do not invent object names, figures or settings that " +
                "are not below.");
            prompt.AppendLine();
            prompt.AppendLine(
                "The reader can ask follow-up questions after this answer.  Answer those directly and as briefly as the " +
                "question allows, without the headings and without repeating what you have already said.  Everything " +
                "below is still all you know about this instance, so where a follow-up asks for something it does not " +
                "contain, say what would answer it - which DBA Dash view or which DMV - rather than guessing.");

            prompt.AppendLine();
            prompt.AppendLine($"Instance: {instance}");
            prompt.AppendLine($"Window: {window}");
            prompt.AppendLine(string.IsNullOrWhiteSpace(baseline)
                ? "Baseline: none - the reader chose not to compare with another period."
                : $"Baseline: {baseline}");
            foreach (var note in notes.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                prompt.AppendLine($"Note: {note}");
            }

            foreach (var (title, content) in sections)
            {
                prompt.AppendLine();
                prompt.AppendLine($"## {title}");
                prompt.AppendLine(content.TrimEnd());
            }

            return prompt.ToString();
        }

        private static void General(StringBuilder prompt)
        {
            prompt.AppendLine(
                "You are analysing the performance of a SQL Server instance over a window of time, for a DBA who is " +
                "looking at the same figures in DBA Dash right now.");
            prompt.AppendLine();
            prompt.AppendLine(
                "Answer in Markdown, using these four sections as level 2 headings, in this order.  Keep to short " +
                "paragraphs and lists, about 500 words in all; the reader can ask for more.  Use code formatting for " +
                "wait types, counters, objects and settings.");
            prompt.AppendLine("1. Summary - two or three sentences: is the instance under pressure in this window, and if so what is the main constraint.");
            prompt.AppendLine("2. What stands out - the findings that matter, most important first, each with the figures that show it, " +
                              "how it compares with the baseline, and the other sections that corroborate it (PAGEIOLATCH waits with read " +
                              "latency and buffer pool memory, CXPACKET with CPU and MAXDOP, WRITELOG with log drive write latency, " +
                              "LCK_M waits with blocked sessions in the running queries snapshots and with deadlocks, PAGELATCH waits with " +
                              "tempdb allocation or metadata contention).");
            prompt.AppendLine(
                "Running queries snapshots are points in time: a long-running query or an old open transaction seen in them is a lead " +
                "worth naming (it can hold locks and log), not proof of a problem.  Tell tempdb allocation contention apart from " +
                "metadata contention - they have different fixes.  Object execution stats show what used CPU and time, not what " +
                "waited: do not name an object as the cause of tempdb, latch or lock waits because it is busy - only where the figures " +
                "tie it to those waits.  Otherwise say the cause is not identified yet.  For deadlocks, a signature that recurs is one problem to fix; " +
                "name its objects and procedures.  DBA Dash already keeps these snapshots with each query's text, waits, blocking and " +
                "plan, so where the reader needs to know which query it was, point them at DBA Dash's Running Queries tab for the time " +
                "it was seen - not at sys.dm_exec_requests or other live DMVs, which only show what is running now.");
            prompt.AppendLine("3. What to do - ranked, cheapest and safest first, each with its trade-off.");
            prompt.AppendLine("4. What to check next - what these figures cannot tell you, and where to look.");
        }

        private static void LatchesAndSpinlocks(StringBuilder prompt)
        {
            prompt.AppendLine(
                "You are analysing latch and spinlock contention on a SQL Server instance over a window of time, for a " +
                "DBA who has already looked at the figures in DBA Dash and wants to know whether the contention matters " +
                "and what is driving it.  CPU and waits are included as context.");
            prompt.AppendLine();
            prompt.AppendLine(
                "Answer in Markdown, using these five sections as level 2 headings, in this order.  Keep to short " +
                "paragraphs and lists, about 500 words in all; the reader can ask for more.  Use code formatting for " +
                "latch classes, spinlocks, wait types and settings.");
            prompt.AppendLine("1. Summary - is there contention that matters, and roughly how much of the instance's wait time or CPU it accounts for.");
            prompt.AppendLine("2. Latches - the classes that matter, what each is commonly associated with, and how they relate to the waits.");
            prompt.AppendLine("3. Spinlocks - the ones that matter, whether they are costing CPU (spins per ms per CPU, backoffs, sleep time), " +
                              "and what each is commonly associated with.");
            prompt.AppendLine("4. What to do - ranked, cheapest and safest first, each with its trade-off.");
            prompt.AppendLine("5. What to check next - what would confirm the cause.");
            prompt.AppendLine();
            prompt.AppendLine(
                "Latch class and spinlock names are internal, and what drives them varies between versions and is not " +
                "fully documented.  Say how confident you are in each association, and where it is a known pattern " +
                "rather than documented behaviour, say that - the reader may act on it.");
        }
    }
}
