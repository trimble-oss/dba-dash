using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBADash.XE
{
    /// <summary>The events an ad-hoc trace can capture.  Flags so several can be combined.</summary>
    [Flags]
    public enum XETraceEventType
    {
        RpcCompleted = 1,
        SqlBatchCompleted = 2,
        ErrorReported = 4
    }

    /// <summary>Where the ad-hoc session writes its events.  <see cref="None"/> = a target-less session whose events
    /// are consumed live (via the event stream) rather than written to a target.</summary>
    public enum XETraceTargetType
    {
        None = 0,
        EventFile = 1,
        RingBuffer = 2
    }

    /// <summary>
    /// Session scope.  On-prem / Managed Instance sessions are <c>ON SERVER</c>; Azure SQL Database uses
    /// database-scoped sessions (<c>ON DATABASE</c>).
    /// </summary>
    public enum XESessionScope
    {
        Server,
        Database
    }

    /// <summary>Allowed comparison operators.  An allow-list, mapped to a fixed literal per value.</summary>
    public enum XEFilterOp
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Like
    }

    /// <summary>
    /// A single user-supplied filter on any event field or action (from the catalog).  Carries no SQL - the field
    /// and package are validated as identifiers and the value is validated (numeric) or escaped (string) before it
    /// is placed in the DDL.  <see cref="EventName"/> scopes the filter: null/empty applies it to every applicable
    /// event (data-column filters only to events that expose that column; actions to all), or a specific event name
    /// applies it to just that event.
    /// </summary>
    public sealed class XEFilter
    {
        public string EventName { get; set; }

        /// <summary>Field or action name (e.g. "duration", "client_app_name").</summary>
        public string Field { get; set; }

        /// <summary>Package for an action reference (e.g. "sqlserver").  Ignored for event data columns.</summary>
        public string FieldPackage { get; set; } = "sqlserver";

        /// <summary>true = global action (referenced as <c>[package].[field]</c>); false = event data column (<c>[field]</c>).</summary>
        public bool IsAction { get; set; }

        /// <summary>true = numeric comparison; false = string comparison (escaped literal).</summary>
        public bool IsNumeric { get; set; }

        /// <summary>
        /// true = match with regard to case, using XE's case-sensitive comparators (<c>equal_unicode_string</c> /
        /// <c>not_equal_unicode_string</c>) instead of the bare operator.  This is needed because the bare operators
        /// (<c>=</c>/<c>&lt;&gt;</c>) bind to a case-<i>insensitive</i> default comparator regardless of the server
        /// collation, so case-sensitive matching is only possible via the explicit comparator.  Only meaningful for
        /// unicode string equality/inequality (there is no case-sensitive LIKE comparator); ignored elsewhere.  Defaults
        /// to false, so the default remains the (case-insensitive) bare operator - the UI opts a filter in.
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// The package that owns the case-sensitive comparator used when <see cref="CaseSensitive"/> is set (normally
        /// "package0").  Resolved from the instance's catalog by the UI, because the owning package can vary by edition.
        /// Null falls back to "package0".
        /// </summary>
        public string ComparatorPackage { get; set; }

        public XEFilterOp Op { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// A global action to capture on every event (the "global fields"), referenced in the DDL as
    /// <c>package.name</c> inside <c>ACTION(...)</c>.  Package and name are validated as identifiers before use.
    /// </summary>
    public sealed class XEActionDef
    {
        public XEActionDef()
        {
        }

        public XEActionDef(string package, string name)
        {
            Package = package;
            Name = name;
        }

        public string Package { get; set; } = "sqlserver";
        public string Name { get; set; }
    }

    /// <summary>
    /// A customizable-column setting for an event, emitted as <c>name=(value)</c> inside the <c>ADD EVENT ... SET</c>
    /// clause.  <see cref="Value"/> is a numeric literal (<c>0</c>/<c>1</c> for the boolean <c>collect_*</c> switches).
    /// </summary>
    public sealed class XECustomization
    {
        public XECustomization()
        {
        }

        public XECustomization(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// An arbitrary event to capture, by name.  <see cref="DataColumns"/> lists the event's data columns (from the
    /// catalog) so the builder only applies a data-column filter to events that actually expose that column.  Package
    /// and name are validated as identifiers before use in the DDL.
    /// </summary>
    public sealed class XETraceEventDef
    {
        public XETraceEventDef()
        {
        }

        public XETraceEventDef(string package, string name, IEnumerable<string> dataColumns)
        {
            Package = package;
            Name = name;
            DataColumns = dataColumns == null ? new List<string>() : new List<string>(dataColumns);
        }

        public string Package { get; set; } = "sqlserver";
        public string Name { get; set; }
        public List<string> DataColumns { get; set; } = new();
    }

    /// <summary>
    /// Builds the <c>CREATE EVENT SESSION</c> DDL for an ad-hoc extended-events trace from a strongly-typed model.
    ///
    /// <para><b>Security</b>: an XE predicate is DDL and cannot be parameterized, so this builder is the only line
    /// of defence against injection.  It accepts only the typed model (allow-listed fields/operators, numeric
    /// values validated as numbers, string values escaped as <c>N'...'</c> literals with control characters
    /// rejected).  It must run on the <b>service</b> - the GUI sends the model, never DDL, and the service echoes
    /// the generated DDL back purely for logging/audit.</para>
    /// </summary>
    public sealed class XETraceDefinition
    {
        // Session name is service-generated, but still validated + bracket-escaped as defence in depth.
        public string SessionName { get; set; } = "DBADash_AdHoc";

        /// <summary>
        /// The events to capture, each carrying its data columns (from the catalog) so a data-column filter is only
        /// applied to events that actually expose that column, and the severity floor only to events with a
        /// <c>severity</c> column.  De-duplicated by name; package and name are validated as identifiers before use.
        /// </summary>
        public IList<XETraceEventDef> Events { get; set; } = new List<XETraceEventDef>();

        public IList<XEFilter> Filters { get; set; } = new List<XEFilter>();

        public XETraceTargetType TargetType { get; set; } = XETraceTargetType.RingBuffer;

        public XESessionScope Scope { get; set; } = XESessionScope.Server;

        /// <summary>Minimum severity for <c>error_reported</c>.  Default 11 drops informational messages.</summary>
        public int ErrorSeverityFloor { get; set; } = 11;

        /// <summary>
        /// Event sampling: capture ~1 in <c>SampleN</c> events (via <c>package0.divides_by_uint64(package0.counter, N)</c>)
        /// to cut the volume/overhead of high-frequency events.  0 or 1 means no sampling (every event captured).  The UI
        /// enters this as a percentage; the value here is the resolved integer divisor.
        /// </summary>
        public int SampleN { get; set; }

        /// <summary>
        /// The default global actions (the "global fields") captured on every event.  Used when the request doesn't
        /// override them, and by the GUI as the initial selection.
        /// </summary>
        public static IReadOnlyList<XEActionDef> DefaultGlobalActions { get; } = new List<XEActionDef>
        {
            new("sqlserver", "client_app_name"),
            new("sqlserver", "client_hostname"),
            new("sqlserver", "database_id"),
            new("sqlserver", "username"),
            new("sqlserver", "session_id"),
            new("sqlserver", "context_info")
        };

        /// <summary>
        /// Global actions captured on every event.  Referenced as <c>package.name</c> in the <c>ACTION(...)</c> clause;
        /// each package/name is validated as an identifier.  Defaults to <see cref="DefaultGlobalActions"/>; an empty
        /// list emits no <c>ACTION(...)</c> clause.
        /// </summary>
        public IList<XEActionDef> GlobalActions { get; set; } = new List<XEActionDef>(DefaultGlobalActions);

        // event_file target settings (service-controlled path - never user input).
        public string FileName { get; set; }
        public int MaxFileSizeMB { get; set; } = 100;
        public int MaxRolloverFiles { get; set; } = 5;

        // ring_buffer target settings.
        public int RingBufferMaxMemoryKB { get; set; } = 4096;

        public int MaxDispatchLatencySeconds { get; set; } = 3;

        /// <summary>
        /// Application names always excluded from capture (added as <c>client_app_name &lt;&gt; N'...'</c> terms) so a
        /// trace never records DBA Dash's own reader/collector activity.  Escaped like any other string literal.
        /// </summary>
        public IList<string> ExcludedAppNames { get; set; } = new List<string> { "DBADashXE" };

        /// <summary>
        /// Per-event customizable-column settings (the <c>SET</c> toggles), keyed by event name.  Applies to both the
        /// built-in shortcut events and the extra events.  Empty means every event keeps its server defaults.
        /// </summary>
        public IDictionary<string, IList<XECustomization>> EventCustomizations { get; set; } =
            new Dictionary<string, IList<XECustomization>>(StringComparer.OrdinalIgnoreCase);

        private const int MaxStringValueLength = 256;
        private const int MaxSeverity = 25;
        // NB: anchor with \A...\z, NOT ^...$.  In .NET, $ also matches immediately BEFORE a trailing \n, so
        // "^[A-Za-z0-9_]+$" would accept an identifier with one trailing newline - a leak in an allow-list that is
        // meant to admit ONLY [A-Za-z0-9_].  \z matches only the very end of the string, closing that gap.
        private static readonly Regex SessionNamePattern = new(@"\A[A-Za-z0-9_]{1,100}\z", RegexOptions.Compiled);
        private static readonly Regex IdentifierPattern = new(@"\A[A-Za-z0-9_]{1,128}\z", RegexOptions.Compiled);

        /// <summary>Builds and returns the full <c>CREATE EVENT SESSION</c> statement.</summary>
        public string BuildCreateSessionSql()
        {
            if (!SessionNamePattern.IsMatch(SessionName ?? string.Empty))
            {
                throw new ArgumentException($"Invalid session name: '{SessionName}'.");
            }
            if (ErrorSeverityFloor < 0 || ErrorSeverityFloor > MaxSeverity)
            {
                throw new ArgumentException($"ErrorSeverityFloor must be between 0 and {MaxSeverity}.");
            }
            if (TargetType == XETraceTargetType.EventFile && string.IsNullOrWhiteSpace(FileName))
            {
                throw new ArgumentException("FileName is required for the event_file target.");
            }

            var scope = Scope == XESessionScope.Server ? "ON SERVER" : "ON DATABASE";
            var name = BracketEscape(SessionName);

            // De-duplicate the requested events by name (a session can't ADD EVENT the same event twice).  Each event
            // carries its own data columns from the catalog, so no built-in column list is needed here.
            var events = new List<XETraceEventDef>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var evt in Events ?? Enumerable.Empty<XETraceEventDef>())
            {
                if (evt?.Name != null && seen.Add(evt.Name)) events.Add(evt);
            }

            if (events.Count == 0)
            {
                throw new ArgumentException("At least one event must be selected.");
            }

            // Validate every filter's identifiers up front so a malformed filter is rejected even if it wouldn't
            // apply to any selected event (rather than being silently dropped).
            foreach (var filter in Filters ?? Enumerable.Empty<XEFilter>())
            {
                FieldReference(filter);
            }

            // Build (and validate) the shared ACTION(...) clause once - it's identical for every event.
            var actionClause = BuildActionClause();
            var eventBlocks = events.Select(e => BuildEventBlock(e, actionClause)).ToList();

            var sb = new StringBuilder();
            sb.Append("CREATE EVENT SESSION [").Append(name).Append("] ").AppendLine(scope);
            sb.AppendLine("\t" + string.Join(",\r\n\t", eventBlocks));
            // A target-less session (None) dispatches events only to the live event stream - no ADD TARGET clause.
            if (TargetType != XETraceTargetType.None)
            {
                sb.AppendLine("\t" + BuildTargetClause());
            }
            sb.Append("\tWITH (MAX_MEMORY=4096 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=")
                .Append(MaxDispatchLatencySeconds.ToString(CultureInfo.InvariantCulture))
                .Append(" SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=OFF,STARTUP_STATE=OFF)");
            return sb.ToString();
        }

        private string BuildEventBlock(XETraceEventDef evt, string actionClause)
        {
            var package = evt.Package ?? "sqlserver";
            if (!IdentifierPattern.IsMatch(package) || !IdentifierPattern.IsMatch(evt.Name ?? string.Empty))
            {
                throw new ArgumentException($"Invalid event name: '{package}.{evt.Name}'.");
            }
            var fields = new HashSet<string>(evt.DataColumns ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var setClause = BuildSetClause(evt.Name);
            return $"ADD EVENT {package.SqlQuoteName()}.{evt.Name.SqlQuoteName()}(\r\n\t\t{setClause}{actionClause}WHERE ({BuildPredicate(evt.Name, fields)}))";
        }

        /// <summary>
        /// Builds the <c>SET name=(value),...</c> clause (with its trailing separator) for one event from
        /// <see cref="EventCustomizations"/>.  Each name is validated as an identifier and each value as a
        /// non-negative integer.  Returns an empty string when the event has no customizations (keeps server defaults).
        /// </summary>
        private string BuildSetClause(string eventName)
        {
            if (EventCustomizations == null ||
                !EventCustomizations.TryGetValue(eventName ?? string.Empty, out var settings) ||
                settings == null || settings.Count == 0)
            {
                return string.Empty;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parts = new List<string>();
            foreach (var setting in settings)
            {
                if (setting?.Name == null || !IdentifierPattern.IsMatch(setting.Name))
                {
                    throw new ArgumentException($"Invalid customization field: '{setting?.Name}'.");
                }
                if (!long.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                    value < 0)
                {
                    throw new ArgumentException(
                        $"Customization '{setting.Name}' requires a non-negative integer value (got '{setting.Value}').");
                }
                if (seen.Add(setting.Name))
                {
                    parts.Add($"{setting.Name.SqlQuoteName()}=({value.ToString(CultureInfo.InvariantCulture)})");
                }
            }
            return parts.Count == 0 ? string.Empty : $"SET {string.Join(",", parts)}\r\n\t\t";
        }

        /// <summary>
        /// Builds the <c>ACTION(pkg.name,...)</c> clause (with its trailing separator) from <see cref="GlobalActions"/>,
        /// validating and de-duplicating each action.  Returns an empty string when no actions are selected, so the
        /// event is emitted with just its <c>WHERE</c> predicate.
        /// </summary>
        private string BuildActionClause()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var refs = new List<string>();
            foreach (var action in GlobalActions ?? Enumerable.Empty<XEActionDef>())
            {
                var package = action?.Package ?? "sqlserver";
                if (!IdentifierPattern.IsMatch(package) || !IdentifierPattern.IsMatch(action?.Name ?? string.Empty))
                {
                    throw new ArgumentException($"Invalid action: '{package}.{action?.Name}'.");
                }
                // Dedupe on the raw pkg.name; emit bracket-quoted (validated above, quoted here as a second layer).
                var reference = $"{package}.{action.Name}";
                if (seen.Add(reference)) refs.Add($"{package.SqlQuoteName()}.{action.Name.SqlQuoteName()}");
            }
            return refs.Count == 0 ? string.Empty : $"ACTION({string.Join(",", refs)})\r\n\t\t";
        }

        /// <summary>
        /// Builds the WHERE predicate for one event.  Terms are ANDed: the always-on excluded-app-name guards, the
        /// applicable user filters (scoped by <see cref="XEFilter.EventName"/>; an all-events data-column filter is
        /// skipped for events that don't expose that column), and a severity floor for events that expose
        /// <c>severity</c>.  There is always at least one excluded app name, so the predicate is never empty.
        /// </summary>
        private string BuildPredicate(string eventName, ISet<string> availableDataColumns)
        {
            var terms = new List<string>();

            foreach (var app in ExcludedAppNames ?? Enumerable.Empty<string>())
            {
                terms.Add($"[sqlserver].[client_app_name]<>{StringLiteral(app)}");
            }

            foreach (var filter in Filters ?? Enumerable.Empty<XEFilter>())
            {
                if (!FilterAppliesTo(filter, eventName, availableDataColumns)) continue;
                terms.Add(BuildFilterTerm(filter));
            }

            // Events exposing severity (e.g. error_reported) get the floor (validated integer, safe to inline).
            if (availableDataColumns.Contains("severity"))
            {
                terms.Add($"[severity]>=({ErrorSeverityFloor.ToString(CultureInfo.InvariantCulture)})");
            }

            // Sampling term last: capture ~1 in N events via the session counter.  Placed after the real filters so the
            // counter (which increments per predicate evaluation and short-circuits) only advances for events that
            // already matched - i.e. we sample the events of interest, not everything the server does.
            if (SampleN >= 2)
            {
                terms.Add($"[package0].[divides_by_uint64]([package0].[counter],({SampleN.ToString(CultureInfo.InvariantCulture)}))");
            }

            return string.Join(" AND ", terms);
        }

        private static bool FilterAppliesTo(XEFilter filter, string eventName, ISet<string> availableDataColumns)
        {
            if (!string.IsNullOrEmpty(filter.EventName))
            {
                // Scoped to a specific event.
                return string.Equals(filter.EventName, eventName, StringComparison.OrdinalIgnoreCase);
            }
            // All-events scope: actions apply everywhere; a data column only where the event exposes it.
            return filter.IsAction || availableDataColumns.Contains(filter.Field ?? string.Empty);
        }

        private static string FieldReference(XEFilter filter)
        {
            if (string.IsNullOrEmpty(filter.Field) || !IdentifierPattern.IsMatch(filter.Field))
            {
                throw new ArgumentException($"Invalid filter field: '{filter.Field}'.");
            }
            if (!filter.IsAction)
            {
                // Validated above; also bracket-quote at emission so the identifier is provably safe here even if the
                // validation is ever weakened (defence in depth).
                return filter.Field.SqlQuoteName();
            }
            var package = filter.FieldPackage ?? "sqlserver";
            if (!IdentifierPattern.IsMatch(package))
            {
                throw new ArgumentException($"Invalid filter field package: '{package}'.");
            }
            return $"{package.SqlQuoteName()}.{filter.Field.SqlQuoteName()}";
        }

        private static string BuildFilterTerm(XEFilter filter)
        {
            var reference = FieldReference(filter);
            if (filter.IsNumeric)
            {
                if (filter.Op == XEFilterOp.Like)
                {
                    throw new ArgumentException($"LIKE is not valid for numeric field {filter.Field}.");
                }
                if (!long.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < 0)
                {
                    throw new ArgumentException($"Filter for {filter.Field} requires a non-negative integer.");
                }
                return $"{reference}{NumericOperator(filter.Op)}({n.ToString(CultureInfo.InvariantCulture)})";
            }

            // String fields: only equality / inequality / LIKE.  Value escaped as an N'...' literal.
            var literal = StringLiteral(filter.Value);

            // Case-sensitive match: the bare operators (= <>) bind to a case-INsensitive default comparator regardless
            // of the server collation, so a case-sensitive match must use the explicit comparator (equal_unicode_string
            // / not_equal_unicode_string).  Only =/<> have a case-sensitive comparator (there is no plain
            // like_unicode_string), so any other operator falls through to the bare (case-insensitive) form below.  The
            // UI only offers this for unicode string =/<> whose comparator exists on the instance.
            if (filter.CaseSensitive && CaseSensitiveUnicodeComparator(filter.Op) is { } comparator)
            {
                // Reference the comparator by its real package (the UI resolves it from the catalog); fall back to
                // package0 (where these comparators live) if it wasn't supplied.
                var comparatorPackage = filter.ComparatorPackage;
                if (string.IsNullOrEmpty(comparatorPackage) || !IdentifierPattern.IsMatch(comparatorPackage))
                {
                    comparatorPackage = "package0";
                }
                // Validated above (comparator is a hard-coded name); bracket-quote at emission as a second layer.
                return $"{comparatorPackage.SqlQuoteName()}.{comparator.SqlQuoteName()}({reference},{literal})";
            }

            return filter.Op switch
            {
                XEFilterOp.Equal => $"{reference}={literal}",
                XEFilterOp.NotEqual => $"{reference}<>{literal}",
                XEFilterOp.Like => $"{reference} LIKE {literal}",
                _ => throw new ArgumentException($"Operator {filter.Op} is not valid for string field {filter.Field}.")
            };
        }

        /// <summary>
        /// The XE case-sensitive comparator (a <c>pred_compare</c>, normally in <c>package0</c>) for a unicode string
        /// operator, or null if the operator has no case-sensitive form.  Only equality/inequality have one - there is
        /// no plain <c>like_unicode_string</c> - so LIKE returns null and stays case-insensitive.  Shared by the builder
        /// and the UI so the offered option and the generated DDL always agree, and so the UI can confirm the comparator
        /// actually exists on the instance (see <see cref="XEObjectCatalog.SupportsComparator"/>) before offering it.
        /// </summary>
        public static string CaseSensitiveUnicodeComparator(XEFilterOp op) => op switch
        {
            XEFilterOp.Equal => "equal_unicode_string",
            XEFilterOp.NotEqual => "not_equal_unicode_string",
            _ => null
        };

        /// <summary>
        /// Converts a sampling percentage to the integer divisor N for <c>divides_by_uint64(counter, N)</c>: N =
        /// round(100 / percent).  XE can only sample in 1/N steps, so a percentage that isn't a clean 1/N is rounded to
        /// the nearest achievable N.  Returns 0 (no sampling) for percent &le; 0, percent &ge; 100, or anything that
        /// rounds to every event (N &lt; 2).
        /// </summary>
        public static int SampleNFromPercent(double percent)
        {
            if (percent <= 0 || percent >= 100) return 0;
            var n = (int)System.Math.Round(100.0 / percent, System.MidpointRounding.AwayFromZero);
            return n < 2 ? 0 : n;
        }

        /// <summary>The effective sampling percentage for a divisor N (100/N), or 100 when N means "no sampling".</summary>
        public static double PercentFromSampleN(int n) => n >= 2 ? 100.0 / n : 100.0;

        private static string NumericOperator(XEFilterOp op) => op switch
        {
            XEFilterOp.Equal => "=",
            XEFilterOp.NotEqual => "<>",
            XEFilterOp.GreaterThan => ">",
            XEFilterOp.LessThan => "<",
            XEFilterOp.GreaterThanOrEqual => ">=",
            XEFilterOp.LessThanOrEqual => "<=",
            _ => throw new ArgumentException($"Unsupported numeric operator: {op}.")
        };

        private string BuildTargetClause()
        {
            switch (TargetType)
            {
                case XETraceTargetType.EventFile:
                    return "ADD TARGET package0.event_file(SET filename=" + StringLiteral(FileName) +
                           ",max_file_size=(" + MaxFileSizeMB.ToString(CultureInfo.InvariantCulture) +
                           "),max_rollover_files=(" + MaxRolloverFiles.ToString(CultureInfo.InvariantCulture) + "))";
                case XETraceTargetType.RingBuffer:
                    return "ADD TARGET package0.ring_buffer(SET max_memory=(" +
                           RingBufferMaxMemoryKB.ToString(CultureInfo.InvariantCulture) + "))";
                default:
                    throw new ArgumentException($"Unsupported target type: {TargetType}.");
            }
        }

        /// <summary>
        /// Produces a safe <c>N'...'</c> string literal: rejects control characters, caps the length and doubles
        /// single quotes.  This is the core injection guard for every string value that reaches the DDL.
        /// </summary>
        private static string StringLiteral(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("String filter value cannot be null.");
            }
            if (value.Length > MaxStringValueLength)
            {
                throw new ArgumentException($"String value exceeds {MaxStringValueLength} characters.");
            }
            if (value.Any(c => c < 0x20))
            {
                throw new ArgumentException("String value contains control characters.");
            }
            return "N'" + value.Replace("'", "''") + "'";
        }

        /// <summary>Escapes a bracketed identifier by doubling any closing bracket (QUOTENAME semantics).</summary>
        private static string BracketEscape(string identifier) => identifier.Replace("]", "]]");
    }
}
