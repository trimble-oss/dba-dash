using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// The memory grant numbers for a statement, from MemoryGrantInfo.  All in kilobytes, as
    /// showplan reports them.
    ///
    /// Worth keeping together because no single one of them means much: a grant is only excessive
    /// relative to what was used, and only a problem relative to what it waited for.
    /// </summary>
    public sealed class PlanMemoryGrantInfo
    {
        public long? SerialRequiredMemoryKb { get; internal set; }

        public long? SerialDesiredMemoryKb { get; internal set; }

        public long? RequiredMemoryKb { get; internal set; }

        public long? DesiredMemoryKb { get; internal set; }

        /// <summary>What the query asked for.</summary>
        public long? RequestedMemoryKb { get; internal set; }

        /// <summary>What it was given.  Less than requested means it was throttled.</summary>
        public long? GrantedMemoryKb { get; internal set; }

        /// <summary>How much of the grant was actually used.  Only on an actual plan.</summary>
        public long? MaxUsedMemoryKb { get; internal set; }

        /// <summary>The ceiling the server would have allowed.</summary>
        public long? MaxQueryMemoryKb { get; internal set; }

        /// <summary>Milliseconds spent waiting for the grant - RESOURCE_SEMAPHORE time.</summary>
        public long? GrantWaitTimeMs { get; internal set; }

        /// <summary>
        /// The share of the grant that was used, from 0 to 1, or null when either figure is missing.
        /// A low value on a large grant is memory taken from everything else on the server for
        /// nothing, and it is the number the grant warning is really about.
        /// </summary>
        public double? GrantUsedFraction =>
            GrantedMemoryKb is > 0 && MaxUsedMemoryKb is { } used
                ? used / (double)GrantedMemoryKb.Value
                : null;
    }

    /// <summary>Overall timings for a statement, from QueryTimeStats on an actual plan.</summary>
    public sealed class PlanQueryTimeStats
    {
        public long? CpuMs { get; internal set; }

        public long? ElapsedMs { get; internal set; }

        /// <summary>Time inside scalar user defined functions, where SQL Server separated it out.</summary>
        public long? UdfCpuMs { get; internal set; }

        public long? UdfElapsedMs { get; internal set; }
    }

    /// <summary>
    /// One statement from a batch, with its plan.
    ///
    /// Showplan nests the plan inside the statement and the statement inside a batch; those levels
    /// are flattened here because a statement has exactly one plan and the extra hops buy nothing.
    /// A statement with no plan at all - a control flow statement, or one showplan recorded without
    /// one - still appears, with <see cref="RootOperator"/> null, because the statement text is
    /// often the only reason the reader opened the file.
    /// </summary>
    public sealed class PlanStatement
    {
        /// <summary>Showplan's statement id within the batch, where it gave one.</summary>
        public int? StatementId { get; internal set; }

        /// <summary>The SQL text, exactly as showplan recorded it.</summary>
        public string? StatementText { get; internal set; }

        /// <summary>SELECT, INSERT, UPDATE, COND WITH QUERY, and the rest.</summary>
        public string? StatementType { get; internal set; }

        /// <summary>
        /// This statement's own showplan element, re-serialised without formatting.
        ///
        /// The document a plan arrives in can hold a whole batch, and one statement is the unit
        /// anything reasoning about a plan works on - an AI analysis sends this rather than the
        /// document, so the model is not handed nine other statements and asked about the tenth.
        /// </summary>
        public string? Xml { get; internal set; }

        /// <summary>The whole statement's estimated cost, which every percentage is taken against.</summary>
        public double StatementSubTreeCost { get; internal set; }

        public double? StatementEstRows { get; internal set; }

        /// <summary>TRIVIAL or FULL - a trivial plan was never costed or considered.</summary>
        public string? OptimisationLevel { get; internal set; }

        /// <summary>
        /// Why the optimiser stopped early - a time out or a memory limit - which means the plan it
        /// settled on may not be the best one it could have found.
        /// </summary>
        public string? OptimisationEarlyAbortReason { get; internal set; }

        public string? QueryHash { get; internal set; }

        public string? QueryPlanHash { get; internal set; }

        /// <summary>True when this plan came out of the cache rather than being compiled now.</summary>
        public bool? RetrievedFromCache { get; internal set; }

        /// <summary>The cardinality estimator version, which explains a lot of estimate differences.</summary>
        public string? CardinalityEstimationModelVersion { get; internal set; }

        /// <summary>
        /// How deeply nested the statement was in the batch - a statement inside the THEN of a
        /// conditional is one deeper than the conditional.  Kept so the statement list can be
        /// indented rather than presented flat, which loses the structure entirely.
        /// </summary>
        public int NestingLevel { get; internal set; }

        // ---------------------------------------------------------------- plan level

        /// <summary>
        /// The head of the operator tree, or null for a statement showplan recorded without a plan.
        /// </summary>
        public PlanOperator? RootOperator { get; internal set; }

        public bool HasPlan => RootOperator is not null;

        /// <summary>
        /// The DOP the plan was built for.  1 means serial; a parallel plan that ran at DOP 1 is a
        /// different thing and shows up in the runtime counters, not here.
        /// </summary>
        public int? DegreeOfParallelism { get; internal set; }

        public long? CachedPlanSizeKb { get; internal set; }

        public long? CompileTimeMs { get; internal set; }

        public long? CompileCpuMs { get; internal set; }

        public long? CompileMemoryKb { get; internal set; }

        /// <summary>
        /// Why the plan could not go parallel, where the optimiser said.  Often the answer to "why
        /// is this single threaded".
        /// </summary>
        public string? NonParallelPlanReason { get; internal set; }

        public PlanMemoryGrantInfo? MemoryGrant { get; internal set; }

        public PlanQueryTimeStats? QueryTimeStats { get; internal set; }

        /// <summary>
        /// True when every operator's reported elapsed and CPU time is its own, row mode included.
        /// SQL Server 2022 can report times this way and says so on the plan.  Otherwise a row mode
        /// operator's time includes its inputs' - see <see cref="PlanOperatorTiming"/>.
        /// </summary>
        public bool? ExclusiveProfileTimeActive { get; internal set; }

        /// <summary>Threads reserved and used, from ThreadStat on a parallel plan.</summary>
        public int? ReservedThreads { get; internal set; }

        public int? UsedThreads { get; internal set; }

        /// <summary>Where the query spent its time waiting.  Only on an actual plan.</summary>
        public IReadOnlyList<PlanWaitStat> WaitStats { get; internal set; } = [];

        /// <summary>The parameters the plan was compiled with, and ran with.</summary>
        public IReadOnlyList<PlanParameter> Parameters { get; internal set; } = [];

        /// <summary>
        /// Indexes the optimiser says it wanted, highest impact first.
        /// </summary>
        public IReadOnlyList<PlanMissingIndex> MissingIndexes { get; internal set; } = [];

        /// <summary>Warnings on the plan as a whole, as opposed to on one operator.</summary>
        public IReadOnlyList<PlanWarning> Warnings { get; internal set; } = [];

        /// <summary>Set options, trace flags and the rest, for the properties panel.</summary>
        public IReadOnlyList<PlanProperty> Properties { get; internal set; } = [];

        // ---------------------------------------------------------------- derived

        /// <summary>Every operator in the statement, parents before children.  Empty with no plan.</summary>
        public IEnumerable<PlanOperator> Operators =>
            RootOperator?.DescendantsAndSelf() ?? Enumerable.Empty<PlanOperator>();

        /// <summary>
        /// Every warning in the statement - the plan's own and every operator's - which is what the
        /// warnings list shows.  Ordered by severity so the ones that matter are not below a note
        /// about a missing statistic.
        /// </summary>
        public IEnumerable<PlanWarning> AllWarnings =>
            Warnings
                .Concat(Operators.SelectMany(o => o.Warnings))
                .OrderByDescending(w => w.Severity);

        /// <summary>
        /// The missing indexes on the table <paramref name="op"/> reads, best first.  Recommendations
        /// are made for the statement, but each is about one table, and the operator reading it is
        /// where the reader needs to look - so the badge, the tooltip and the cards all match them to
        /// operators this one way.
        /// </summary>
        public IEnumerable<PlanMissingIndex> MissingIndexesFor(PlanOperator op) =>
            op.PrimaryObject is { } target && !string.IsNullOrEmpty(target.QualifiedTableName)
                ? MissingIndexes
                    .Where(index => string.Equals(index.QualifiedTableName, target.QualifiedTableName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(index => index.Impact)
                : Enumerable.Empty<PlanMissingIndex>();

        /// <summary>
        /// True when the plan carries measurements rather than only estimates.  Asked of every
        /// operator rather than the root, because the root is often a Compute Scalar, and SQL Server
        /// leaves those unmeasured when their work is deferred.
        /// </summary>
        public bool IsActualPlan => Operators.Any(o => o.HasRuntime);

        /// <summary>
        /// A short caption for the statement selector: the type, then as much of the text as fits.
        /// </summary>
        public string Caption
        {
            get
            {
                var text = string.IsNullOrWhiteSpace(StatementText)
                    ? null
                    : PlanFormat.SingleLine(StatementText, 100);

                var type = string.IsNullOrWhiteSpace(StatementType) ? "Statement" : StatementType;
                return text is null ? type : type + " - " + text;
            }
        }

        public override string ToString() => Caption;
    }
}
