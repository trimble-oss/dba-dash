using DBADashGUI.Controls;
using DBADashGUI.Theme;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    internal enum InsightSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>A single insight (issue or observation) shown as a card. Text supports markdown-style [text](url) links.</summary>
    internal sealed class Insight
    {
        public InsightSeverity Severity { get; }
        public string Text { get; }

        /// <summary>Optional in-app actions keyed by the <c>action:&lt;key&gt;</c> links used in <see cref="Text"/>.</summary>
        public IReadOnlyDictionary<string, Action> Actions { get; }

        /// <summary>Optional hover tooltips keyed by the full link url (e.g. <c>action:&lt;key&gt;</c>) used in <see cref="Text"/>.</summary>
        public IReadOnlyDictionary<string, string> Tooltips { get; }

        /// <summary>True for a general summary shown when no issues were detected - not counted as an insight.</summary>
        public bool IsSummary { get; init; }

        public Insight(InsightSeverity severity, string text, IReadOnlyDictionary<string, Action> actions = null, IReadOnlyDictionary<string, string> tooltips = null)
        {
            Severity = severity;
            Text = text;
            Actions = actions;
            Tooltips = tooltips;
        }
    }

    /// <summary>
    /// Thresholds and wording helpers shared by the running query insights - for a single session
    /// (<see cref="SessionDetailViewer"/>) and for a whole snapshot (<see cref="SnapshotInsights"/>).
    /// </summary>
    internal static class QueryInsights
    {
        // Wait time (ms) at or above which each kind of wait is escalated from a warning to a critical issue.
        // Thresholds vary by wait type - e.g. an allocation-page latch should never take long, whereas queuing
        // for a memory grant can legitimately take longer.
        public const double AllocationCriticalMs = 1_000;      // PFS/GAM/SGAM latch - should be sub-second
        public const double TempDbCriticalMs = 5_000;          // Other tempdb contention
        public const double CompileLockCriticalMs = 5_000;     // Compile lock (serialized compilation)
        public const double CompileMemoryCriticalMs = 10_000;  // Waiting for memory to compile
        public const double MemoryGrantCriticalMs = 30_000;    // Queued for a memory grant to run
        public const double AsyncNetworkIoCriticalMs = 30_000; // Client not consuming results (rarely a server problem)

        // Memory grant (KB) at or above which a running query is flagged as having a large grant.
        public const double LargeMemoryGrantKB = 512d * 1024;      // 512 MB

        // Number of other sessions also hitting allocation-page contention in the same database at or above which the
        // wait is treated as widespread contention (rather than an isolated blip) even when the wait time is 0.
        public const int AllocationContentionWaiterThreshold = 3;

        public static InsightSeverity WaitSeverity(double waitMs, double criticalThresholdMs) =>
            waitMs >= criticalThresholdMs ? InsightSeverity.Critical : InsightSeverity.Warning;

        /// <summary>True for an allocation page type (PFS/GAM/SGAM) - the pages involved in allocation contention.</summary>
        public static bool IsAllocationPage(string pageType) => pageType is "PFS" or "GAM" or "SGAM";

        /// <summary>
        /// True for an in-memory page latch wait (PAGELATCH_*). Allocation and tempdb metadata contention show up as
        /// page latch waits - not PAGEIOLATCH_* (reading the page from disk) or lock waits, which can also report a page.
        /// </summary>
        public static bool IsPageLatchWait(string waitType) =>
            waitType?.StartsWith("PAGELATCH_", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>Allocation contention: a page latch wait on a PFS/GAM/SGAM page.</summary>
        public static bool IsAllocationContention(string pageType, string waitType) =>
            IsAllocationPage(pageType) && IsPageLatchWait(waitType);

        /// <summary>
        /// Describes the special negative blocking_session_id values that identify a blocker that isn't a session, or
        /// null for an ordinary session id. -4/-5 are latch waits (the latch owner isn't known/tracked) rather than
        /// blocking, so those also return null - callers should treat only positive ids as blocking by a session.
        /// </summary>
        public static string SpecialBlockerDescription(int blockingSessionId) => blockingSessionId switch
        {
            -2 => "an orphaned distributed transaction (blocking_session_id -2) - a DTC transaction that is no longer associated with a session. It can be found in sys.dm_tran_locks (request_session_id -2) and resolved by killing its unit of work (KILL 'UOW') once the outcome has been checked with the distributed transaction coordinator",
            -3 => "a deferred recovery transaction (blocking_session_id -3) - a transaction that couldn't be rolled back during recovery (e.g. because of an unreadable page or offline filegroup). It holds its locks until the underlying problem is fixed and the transaction can be recovered",
            _ => null
        };

        public const string CompileLockExplanation =
            "Compile locks serialize compilation when multiple sessions try to compile (or recompile) the same object at the same time. Common causes are frequent recompiles of a busy procedure, calling procedures without schema-qualifying the name, and using the sp_ prefix for user procedures.";

        public const string CompileMemoryExplanation =
            "SQL Server limits how many memory-intensive compilations can run at the same time, so concurrent compilations of large or complex queries queue up. This often indicates memory pressure, or a high volume of complex queries being compiled instead of reusing cached plans.";

        public const string TempDbPageLatchExplanation =
            "The page isn't a PFS/GAM/SGAM allocation page, so this isn't allocation contention. It's commonly tempdb metadata contention (latches on system table pages caused by a high rate of temp table creation and deletion), which SQL Server 2019+ can reduce with memory-optimized tempdb metadata. It can also be contention on the pages of a heavily used temp table.";

        /// <summary>
        /// Render a wait type as a markdown link to its sqlskills.com reference page (used by CreateContentLabel).
        /// </summary>
        public static string WaitTypeLink(string waitType)
        {
            if (string.IsNullOrEmpty(waitType)) return waitType;
            return $"[{waitType}](https://www.sqlskills.com/help/waits/{waitType.ToLowerInvariant()}/)";
        }

        // Prefix check for a pure read that RCSI can unblock. SELECT is the obvious read; CONDITIONAL is a
        // control-flow predicate (e.g. IF EXISTS(SELECT ...)) that reads under the session's isolation level and
        // takes no modification locks, so RCSI helps it the same way.
        public static bool IsReadCommand(string command)
        {
            var c = command?.Trim();
            return c?.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) == true ||
                   c?.StartsWith("CONDITIONAL", StringComparison.OrdinalIgnoreCase) == true;
        }

        // RCSI only changes the behaviour of the default Read Committed isolation level. Note SQL Server still reports
        // isolation level 'ReadCommitted' when RCSI is enabled, so this correctly gates the recommendation.
        public static bool IsReadCommittedIsolation(string isolationLevel) =>
            string.Equals(isolationLevel, "ReadCommitted", StringComparison.OrdinalIgnoreCase);

        public static string Pluralize(int count, string singular, string plural) =>
            count + " " + (count == 1 ? singular : plural);

        /// <summary>Format a size given in KB as a human-readable string (MB/GB).</summary>
        public static string HumanizeKb(double kb)
        {
            if (kb >= 1024 * 1024)
            {
                return $"{kb / (1024 * 1024):0.##} GB";
            }
            return kb >= 1024 ? $"{kb / 1024:0.##} MB" : $"{kb:0.##} KB";
        }

        public static string HumanizeMs(double ms) =>
            ms <= 0 ? null : TimeSpan.FromMilliseconds(ms).Humanize(precision: 2, maxUnit: TimeUnit.Day);

        /// <summary>" (waiting X)" suffix for the current wait time, or empty when there is no meaningful wait.</summary>
        public static string WaitedSuffix(double waitMs)
        {
            var human = HumanizeMs(waitMs);
            return human == null ? string.Empty : $" (waiting {human})";
        }
    }

    /// <summary>
    /// A vertical stack of <see cref="InsightCard"/>s. Implements <see cref="IThemedControl"/> so theming doesn't
    /// overwrite the cards' severity colours - the cards keep their pale severity background in either theme.
    /// </summary>
    internal sealed class InsightsPanel : FlowLayoutPanel, IThemedControl
    {
        private Font boldFont;

        public InsightsPanel()
        {
            Dock = DockStyle.Top;
            FlowDirection = FlowDirection.TopDown;
            WrapContents = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10, 8, 10, 10);
        }

        /// <summary>Replace the cards with one per insight, in the order given.</summary>
        public void SetInsights(IEnumerable<Insight> insights)
        {
            SuspendLayout();
            try
            {
                ClearInsights();
                foreach (var insight in insights)
                {
                    Controls.Add(CreateCard(insight));
                }
                InsightCard.FitToWidth(this);
            }
            finally
            {
                ResumeLayout(true);
            }
            ApplyTheme(ThemeExtensions.CurrentTheme);
        }

        /// <summary>Remove (and dispose) all the cards.</summary>
        public void ClearInsights()
        {
            while (Controls.Count > 0)
            {
                var card = Controls[^1];
                Controls.RemoveAt(Controls.Count - 1);
                card.Dispose();
            }
        }

        private InsightCard CreateCard(Insight insight)
        {
            var icon = insight.Severity switch
            {
                InsightSeverity.Critical => InsightCard.CardIcon.Critical,
                InsightSeverity.Warning => InsightCard.CardIcon.Warning,
                _ => InsightCard.CardIcon.Information
            };
            var textColor = InsightCard.TextFor(icon);
            var backColor = InsightCard.FillFor(icon);

            var card = new InsightCard
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(InsightCard.AccentWidth + InsightCard.IconGutter, 12, 16, 12),
                FillColor = backColor,
                AccentColor = InsightCard.AccentFor(icon),
                Icon = icon
            };

            var lbl = InsightCard.CreateContentLabel(insight.Text, insight.Actions, insight.Tooltips);
            lbl.Dock = DockStyle.Top;
            lbl.BackColor = backColor;
            lbl.ForeColor = textColor;
            if (insight.Severity == InsightSeverity.Critical)
            {
                lbl.Font = boldFont ??= new Font(Font, FontStyle.Bold);
            }
            card.Controls.Add(lbl);
            return card;
        }

        public void ApplyTheme(BaseTheme theme)
        {
            // The cards blend their rounded corners into this panel's background.
            BackColor = theme.BackgroundColor;
            ForeColor = theme.ForegroundColor;
            foreach (Control card in Controls)
            {
                foreach (Control c in card.Controls)
                {
                    if (c is LinkLabel link)
                    {
                        // Keep links legible against the pale severity background in either theme.
                        link.LinkColor = DashColors.LinkColor;
                    }
                }
                card.Invalidate();
            }
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            // Also catches a vertical scroll bar appearing when the panel's height is capped.
            InsightCard.FitToWidth(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                boldFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
