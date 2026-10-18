using System.Text;
using DBADashAI.Models;

namespace DBADashAI.Services
{
    /// <summary>
    /// Turns an instance's performance over a window into the prompt the model answers.  The performance
    /// counterpart of <see cref="AiPlanPromptBuilder"/>.
    ///
    /// Most of the guidance is about weighing figures rather than finding them.  A model handed a table of
    /// rates and baseline ratios will report the biggest ratio, which is usually a wait that went from
    /// nothing to almost nothing; what a DBA wants is the figure that accounts for the time, and the other
    /// sections that agree with it.
    /// </summary>
    public class AiPerformancePromptBuilder
    {
        public string Build(AiPerformanceAnalysisRequest request)
        {
            var prompt = new StringBuilder();

            if (request.IsLatchesAndSpinlocks)
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
            prompt.AppendLine($"Instance: {request.Instance}");
            prompt.AppendLine($"Window: {request.Window}");
            prompt.AppendLine(string.IsNullOrWhiteSpace(request.Baseline)
                ? "Baseline: none - the reader chose not to compare with another period."
                : $"Baseline: {request.Baseline}");
            foreach (var note in request.Notes.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                prompt.AppendLine($"Note: {note}");
            }

            foreach (var section in request.Sections)
            {
                prompt.AppendLine();
                prompt.AppendLine($"## {section.Title}");
                prompt.AppendLine(section.Content.TrimEnd());
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
                              "latency and buffer pool memory, CXPACKET with CPU and MAXDOP, WRITELOG with log drive write latency).");
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
