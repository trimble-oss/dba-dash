using System;
using System.Collections.Generic;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// Works out which <see cref="PlanOperatorKind"/> an operator is, from the physical and logical
    /// operator names SQL Server emits.
    ///
    /// Most of this is a lookup on the physical operator, but a handful need the logical one too: a
    /// Hash Match is a join or an aggregate, and an index seek doing a lookup is the key lookup
    /// people go looking for.  Unrecognised names come back as <see cref="PlanOperatorKind.Unknown"/>
    /// rather than throwing - new operators arrive with every release, and a plan containing one is
    /// still worth drawing.
    /// </summary>
    public static class PlanOperatorClassifier
    {
        private static readonly Dictionary<string, PlanOperatorKind> PhysicalOps =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Table Scan"] = PlanOperatorKind.TableScan,
                ["Clustered Index Scan"] = PlanOperatorKind.ClusteredIndexScan,
                ["Index Scan"] = PlanOperatorKind.NonClusteredIndexScan,
                ["Columnstore Index Scan"] = PlanOperatorKind.ColumnstoreIndexScan,
                ["Clustered Index Seek"] = PlanOperatorKind.ClusteredIndexSeek,
                ["Index Seek"] = PlanOperatorKind.NonClusteredIndexSeek,
                ["RID Lookup"] = PlanOperatorKind.RidLookup,
                ["JSON Index Seek"] = PlanOperatorKind.NonClusteredIndexSeek,
                ["Key Lookup"] = PlanOperatorKind.KeyLookup,
                ["Constant Scan"] = PlanOperatorKind.ConstantScan,
                ["Deleted Scan"] = PlanOperatorKind.ConstantScan,
                ["Inserted Scan"] = PlanOperatorKind.ConstantScan,
                ["Parameter Table Scan"] = PlanOperatorKind.ConstantScan,
                ["Log Row Scan"] = PlanOperatorKind.ConstantScan,
                ["Table-valued function"] = PlanOperatorKind.TableValuedFunction,
                ["Table Valued Function"] = PlanOperatorKind.TableValuedFunction,
                ["Remote Query"] = PlanOperatorKind.RemoteQuery,
                ["Put"] = PlanOperatorKind.RemoteQuery,
                ["Remote Scan"] = PlanOperatorKind.RemoteQuery,
                ["Remote Index Scan"] = PlanOperatorKind.RemoteQuery,
                ["Remote Index Seek"] = PlanOperatorKind.RemoteQuery,
                ["Remote Insert"] = PlanOperatorKind.RemoteQuery,
                ["Remote Update"] = PlanOperatorKind.RemoteQuery,
                ["Remote Delete"] = PlanOperatorKind.RemoteQuery,

                ["Nested Loops"] = PlanOperatorKind.NestedLoops,
                ["Merge Join"] = PlanOperatorKind.MergeJoin,
                ["Adaptive Join"] = PlanOperatorKind.AdaptiveJoin,
                ["Concatenation"] = PlanOperatorKind.Concatenation,
                ["Merge Interval"] = PlanOperatorKind.MergeInterval,

                ["Stream Aggregate"] = PlanOperatorKind.StreamAggregate,
                ["Sort"] = PlanOperatorKind.Sort,
                ["Top"] = PlanOperatorKind.Top,
                ["Filter"] = PlanOperatorKind.Filter,
                ["Compute Scalar"] = PlanOperatorKind.ComputeScalar,
                ["Segment"] = PlanOperatorKind.Segment,
                ["Sequence Project"] = PlanOperatorKind.SequenceProject,
                ["Window Aggregate"] = PlanOperatorKind.WindowAggregate,
                ["Window Spool"] = PlanOperatorKind.WindowSpool,
                ["Split"] = PlanOperatorKind.Split,
                ["Collapse"] = PlanOperatorKind.Collapse,
                ["Switch"] = PlanOperatorKind.Switch,
                ["Sequence"] = PlanOperatorKind.Sequence,
                ["Assert"] = PlanOperatorKind.Assert,
                ["Bitmap"] = PlanOperatorKind.Bitmap,
                ["Batch Hash Table Build"] = PlanOperatorKind.Bitmap,

                ["Table Spool"] = PlanOperatorKind.TableSpool,
                ["Index Spool"] = PlanOperatorKind.IndexSpool,
                ["Row Count Spool"] = PlanOperatorKind.RowCountSpool,

                ["Insert"] = PlanOperatorKind.Insert,
                ["Table Insert"] = PlanOperatorKind.Insert,
                ["Clustered Index Insert"] = PlanOperatorKind.Insert,
                ["Index Insert"] = PlanOperatorKind.Insert,
                ["Columnstore Index Insert"] = PlanOperatorKind.Insert,
                ["JSON Index Insert"] = PlanOperatorKind.Insert,
                ["Online Index Insert"] = PlanOperatorKind.Insert,
                ["Update"] = PlanOperatorKind.Update,
                ["Table Update"] = PlanOperatorKind.Update,
                ["Clustered Index Update"] = PlanOperatorKind.Update,
                ["Index Update"] = PlanOperatorKind.Update,
                ["Columnstore Index Update"] = PlanOperatorKind.Update,
                ["JSON Index Update"] = PlanOperatorKind.Update,
                ["Delete"] = PlanOperatorKind.Delete,
                ["Table Delete"] = PlanOperatorKind.Delete,
                ["Clustered Index Delete"] = PlanOperatorKind.Delete,
                ["Index Delete"] = PlanOperatorKind.Delete,
                ["Columnstore Index Delete"] = PlanOperatorKind.Delete,
                ["JSON Index Delete"] = PlanOperatorKind.Delete,
                ["Table Merge"] = PlanOperatorKind.Merge,
                ["Clustered Index Merge"] = PlanOperatorKind.Merge,
                ["Index Merge"] = PlanOperatorKind.Merge,
                ["Columnstore Index Merge"] = PlanOperatorKind.Merge,
                ["Clustered Update"] = PlanOperatorKind.ClusteredUpdate,
                ["Foreign Key References Check"] = PlanOperatorKind.ConstraintCheck,

                ["UDX"] = PlanOperatorKind.UdxOperator,
                ["Print"] = PlanOperatorKind.LanguageConstruct,
                ["Declare"] = PlanOperatorKind.LanguageConstruct,
                ["Assign"] = PlanOperatorKind.LanguageConstruct,
                ["If"] = PlanOperatorKind.LanguageConstruct,
                ["Language Element"] = PlanOperatorKind.LanguageConstruct,
                ["Fetch Query"] = PlanOperatorKind.Cursor,
                ["Population Query"] = PlanOperatorKind.Cursor,
                ["Refresh Query"] = PlanOperatorKind.Cursor,
                ["Dynamic"] = PlanOperatorKind.Cursor,
                ["Keyset"] = PlanOperatorKind.Cursor,
                ["Snapshot"] = PlanOperatorKind.Cursor,
                ["Fast Forward"] = PlanOperatorKind.Cursor
            };

        /// <summary>
        /// Classify an operator.  <paramref name="isLookup"/> comes from the IndexScan element's
        /// Lookup attribute, which is the only thing separating a key lookup from an ordinary seek.
        /// <paramref name="storage"/> is the Storage attribute of the operator's object, which is the
        /// only thing separating a columnstore scan from a rowstore one: showplan calls both Index
        /// Scan or Clustered Index Scan, and "Columnstore Index Scan" is a name the tools give it.
        /// </summary>
        public static PlanOperatorKind Classify(string? physicalOp, string? logicalOp, bool isLookup = false, string? storage = null)
        {
            if (string.IsNullOrWhiteSpace(physicalOp)) return PlanOperatorKind.Unknown;

            // Hash Match does two unrelated jobs and the logical operator says which.  Nobody reads
            // the name and pictures the same thing for both, so they get separate shapes.
            if (physicalOp.Equals("Hash Match", StringComparison.OrdinalIgnoreCase))
            {
                return IsAggregate(logicalOp) ? PlanOperatorKind.HashMatchAggregate : PlanOperatorKind.HashMatchJoin;
            }

            // A sort that is only there to feed a Top is worth its own shape: the fix is usually an
            // index, whereas a plain sort is often just what the query asked for.
            if (physicalOp.Equals("Sort", StringComparison.OrdinalIgnoreCase) &&
                logicalOp is not null &&
                logicalOp.Contains("TopN", StringComparison.OrdinalIgnoreCase))
            {
                return PlanOperatorKind.TopSort;
            }

            // Nested Loops presented as an Apply - a correlated join over a function or a subquery.
            if (physicalOp.Equals("Nested Loops", StringComparison.OrdinalIgnoreCase) &&
                logicalOp is not null &&
                logicalOp.Contains("Apply", StringComparison.OrdinalIgnoreCase))
            {
                return PlanOperatorKind.Apply;
            }

            // The exchange operators are all PhysicalOp="Parallelism" and differ only in the logical
            // operator, and which one it is changes what you would do about it.
            if (physicalOp.Equals("Parallelism", StringComparison.OrdinalIgnoreCase))
            {
                if (logicalOp is null) return PlanOperatorKind.RepartitionStreams;
                if (logicalOp.Contains("Gather", StringComparison.OrdinalIgnoreCase)) return PlanOperatorKind.GatherStreams;
                if (logicalOp.Contains("Distribute", StringComparison.OrdinalIgnoreCase)) return PlanOperatorKind.DistributeStreams;
                return PlanOperatorKind.RepartitionStreams;
            }

            if (!PhysicalOps.TryGetValue(physicalOp, out var kind)) return PlanOperatorKind.Unknown;

            // A seek fetching columns an index did not cover is a key lookup, and that is the thing
            // the reader is hunting for rather than "another seek".
            if (isLookup &&
                kind is PlanOperatorKind.ClusteredIndexSeek or PlanOperatorKind.NonClusteredIndexSeek)
            {
                return PlanOperatorKind.KeyLookup;
            }

            if (kind is PlanOperatorKind.ClusteredIndexScan or PlanOperatorKind.NonClusteredIndexScan &&
                string.Equals(storage, "ColumnStore", StringComparison.OrdinalIgnoreCase))
            {
                return PlanOperatorKind.ColumnstoreIndexScan;
            }

            return kind;
        }

        /// <summary>The colour family for a kind.</summary>
        public static PlanOperatorCategory CategoryOf(PlanOperatorKind kind) => kind switch
        {
            PlanOperatorKind.StatementRoot => PlanOperatorCategory.Root,

            PlanOperatorKind.TableScan
                or PlanOperatorKind.ClusteredIndexScan
                or PlanOperatorKind.NonClusteredIndexScan
                or PlanOperatorKind.ColumnstoreIndexScan
                or PlanOperatorKind.ClusteredIndexSeek
                or PlanOperatorKind.NonClusteredIndexSeek
                or PlanOperatorKind.KeyLookup
                or PlanOperatorKind.RidLookup
                or PlanOperatorKind.ConstantScan
                or PlanOperatorKind.RemoteQuery
                or PlanOperatorKind.TableValuedFunction => PlanOperatorCategory.DataAccess,

            PlanOperatorKind.NestedLoops
                or PlanOperatorKind.HashMatchJoin
                or PlanOperatorKind.MergeJoin
                or PlanOperatorKind.AdaptiveJoin
                or PlanOperatorKind.Apply
                or PlanOperatorKind.Concatenation
                or PlanOperatorKind.MergeInterval => PlanOperatorCategory.Join,

            PlanOperatorKind.StreamAggregate
                or PlanOperatorKind.HashMatchAggregate
                or PlanOperatorKind.Sort
                or PlanOperatorKind.TopSort
                or PlanOperatorKind.Top
                or PlanOperatorKind.Segment
                or PlanOperatorKind.SequenceProject
                or PlanOperatorKind.WindowAggregate => PlanOperatorCategory.Transform,

            PlanOperatorKind.TableSpool
                or PlanOperatorKind.IndexSpool
                or PlanOperatorKind.RowCountSpool
                or PlanOperatorKind.WindowSpool => PlanOperatorCategory.Spool,

            PlanOperatorKind.GatherStreams
                or PlanOperatorKind.RepartitionStreams
                or PlanOperatorKind.DistributeStreams => PlanOperatorCategory.Parallelism,

            PlanOperatorKind.Insert
                or PlanOperatorKind.Update
                or PlanOperatorKind.Delete
                or PlanOperatorKind.Merge
                or PlanOperatorKind.ClusteredUpdate
                or PlanOperatorKind.ConstraintCheck => PlanOperatorCategory.DataModification,

            PlanOperatorKind.Filter
                or PlanOperatorKind.ComputeScalar
                or PlanOperatorKind.Assert
                or PlanOperatorKind.Bitmap
                or PlanOperatorKind.Split
                or PlanOperatorKind.Collapse => PlanOperatorCategory.Compute,

            _ => PlanOperatorCategory.Other
        };

        /// <summary>What a colour family is called to a reader - the heading the operator reference and the legend group by.</summary>
        public static string CategoryName(PlanOperatorCategory category) => category switch
        {
            PlanOperatorCategory.Root => "Statement",
            PlanOperatorCategory.DataAccess => "Reading data",
            PlanOperatorCategory.Join => "Joins",
            PlanOperatorCategory.Transform => "Aggregating, sorting and shaping",
            PlanOperatorCategory.Spool => "Spools",
            PlanOperatorCategory.Parallelism => "Parallelism",
            PlanOperatorCategory.DataModification => "Changing data",
            PlanOperatorCategory.Compute => "Computing and checking",
            _ => "Other"
        };

        /// <summary>
        /// True when the logical operator names an aggregation, which is what separates the two jobs
        /// a Hash Match does.
        /// </summary>
        private static bool IsAggregate(string? logicalOp) =>
            logicalOp is not null &&
            (logicalOp.Contains("Aggregate", StringComparison.OrdinalIgnoreCase) ||
             logicalOp.Contains("Distinct", StringComparison.OrdinalIgnoreCase) ||
             logicalOp.Contains("Union", StringComparison.OrdinalIgnoreCase) ||
             logicalOp.Contains("Group", StringComparison.OrdinalIgnoreCase));
    }
}
