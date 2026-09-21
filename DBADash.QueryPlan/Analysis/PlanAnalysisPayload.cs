using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Analysis
{
    /// <summary>
    /// Everything an AI analysis of a query plan is given, assembled in one place so that what the user
    /// is shown before submitting and what is actually sent cannot drift apart.  The viewer previews
    /// this object; the client serialises the same object.  It is the query plan counterpart of
    /// DeadlockAnalysisPayload, and deliberately the same shape.
    ///
    /// One statement, not the document.  A .sqlplan of a batch holds ten statements and the reader is
    /// looking at one of them; handing the model all ten and asking about the one produces an answer
    /// about whichever it found most interesting.
    ///
    /// It leaves the estate, so what goes in is a deliberate decision rather than a convenience:
    /// <list type="bullet">
    ///   <item>the statement's plan XML, which is the whole point, and the largest thing here;</item>
    ///   <item>the insights the viewer has already drawn from it, so the model builds on what the
    ///   reader can see rather than rediscovering - or contradicting - it;</item>
    ///   <item>a ranked summary of the operators, because the thing that makes a plan slow is usually
    ///   one operator and the XML makes the reader hunt for it;</item>
    ///   <item>the identities, so the answer is recorded against this plan and this query and shown
    ///   again next time either turns up, rather than paid for twice.</item>
    /// </list>
    /// The plan XML carries the statement as it was compiled or ran - which means literals, and for an
    /// actual plan the parameter values it ran with.  That is the reason the viewer shows this before
    /// sending rather than after.
    /// </summary>
    public sealed class PlanAnalysisPayload
    {
        /// <summary>
        /// A plan large enough to be worth a word before it goes.  Not a limit: a plan this size is
        /// unusual rather than wrong, and a reader who wants one analysed is better served by being told
        /// what they are about to send than by being refused.  What is past it goes if they say so.
        /// </summary>
        public const int LargePlanXmlLength = 512 * 1024;

        /// <summary>
        /// A plan past what any of this is for.  Above this the XML is left out whatever the reader
        /// says: nothing available today would take it, the service will not accept it either, and the
        /// structured summary - which is most of what an answer is built from - goes on its own.
        /// </summary>
        public const int MaxPlanXmlLength = 2 * 1024 * 1024;

        /// <summary>Operators worth naming individually.  Past this the tail is noise in every plan.</summary>
        private const int TopOperators = 15;

        /// <summary>
        /// A ceiling on the lists that have no natural one.  A plan over a thousand-partition table
        /// names a thousand objects, and a plan with a warning on every operator produces a thousand
        /// insights - both would arrive as pages of prose ahead of the XML that actually says it.
        /// </summary>
        private const int MaxListed = 60;

        /// <summary>
        /// What kind of request this is - which is to say, what the model was given to reason from.
        /// Recorded with the analysis, so a reader looking at a stored answer can tell whether it was
        /// produced from the plan XML or from the summary alone.
        ///
        /// Bump <see cref="Revision"/> when the content changes materially - not for wording, but for
        /// anything the answer could depend on.
        /// </summary>
        public string Version => PlanXmlIncluded ? $"{Revision}+xml" : Revision;

        /// <summary>1: the first shape - summary, insights, operators, and the statement's plan XML.</summary>
        private const string Revision = "1";

        /// <summary>The query's identity, which groups this statement across the plans it has had.</summary>
        public string Signature { get; private set; } = string.Empty;

        /// <summary>This plan's identity, which identifies the shape the optimiser produced for it.</summary>
        public string PlanHash { get; private set; } = string.Empty;

        /// <summary>What the identities were taken from, so the grouping is inspectable.</summary>
        public string SignatureComponents { get; private set; } = string.Empty;

        /// <summary>The instance the plan came from, where the viewer knows it.  Null for a file.</summary>
        public string? Instance { get; private set; }

        /// <summary>The file the plan was opened from, where it was one.  Context, and nothing more.</summary>
        public string? FileName { get; private set; }

        /// <summary>The statement itself.  Capped: a generated statement can run to megabytes.</summary>
        public string StatementText { get; private set; } = string.Empty;

        /// <summary>Whether <see cref="StatementText"/> was cut short.</summary>
        public bool StatementTruncated { get; private set; }

        /// <summary>How the plan was produced and what SQL Server produced it, as "key: value" lines.</summary>
        public IReadOnlyList<string> Context { get; private set; } = Array.Empty<string>();

        /// <summary>The statement's own figures - cost, rows, grant, compile, timings.</summary>
        public IReadOnlyList<string> Statistics { get; private set; } = Array.Empty<string>();

        /// <summary>What the local rules already established, as "severity: text".</summary>
        public IReadOnlyList<string> Insights { get; private set; } = Array.Empty<string>();

        /// <summary>The indexes the optimiser asked for, worst first, with the columns it wanted.</summary>
        public IReadOnlyList<string> MissingIndexes { get; private set; } = Array.Empty<string>();

        /// <summary>The operators that matter, ranked by their own cost.</summary>
        public IReadOnlyList<string> Operators { get; private set; } = Array.Empty<string>();

        /// <summary>What the statement waited on, where the plan measured it.</summary>
        public IReadOnlyList<string> Waits { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// The parameters, with the values the plan was compiled for and the values it ran with.  These
        /// are real values from a real workload, which is why the preview says so.
        /// </summary>
        public IReadOnlyList<string> Parameters { get; private set; } = Array.Empty<string>();

        /// <summary>The objects the plan reads or writes, for a later schema enrichment to resolve.</summary>
        public IReadOnlyList<string> Objects { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// The statement's plan XML, or empty when it was left out - because the reader switched it off,
        /// or because it is over <see cref="MaxPlanXmlLength"/>.
        /// </summary>
        public string PlanXml { get; private set; } = string.Empty;

        public bool PlanXmlIncluded => PlanXml.Length > 0;

        /// <summary>
        /// The size of this statement's plan XML, whether or not it is going.  Kept when it is not,
        /// because it is what the viewer's toggle has to say to be worth reading.
        /// </summary>
        public int PlanXmlLength { get; private set; }

        /// <summary>Large enough to be worth saying so first - see <see cref="LargePlanXmlLength"/>.</summary>
        public bool PlanXmlIsLarge => PlanXmlLength > LargePlanXmlLength;

        /// <summary>
        /// What the reader should know about the size before pressing send.  Set only when the XML is
        /// both going and large: it is a warning about what is about to happen, not a refusal.
        /// </summary>
        public string? PlanXmlWarning { get; private set; }

        /// <summary>Why the XML is not here, when it is not.  Null when it is.</summary>
        public string? PlanXmlOmittedReason { get; private set; }

        /// <summary>
        /// Roughly what a length of showplan costs in tokens.  Showplan runs about three characters to
        /// the token - tag and attribute names repeat, and most of the rest is numbers and GUIDs - which
        /// is close enough for the one decision it informs: whether this is near what a model will take.
        /// </summary>
        public static int EstimateTokens(int characters) => characters / 3;

        /// <summary>
        /// A size as the reader needs to see it, which is in tokens as well as bytes.  A megabyte means
        /// nothing against a context window; 350k tokens against a 200k limit means everything.
        /// </summary>
        public static string DescribeSize(int characters)
        {
            var size = characters >= 1024 * 1024
                ? $"{characters / 1024d / 1024d:0.0} MB"
                : $"{characters / 1024d:N0} KB";

            return $"{size}, roughly {EstimateTokens(characters) / 1000d:N0}k tokens";
        }

        /// <summary>True when the plan carries measurements rather than only the optimiser's estimates.</summary>
        public bool IsActualPlan { get; private set; }

        /// <summary>A statement long enough to be generated rather than written, cut to something sendable.</summary>
        private const int MaxStatementLength = 16 * 1024;

        public static PlanAnalysisPayload Build(
            ExecutionPlan plan,
            PlanStatement statement,
            string? instance = null,
            string? fileName = null,
            bool includePlanXml = true)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(statement);

            var identity = PlanIdentity.For(statement, statement.Xml);
            var text = statement.StatementText ?? string.Empty;

            var payload = new PlanAnalysisPayload
            {
                Signature = identity.Query,
                PlanHash = identity.Plan,
                SignatureComponents = identity.Components,
                Instance = instance,
                FileName = fileName,
                StatementText = text.Length > MaxStatementLength ? text[..MaxStatementLength] : text,
                StatementTruncated = text.Length > MaxStatementLength,
                IsActualPlan = statement.IsActualPlan,
                Context = DescribeContext(plan, statement),
                Statistics = DescribeStatistics(statement),
                // Worst first already, so a cap takes the tail rather than the point.
                Insights = Cap(PlanInsights.ForStatement(statement).Select(i => $"{i.Severity}: {i.Text}")),
                MissingIndexes = Cap(DescribeMissingIndexes(statement)),
                Operators = DescribeOperators(statement),
                Waits = Cap(DescribeWaits(statement)),
                Parameters = Cap(DescribeParameters(statement)),
                Objects = Cap(DescribeObjects(statement))
            };

            payload.SetPlanXml(statement.Xml, includePlanXml, plan.Statements.Count);

            return payload;
        }

        /// <summary>
        /// Decides whether the XML goes, and what to say about it when it does.
        ///
        /// The reader's choice comes first.  After that size decides, and it decides twice: a plan that
        /// is merely large goes with a warning, because a plan measured in megabytes is a real thing to
        /// have and refusing to try is not help; a plan past <see cref="MaxPlanXmlLength"/> does not go
        /// at all.  Either way it is sent whole or left out rather than cut - half a plan is not a plan,
        /// and a model handed a truncated one reasons confidently about a tree that stops in mid-air.
        /// </summary>
        private void SetPlanXml(string? xml, bool include, int statementsInDocument)
        {
            PlanXmlLength = xml?.Length ?? 0;

            if (!include)
            {
                PlanXmlOmittedReason = "The plan XML was not included.  The summary below is all the model is given.";
                return;
            }

            if (string.IsNullOrWhiteSpace(xml))
            {
                PlanXmlOmittedReason = "This statement has no plan - only the text is available.";
                return;
            }

            if (xml!.Length > MaxPlanXmlLength)
            {
                PlanXmlOmittedReason =
                    $"The plan XML is {DescribeSize(xml.Length)}, past the {DescribeSize(MaxPlanXmlLength)} anything " +
                    "would accept, so it is not included.  It is left out rather than cut short: a plan that stops " +
                    "in the middle reads as a complete one.  The summary below is all the model is given.";
                return;
            }

            PlanXml = xml;

            if (xml.Length <= LargePlanXmlLength) return;

            PlanXmlWarning =
                $"This plan is large: {DescribeSize(xml.Length)}.  Models in common use accept around 200k tokens " +
                "and refuse anything past that, so this may come back as a refusal rather than an answer - it is " +
                "worth sending where the service is configured with a model whose context window is larger." +
                (statementsInDocument > 1
                    ? $"  This is the plan for this statement alone; the document holds {statementsInDocument:N0}."
                    : string.Empty);
        }

        private static List<string> DescribeContext(ExecutionPlan plan, PlanStatement statement)
        {
            var context = new List<string>
            {
                statement.IsActualPlan
                    ? "This is an actual plan: it carries the row counts and times of a real run."
                    : "This is an estimated plan: it shows what the optimizer expected, with no measurements."
            };

            Add(context, "Statement type", statement.StatementType);
            Add(context, "SQL Server build", plan.Build);
            Add(context, "Showplan version", plan.Version);
            Add(context, "Cardinality estimation model", statement.CardinalityEstimationModelVersion);
            Add(context, "Optimization level", statement.OptimisationLevel);
            Add(context, "Optimizer stopped early because", statement.OptimisationEarlyAbortReason);
            Add(context, "Retrieved from cache", statement.RetrievedFromCache?.ToString());
            Add(context, "Single-threaded because", statement.NonParallelPlanReason);
            Add(context, "Statements in the document", plan.Statements.Count > 1 ? plan.Statements.Count.ToString(CultureInfo.InvariantCulture) : null);

            return context;
        }

        private static List<string> DescribeStatistics(PlanStatement statement)
        {
            var stats = new List<string>();

            Add(stats, "Estimated subtree cost", PlanFormat.Cost(statement.StatementSubTreeCost));
            Add(stats, "Estimated rows", statement.StatementEstRows is { } rows ? PlanFormat.Rows(rows) : null);
            Add(stats, "Degree of parallelism", statement.DegreeOfParallelism?.ToString(CultureInfo.InvariantCulture));
            Add(stats, "Threads reserved / used",
                statement.ReservedThreads is { } reserved && statement.UsedThreads is { } used
                    ? $"{reserved} / {used}"
                    : null);
            Add(stats, "Cached plan size", statement.CachedPlanSizeKb is { } size ? $"{size:N0} KB" : null);
            Add(stats, "Compile time", statement.CompileTimeMs is { } compile ? PlanFormat.Duration(compile) : null);
            Add(stats, "Compile CPU", statement.CompileCpuMs is { } compileCpu ? PlanFormat.Duration(compileCpu) : null);
            Add(stats, "Compile memory", statement.CompileMemoryKb is { } compileMemory ? $"{compileMemory:N0} KB" : null);

            if (statement.QueryTimeStats is { } times)
            {
                Add(stats, "Elapsed", times.ElapsedMs is { } elapsed ? PlanFormat.Duration(elapsed) : null);
                Add(stats, "CPU", times.CpuMs is { } cpu ? PlanFormat.Duration(cpu) : null);
                Add(stats, "Elapsed in scalar UDFs", times.UdfElapsedMs is { } udf ? PlanFormat.Duration(udf) : null);
            }

            if (statement.MemoryGrant is { } grant)
            {
                Add(stats, "Memory requested", grant.RequestedMemoryKb is { } requested ? $"{requested:N0} KB" : null);
                Add(stats, "Memory granted", grant.GrantedMemoryKb is { } granted ? $"{granted:N0} KB" : null);
                Add(stats, "Memory used", grant.MaxUsedMemoryKb is { } maxUsed ? $"{maxUsed:N0} KB" : null);
                Add(stats, "Waited for the grant", grant.GrantWaitTimeMs is { } wait ? PlanFormat.Duration(wait) : null);
            }

            return stats;
        }

        private static List<string> DescribeMissingIndexes(PlanStatement statement) =>
            statement.MissingIndexes
                .OrderByDescending(i => i.Impact)
                .Select(index =>
                {
                    var keys = index.EqualityColumns.Concat(index.InequalityColumns).ToList();
                    var parts = new List<string> { $"{index.QualifiedTableName}, impact {index.Impact:0.#}%" };

                    if (keys.Count > 0) parts.Add("keys " + string.Join(", ", keys));
                    if (index.IncludedColumns.Count > 0) parts.Add("include " + string.Join(", ", index.IncludedColumns));

                    return string.Join("; ", parts);
                })
                .ToList();

        /// <summary>
        /// The operators worth naming, ranked by their own cost rather than their subtree cost.
        ///
        /// Subtree cost is what the XML carries and what puts 100% on the root of every plan, which
        /// tells nobody anything.  Own cost is the figure that points at the operator to look at - and
        /// alongside it, the estimate error, which is what usually explains why the plan is that shape.
        /// </summary>
        private static List<string> DescribeOperators(PlanStatement statement)
        {
            if (!statement.HasPlan) return new List<string>();

            return statement.Operators
                .Where(op => op.NodeId >= 0)
                .OrderByDescending(op => op.OperatorCost)
                .Take(TopOperators)
                .Select(Describe)
                .ToList();

            static string Describe(PlanOperator op)
            {
                var parts = new List<string> { $"node {op.NodeId} {op.DisplayName}" };

                if (!string.IsNullOrWhiteSpace(op.LogicalOp) && !string.Equals(op.LogicalOp, op.PhysicalOp, StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add(op.LogicalOp!);
                }

                if (op.PrimaryObject is { } obj) parts.Add("on " + obj);
                if (op.IsLookup) parts.Add("key lookup");
                if (op.IsParallel) parts.Add("parallel");

                parts.Add($"{PlanFormat.Percent(op.CostPercent)} of cost");
                parts.Add($"estimated {PlanFormat.Rows(op.EstimateRowsAllExecutions ?? op.EstimateRows)} rows");

                if (op.Runtime is { } runtime)
                {
                    parts.Add($"actual {PlanFormat.Rows(runtime.ActualRows)} rows");
                    if (op.RowEstimateError is { } error and > 1.5) parts.Add($"estimate out by {error:0.#}x");
                    if (runtime.ActualExecutions > 1) parts.Add($"{runtime.ActualExecutions:N0} executions");
                    if (runtime.ActualLogicalReads is { } reads and > 0) parts.Add($"{reads:N0} logical reads");
                    if (op.OwnElapsedMs is { } elapsed and > 0) parts.Add($"{PlanFormat.Duration(elapsed)} in this operator");
                }

                if (!string.IsNullOrWhiteSpace(op.SeekPredicate)) parts.Add("seek " + PlanFormat.SingleLine(op.SeekPredicate!, 200));
                if (!string.IsNullOrWhiteSpace(op.Predicate)) parts.Add("predicate " + PlanFormat.SingleLine(op.Predicate!, 200));

                foreach (var warning in op.Warnings)
                {
                    parts.Add("warning: " + warning);
                }

                return string.Join(", ", parts);
            }
        }

        private static List<string> DescribeWaits(PlanStatement statement) =>
            statement.WaitStats
                .OrderByDescending(w => w.WaitTimeMs)
                .Select(w => $"{w.WaitType}: {PlanFormat.Duration(w.WaitTimeMs)}")
                .ToList();

        private static List<string> DescribeParameters(PlanStatement statement) =>
            statement.Parameters
                .Select(p =>
                {
                    var parts = new List<string> { p.Name ?? "(unnamed)" };

                    if (!string.IsNullOrWhiteSpace(p.CompiledValue)) parts.Add("compiled for " + p.CompiledValue);
                    if (!string.IsNullOrWhiteSpace(p.RuntimeValue)) parts.Add("ran with " + p.RuntimeValue);
                    if (p.CompiledValueDiffers) parts.Add("different from the compiled value");

                    return string.Join(", ", parts);
                })
                .ToList();

        private static List<string> DescribeObjects(PlanStatement statement) =>
            statement.HasPlan
                ? statement.Operators
                    .SelectMany(op => op.Objects)
                    .Select(o => o.ToString())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : new List<string>();

        /// <summary>
        /// The payload as something a person can actually read before deciding to send it.
        ///
        /// Rendered rather than shown as the raw JSON, for the same reason the deadlock viewer does:
        /// JSON escaping turns a plan into one unreadable line, and a preview nobody can read is not a
        /// preview.  Every field that goes on the wire appears here - a test holds that true.
        /// </summary>
        public string ToPreview()
        {
            var preview = new StringBuilder();

            preview.AppendLine("This is everything that would be sent to the AI service.");
            preview.AppendLine("Nothing leaves this machine until you press Submit for analysis.");
            preview.AppendLine();

            Section(preview, "Query identity", $"{Signature} (this query, across the plans it has had)");
            Section(preview, "Plan identity", $"{PlanHash} (this shape of plan for it)");
            Section(preview, "Identities are based on", SignatureComponents);
            Section(preview, "Instance", Instance);
            Section(preview, "File", FileName);

            preview.AppendLine("Statement");
            preview.AppendLine("---------");
            preview.AppendLine(
                "Note: the statement and the plan carry the query as it was compiled or ran, which means " +
                "literal values, and for an actual plan the parameter values it ran with.");
            preview.AppendLine();
            foreach (var line in StatementText.Replace("\r\n", "\n").Split('\n'))
            {
                preview.AppendLine("  " + line);
            }
            if (StatementTruncated) preview.AppendLine("  ... (cut short)");
            preview.AppendLine();

            Section(preview, "Plan context", Context);
            Section(preview, "Statement statistics", Statistics);
            Section(preview, "Findings already established locally", Insights);
            Section(preview, "Missing indexes the optimizer asked for", MissingIndexes);
            Section(preview, "Operators by their own cost", Operators);
            Section(preview, "Waits", Waits);
            Section(preview, "Parameters", Parameters);
            Section(preview, "Objects touched", Objects);

            preview.AppendLine();
            preview.AppendLine("Plan XML");
            preview.AppendLine("--------");

            if (!PlanXmlIncluded)
            {
                preview.AppendLine(PlanXmlOmittedReason);

                return preview.ToString();
            }

            preview.AppendLine(DescribeSize(PlanXml.Length));

            if (PlanXmlWarning is not null)
            {
                preview.AppendLine();
                preview.AppendLine(PlanXmlWarning);
            }

            preview.AppendLine();
            preview.AppendLine(PlanXml);

            return preview.ToString();
        }

        /// <summary>
        /// The first <see cref="MaxListed"/> of a list, saying so when there were more.  The note is
        /// part of the payload rather than dropped quietly: an answer drawn from sixty of two hundred
        /// objects should know it was, and so should the reader of the preview.
        /// </summary>
        private static List<string> Cap(IEnumerable<string> values)
        {
            var all = values.ToList();
            if (all.Count <= MaxListed) return all;

            var capped = all.Take(MaxListed).ToList();
            capped.Add($"... and {all.Count - MaxListed:N0} more, not sent.");

            return capped;
        }

        private static void Add(List<string> lines, string heading, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add($"{heading}: {value}");
        }

        private static void Section(StringBuilder preview, string heading, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            preview.AppendLine($"{heading}: {value}");
            preview.AppendLine();
        }

        private static void Section(StringBuilder preview, string heading, IReadOnlyList<string> values)
        {
            if (values.Count == 0) return;

            preview.AppendLine($"{heading}:");
            foreach (var value in values)
            {
                preview.AppendLine($"  - {value}");
            }
            preview.AppendLine();
        }
    }
}
