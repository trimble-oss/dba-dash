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

        // Captured from SQL Server 2025 (17.0.4025.3, Express) on LocalDB, over dbo.Nums (N int primary
        // key, Grp int, Pad char(200)) holding 200,000 rows.  @t is (N int primary key, Grp int), filled
        // with the 50,000 rows N <= 50000, and the query reads Grp = 5 (486 rows) joined back to Nums.
        // dbo.MSTVF(@Max) is a multi-statement function returning the same rows N <= @Max.

        /// <summary>The read of @t at compatibility level 170: deferred compilation knew it held 50,000 rows.</summary>
        public const string TableVariableDeferred = "TableVariableDeferred";

        /// <summary>The same read at compatibility level 140: compiled while @t was empty, so costed for one row.</summary>
        public const string TableVariableCompiledEmpty = "TableVariableCompiledEmpty";

        /// <summary>INSERT @t SELECT ...: serial, with the table variable non-parallel reason.</summary>
        public const string TableVariableInsert = "TableVariableInsert";

        /// <summary>The DECLARE, INSERT and SELECT batch as an estimated plan at level 170: the read is costed for one row.</summary>
        public const string TableVariableEstimated = "TableVariableEstimated";

        /// <summary>dbo.MSTVF(50000) joined to Nums at level 170: interleaved execution ran it first.</summary>
        public const string FunctionInterleaved = "FunctionInterleaved";

        /// <summary>The same query run again from cache: still flagged as interleaved, still compiled for 50,000 rows.</summary>
        public const string FunctionInterleavedReused = "FunctionInterleavedReused";

        /// <summary>
        /// dbo.MSTVF(40000) grouped, at level 170, read from the plan cache: interleaved when it was
        /// compiled, so estimated at 40,000 rows, with no run figures and nothing else to say so.
        /// </summary>
        public const string FunctionCachedInterleaved = "FunctionCachedInterleaved";

        /// <summary>The same query at level 130: no interleaved execution, so the fixed guess of 100 rows.</summary>
        public const string FunctionFixedGuess = "FunctionFixedGuess";

        /// <summary>The same query as an estimated plan at level 170, with the function body's statements.</summary>
        public const string FunctionEstimated = "FunctionEstimated";

        /// <summary>
        /// (VALUES (1000), (2000)) CROSS APPLY dbo.MSTVF(v.m) at level 170: a lone Table-valued function
        /// operator run once per outer row, not interleaved.
        /// </summary>
        public const string FunctionCrossApply = "FunctionCrossApply";

        /// <summary>
        /// STRING_SPLIT('1,2,3,4,5', ',') joined to Nums: a built-in table-valued function, estimated
        /// at its fixed guess of 50 rows, returning 5.
        /// </summary>
        public const string StringSplit = "StringSplit";

        /// <summary>
        /// sys.dm_exec_requests CROSS APPLY sys.dm_exec_sql_text: two DMVs, SYSREQUESTS returning its
        /// estimated 77 rows, and FNGETSQL run once for each, at a row a call, returning 1 row in all.
        /// </summary>
        public const string BuiltInFunctions = "BuiltInFunctions";

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
