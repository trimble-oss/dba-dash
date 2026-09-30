using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Loads the embedded sample plans.
    ///
    /// Embedded rather than copied to the output directory so the tests do not depend on where the
    /// build put them, which is what the deadlock tests do for the same reason.
    /// </summary>
    internal static class TestPlans
    {
        public const string KeyLookupSeek = "KeyLookupSeek";

        public const string ParallelSpill = "ParallelSpill";

        public const string Batch = "Batch";

        /// <summary>
        /// A plan whose compute scalars build one value out of another, and whose filter reports the
        /// same conversion warning five times - what the expression list and the combined card are
        /// for.
        /// </summary>
        public const string Expressions = "Expressions";

        /// <summary>
        /// A predicate on the output of a Concatenation, whose defined value is columns from each
        /// input rather than an expression - the shape a Merge Interval's range seek is fed by.
        /// </summary>
        public const string Concatenation = "Concatenation";

        /// <summary>
        /// A catch-all query - (A = @A OR @A IS NULL) AND (B = @B OR @B IS NULL) - compiled without
        /// OPTION (RECOMPILE), at compatibility level 150.  Constructed rather than captured.
        /// </summary>
        public const string OptionalParameters = "OptionalParameters";

        // The rest are captured from SQL Server 2025 (17.0.1135.8) at compatibility level 170, running
        // procedures over dbo.T (ID, A int NULL, B varchar(50) NULL) with an index on each of A and B.

        /// <summary>
        /// The same query, run with @A = 5 and @B NULL: optional parameter plan optimization compiled
        /// a variant for @A and left the condition on @B in it.
        /// </summary>
        public const string OptionalParametersVariant = "OptionalParametersVariant";

        /// <summary>
        /// The variant above reused from cache for @A = 5, @B = 'b7': compiled with @B NULL, run with
        /// it supplied, so its condition on @B can't seek.
        /// </summary>
        public const string OptionalParametersVariantReused = "OptionalParametersVariantReused";

        /// <summary>A = ISNULL(@A, A) AND B = ISNULL(@B, B), for which no variant is compiled.</summary>
        public const string OptionalParametersIsNull = "OptionalParametersIsNull";

        /// <summary>A = COALESCE(@A, A) AND B = COALESCE(@B, B), for which no variant is compiled.</summary>
        public const string OptionalParametersCoalesce = "OptionalParametersCoalesce";

        /// <summary>The ISNULL query with OPTION (RECOMPILE), @A = 5 and @B NULL: a seek on A, and B = B left.</summary>
        public const string OptionalParametersRecompile = "OptionalParametersRecompile";

        /// <summary>The ISNULL query with OPTION (RECOMPILE), @A = 5 and @B = 'b7': a seek on each.</summary>
        public const string OptionalParametersRecompileSupplied = "OptionalParametersRecompileSupplied";

        // Captured from SQL Server 2022 (16.0.4275.2) against DBADashDB's dbo.CPU, clustered on
        // (InstanceID, EventTime) and partitioned by day on EventTime DATETIME2(3) - 381 partitions.

        /// <summary>EventTime &gt;= CONVERT(datetime, '20260930'): a scan of every partition, with no partition seek.</summary>
        public const string PartitionScanConvert = "PartitionScanConvert";

        /// <summary>EventTime &gt;= @t, @t DATETIME: a scan of every partition, with no partition seek.</summary>
        public const string PartitionScanDatetimeVariable = "PartitionScanDatetimeVariable";

        /// <summary>
        /// EventTime &gt;= @t AND InstanceID = 1, @t DATETIME: a seek on each of the 381 partitions,
        /// from a constant partition range of 1 to 381 and a GetRangeWithMismatchedTypes range on
        /// EventTime.
        /// </summary>
        public const string PartitionSeekDatetimeVariable = "PartitionSeekDatetimeVariable";

        /// <summary>
        /// EventTime &gt;= CONVERT(datetime2(3), '20260930'): a scan whose partition range starts at
        /// RangePartitionNew, reading partitions 367 to 381.
        /// </summary>
        public const string PartitionElimination = "PartitionElimination";

        /// <summary>
        /// SELECT INTO where EventTime &lt; a DATETIME2(3) 100 days ago: a scan whose partition range ends at
        /// RangePartitionNew, reading partitions 1 to 267 - from the first, and still eliminating.
        /// </summary>
        public const string PartitionEliminationLessThan = "PartitionEliminationLessThan";

        /// <summary>The query above as an estimated plan, with no partitions accessed to go by.</summary>
        public const string PartitionEliminationLessThanEstimated = "PartitionEliminationLessThanEstimated";

        public static string Xml(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resource = assembly.GetManifestResourceNames()
                               .FirstOrDefault(n => n.EndsWith("." + name + ".sqlplan", StringComparison.Ordinal))
                           ?? throw new InvalidOperationException(
                               $"The sample plan '{name}' is not embedded.  Available: " +
                               string.Join(", ", assembly.GetManifestResourceNames()));

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static ExecutionPlan Load(string name) => PlanParser.Parse(Xml(name));

        /// <summary>The statement the viewer would open on, which is what most tests are about.</summary>
        public static PlanStatement Statement(string name) =>
            Load(name).PrimaryStatement ?? throw new InvalidOperationException("No statement in " + name);

        /// <summary>The operator with a given node id, so a test can name the one it means.</summary>
        public static PlanOperator Operator(PlanStatement statement, int nodeId) =>
            statement.Operators.FirstOrDefault(o => o.NodeId == nodeId)
            ?? throw new InvalidOperationException($"No operator with node id {nodeId}.");
    }
}
