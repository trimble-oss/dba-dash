namespace DBADashAI.Models
{
    /// <summary>
    /// An instance's performance over a window, handed over for analysis.  The same deal as the deadlock
    /// and plan requests: the caller has already gathered and summarised everything - from the procedures
    /// behind the charts the reader is looking at - and the reader has seen exactly this before sending
    /// it.  The service reasons over what it is given rather than going to find more.
    ///
    /// The sections arrive as text rather than as typed figures.  The caller decides what each contains
    /// and how it is laid out, so a section can change without a matching change here, and what the model
    /// reads is character for character what the reader approved.
    ///
    /// Nothing about it is stored: a performance analysis is about a moment, and is kept in memory by the
    /// viewer for as long as the reader is asking about it.
    /// </summary>
    public class AiPerformanceAnalysisRequest
    {
        /// <summary>
        /// The most the sections can come to.  A week summarised at the caller's deepest settings is well
        /// under this; it is the point past which the request is no longer a summary.
        /// </summary>
        public const int MaxSectionsLength = 512 * 1024;

        public const int MaxSections = 20;

        public const int MaxTitleLength = 100;

        public const int MaxHeaderLength = 1024;

        public const int MaxNotes = 10;

        public const string GeneralFocus = "General";

        public const string LatchesAndSpinlocksFocus = "LatchesAndSpinlocks";

        public string Instance { get; set; } = string.Empty;

        /// <summary>For the telemetry and the log.  Context, and nothing more.</summary>
        public int? InstanceId { get; set; }

        /// <summary><see cref="GeneralFocus"/> or <see cref="LatchesAndSpinlocksFocus"/> - which changes what the model is asked to concentrate on.</summary>
        public string Focus { get; set; } = GeneralFocus;

        /// <summary>The window, as the reader was shown it.</summary>
        public string Window { get; set; } = string.Empty;

        /// <summary>The baseline the window is compared with, as the reader was shown it.  Null when there is none.</summary>
        public string? Baseline { get; set; }

        /// <summary>How the data was chosen, where it matters to reading it.</summary>
        public List<string> Notes { get; set; } = new();

        public List<AiPerformanceAnalysisSection> Sections { get; set; } = new();

        /// <summary>What shape of payload this is, so a change in what the caller sends can be told apart in the log.</summary>
        public string? PayloadVersion { get; set; }

        /// <summary>Override the configured AI model for this request.  Null = use configured default.</summary>
        public string? ModelOverride { get; set; }

        /// <summary>What has been said about this window already - see <see cref="AiConversation"/>.</summary>
        public List<AiConversationTurn> History { get; set; } = new();

        /// <summary>The follow-up question, when this is a follow-up.</summary>
        public string? Question { get; set; }

        /// <summary>The conversation this turn belongs to.  Minted here on the first turn and echoed back.</summary>
        public Guid? ConversationId { get; set; }

        /// <summary>
        /// Let the model look up more detail from the repository before it answers - see
        /// PerformanceInvestigation.  Needs the instance id and the window in UTC, and a repository
        /// connection on the service; without any of them the analysis is answered from the payload alone.
        /// </summary>
        public bool AllowInvestigation { get; set; }

        /// <summary>The window in UTC.  The model can only look inside it.</summary>
        public DateTime? FromUtc { get; set; }

        public DateTime? ToUtc { get; set; }

        /// <summary>
        /// The reader's offset from UTC over the window.  The payload's times are the reader's, so the
        /// times the model asks for are too, and this turns them back into UTC.
        /// </summary>
        public int UtcOffsetMinutes { get; set; }

        /// <summary>
        /// Clock changes inside the window, oldest first.  <see cref="UtcOffsetMinutes"/> is the offset at the
        /// end, so without these a window across a change is read an hour out before it.
        /// </summary>
        public List<AiUtcOffsetChange> UtcOffsetChanges { get; set; } = new();

        /// <summary>The reader's offset at a UTC time.</summary>
        public int OffsetAt(DateTime utc)
        {
            var offset = UtcOffsetChanges.Count > 0 ? UtcOffsetChanges[0].PreviousOffsetMinutes : UtcOffsetMinutes;
            foreach (var change in UtcOffsetChanges)
            {
                if (utc >= change.FromUtc) offset = change.OffsetMinutes;
            }

            return offset;
        }

        /// <summary>
        /// A reader's time in UTC.  Each offset in the window is tried and the first that lands where it
        /// applies is used; a time the clocks skipped falls back to the offset at the end.
        /// </summary>
        public DateTime LocalToUtc(DateTime local)
        {
            var offsets = UtcOffsetChanges.Select(c => c.PreviousOffsetMinutes).Append(UtcOffsetMinutes).Distinct();
            foreach (var offset in offsets)
            {
                var utc = local.AddMinutes(-offset);
                if (OffsetAt(utc) == offset) return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            return DateTime.SpecifyKind(local.AddMinutes(-UtcOffsetMinutes), DateTimeKind.Utc);
        }

        public bool IsLatchesAndSpinlocks => string.Equals(Focus, LatchesAndSpinlocksFocus, StringComparison.OrdinalIgnoreCase);

        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Instance) || Instance.Length > MaxTitleLength * 2)
            {
                return "Instance is required.";
            }

            if (!string.Equals(Focus, GeneralFocus, StringComparison.OrdinalIgnoreCase) && !IsLatchesAndSpinlocks)
            {
                return $"Focus must be {GeneralFocus} or {LatchesAndSpinlocksFocus}.";
            }

            if (string.IsNullOrWhiteSpace(Window) || Window.Length > MaxHeaderLength || Baseline?.Length > MaxHeaderLength)
            {
                return "Window is required, and it and the baseline are a line each.";
            }

            // An explicit null in the JSON replaces the empty list the request starts with - a bad request, not a 500.
            if (Notes is null || Sections is null || Sections.Any(s => s is null))
            {
                return "Notes and sections are lists, and every section is a title and its content.";
            }

            if (Notes.Count > MaxNotes || Notes.Any(n => (n?.Length ?? 0) > MaxHeaderLength))
            {
                return $"At most {MaxNotes} notes of a line each.";
            }

            if (Sections.Count == 0)
            {
                return "There is nothing to analyse - include at least one section.";
            }

            if (Sections.Count > MaxSections)
            {
                return $"At most {MaxSections} sections.";
            }

            if (Sections.Any(s => string.IsNullOrWhiteSpace(s.Title) || s.Title.Length > MaxTitleLength))
            {
                return "Every section needs a title of a line.";
            }

            if (Sections.Sum(s => (s.Content?.Length ?? 0) + s.Title.Length) > MaxSectionsLength)
            {
                return $"The sections exceed {MaxSectionsLength:N0} characters.  Leave out a section or a time series.";
            }

            if (FromUtc.HasValue != ToUtc.HasValue || FromUtc >= ToUtc || Math.Abs(UtcOffsetMinutes) > 14 * 60)
            {
                return "The window in UTC is a start before an end, and the offset from UTC is at most 14 hours.";
            }

            if (UtcOffsetChanges is null || UtcOffsetChanges.Count > 10
                || UtcOffsetChanges.Any(c => c is null || Math.Abs(c.OffsetMinutes) > 14 * 60 || Math.Abs(c.PreviousOffsetMinutes) > 14 * 60))
            {
                return "Clock changes in the window are a short list of offsets of at most 14 hours.";
            }

            UtcOffsetChanges.Sort((a, b) => a.FromUtc.CompareTo(b.FromUtc));

            return AiConversation.Validate(History, Question);
        }
    }

    public class AiUtcOffsetChange
    {
        public DateTime FromUtc { get; set; }

        public int PreviousOffsetMinutes { get; set; }

        public int OffsetMinutes { get; set; }
    }

    public class AiPerformanceAnalysisSection
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
