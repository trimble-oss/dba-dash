using System.Text.RegularExpressions;

namespace DBADashAI.Models
{
    /// <summary>
    /// A query plan handed over for analysis.  The query plan counterpart of
    /// <see cref="AiDeadlockAnalysisRequest"/>, and deliberately the same shape: the caller already
    /// holds the artifact and everything worth knowing about it, so the service's job is to reason
    /// over what it is given rather than to go and find anything.
    ///
    /// One statement, not a document.  A plan file can hold a whole batch and the reader is looking at
    /// one statement of it; the viewer decides which, and sends that.
    ///
    /// The shape matches what the viewer shows the user before they submit: the two must stay the
    /// same, or the preview stops being a preview.
    /// </summary>
    public class AiPlanAnalysisRequest
    {
        /// <summary>
        /// One statement's plan.  Generous compared with a deadlock graph, because a plan legitimately
        /// is: a wide query over a partitioned table runs to hundreds of kilobytes of showplan, and a
        /// statement over a thousand-partition table to megabytes.
        ///
        /// This is not the size an analysis is expected to be - it is the point past which there is no
        /// model to send one to.  The viewer warns well before here and sends anyway if the reader says
        /// so, because whether a large plan fits is the configured model's business rather than this
        /// check's.  What the check is still for is that a request arrives over the network into memory,
        /// so the field cannot be unbounded however sure of itself the caller is.
        /// </summary>
        public const int MaxPlanXmlLength = 2 * 1024 * 1024;

        /// <summary>A statement long enough to be generated rather than written, as the viewer caps it.</summary>
        public const int MaxStatementLength = 16 * 1024;

        /// <summary>
        /// The statement's plan XML.  Optional, unlike a deadlock's graph: a plan over the size limit,
        /// or one the reader chose not to send, is analysed from the summary alone, which is most of
        /// what the answer is built from anyway.
        /// </summary>
        public string? PlanXml { get; set; }

        /// <summary>The statement itself, which is required - there is nothing to analyse without it.</summary>
        public string StatementText { get; set; } = string.Empty;

        public bool StatementTruncated { get; set; }

        /// <summary>
        /// The query's identity - SQL Server's QueryHash where the plan carried one.  Groups this
        /// statement across the plans it has had, and is what a stored answer is found by.
        /// </summary>
        public string? Signature { get; set; }

        /// <summary>This plan's identity - SQL Server's QueryPlanHash where the plan carried one.</summary>
        public string? PlanHash { get; set; }

        /// <summary>
        /// The shape both identities have: "0x" followed by eight bytes as hex.  Checked rather than
        /// assumed - the caller is whoever posted the request, and the values go into the prompt, the
        /// telemetry description, the log, and the key of a stored answer.  None of those want free
        /// text, and the repository cannot take it anyway: the insert converts the string to BINARY(8).
        ///
        /// Anchored with \A and \z rather than ^ and $: the latter also matches before a trailing
        /// newline, which would let a line break through the one check standing between a posted value
        /// and the log.
        /// </summary>
        private static readonly Regex HashPattern =
            new(@"\A0x[0-9a-fA-F]{16}\z", RegexOptions.CultureInvariant);

        public string? Instance { get; set; }

        /// <summary>The file the plan was opened from, where there was one.  Context, and nothing more.</summary>
        public string? FileName { get; set; }

        /// <summary>How the plan was produced and what produced it, as the viewer summarises it.</summary>
        public List<string> Context { get; set; } = new();

        /// <summary>The statement's own figures - cost, rows, grant, compile, timings.</summary>
        public List<string> Statistics { get; set; } = new();

        /// <summary>
        /// What the caller's own rules already established.  Passed on so the model builds on the
        /// insights the user can already see rather than rediscovering or contradicting them.
        /// </summary>
        public List<string> Insights { get; set; } = new();

        public List<string> MissingIndexes { get; set; } = new();

        /// <summary>The operators that matter, ranked by their own cost - see the viewer for why own cost.</summary>
        public List<string> Operators { get; set; } = new();

        public List<string> Waits { get; set; } = new();

        public List<string> Parameters { get; set; } = new();

        public List<string> Objects { get; set; } = new();

        /// <summary>True when the plan carries measurements rather than only the optimizer's estimates.</summary>
        public bool IsActualPlan { get; set; }

        /// <summary>
        /// What kind of request this is - which is to say, what the model was given to reason from.
        /// Recorded with the analysis so a reader can tell whether an older answer was produced without
        /// the plan XML.
        /// </summary>
        public string? PayloadVersion { get; set; }

        /// <summary>Recorded with the analysis, so it is possible to see where one came from.</summary>
        public int? InstanceId { get; set; }

        /// <summary>Override the configured AI model for this request.  Null = use configured default.</summary>
        public string? ModelOverride { get; set; }

        /// <summary>What has been said about this plan already - see <see cref="AiConversation"/>.</summary>
        public List<AiConversationTurn> History { get; set; } = new();

        /// <summary>The follow-up question, when this is a follow-up.</summary>
        public string? Question { get; set; }

        /// <summary>The conversation this turn belongs to.  Minted by the caller on the first turn.</summary>
        public Guid? ConversationId { get; set; }

        /// <summary>
        /// A ceiling on the summary lists regardless of what the caller sends.  Each is capped by the
        /// viewer already; this is the total, because a plan of a thousand operators would otherwise
        /// arrive as a thousand lines of prose ahead of the XML.
        /// </summary>
        public const int MaxSummaryLength = 128 * 1024;

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(StatementText))
            {
                return "StatementText is required.";
            }

            if (StatementText.Length > MaxStatementLength)
            {
                return $"StatementText exceeds {MaxStatementLength} characters.";
            }

            if (PlanXml is { Length: > MaxPlanXmlLength })
            {
                return $"PlanXml exceeds {MaxPlanXmlLength:N0} characters, which is past what any model could " +
                       "be given.  Send the summary without it.";
            }

            if (!string.IsNullOrWhiteSpace(Signature) && !HashPattern.IsMatch(Signature!))
            {
                return "Signature is not a query hash.";
            }

            if (!string.IsNullOrWhiteSpace(PlanHash) && !HashPattern.IsMatch(PlanHash!))
            {
                return "PlanHash is not a query plan hash.";
            }

            var summary = Context.Concat(Statistics).Concat(Insights).Concat(MissingIndexes)
                .Concat(Operators).Concat(Waits).Concat(Parameters).Concat(Objects)
                .Sum(line => line?.Length ?? 0);

            return summary > MaxSummaryLength
                ? $"The plan summary exceeds {MaxSummaryLength} characters."
                : AiConversation.Validate(History, Question);
        }
    }
}
