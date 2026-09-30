using System.Collections.Generic;

namespace DBADash.QueryPlan.Model
{
    /// <summary>A contiguous run of partition numbers, first and last inclusive.</summary>
    public readonly record struct PlanPartitionRange(int Start, int End)
    {
        public int Count => End - Start + 1;
    }

    /// <summary>
    /// Which partitions a partitioned operator reads, and whether it chose them from the query's
    /// values.
    ///
    /// Showplan never names the partitioning column, so this is read from the seek on the
    /// operator's partition id (PtnId1000 and the like), which is how SQL Server picks partitions:
    /// <list type="bullet">
    /// <item>No seek on a partition id: every partition is read.</item>
    /// <item>A bound worked out from a value - RangePartitionNew(value, ...) - partitions are
    /// eliminated as the value allows, at run time.</item>
    /// <item>Constant bounds: from 1 is what a seek with nothing to eliminate by gets, 1 to the
    /// last partition; a literal compiled into the plan can give constants too, so a range starting
    /// after 1 is elimination done at compile time.  One from 1 that stops short of the last
    /// partition looks the same as one that doesn't, as the plan doesn't say how many there are.</item>
    /// </list>
    /// </summary>
    public sealed class PlanPartitionAccess
    {
        /// <summary>True when the operator has a seek on its partition id at all.</summary>
        public bool HasPartitionSeek { get; internal set; }

        /// <summary>True when a bound of that seek is worked out from a value rather than a constant.</summary>
        public bool HasDynamicBound { get; internal set; }

        /// <summary>
        /// The partition id range when both bounds are constants.  Null when there is no partition
        /// seek, or a bound comes from a value.
        /// </summary>
        public PlanPartitionRange? ConstantRange { get; internal set; }

        /// <summary>How many partitions were read, from an actual plan.  Null on an estimated plan.</summary>
        public int? PartitionsAccessed { get; internal set; }

        /// <summary>The partitions read, from an actual plan.  Empty on an estimated plan.</summary>
        public IReadOnlyList<PlanPartitionRange> AccessedRanges { get; internal set; } = [];

        /// <summary>
        /// True when partitions were, or could be, left out by the query's values: a bound worked out
        /// from a value, a constant range that starts after the first partition, or an actual run that
        /// skipped some at the start or in the middle.
        /// </summary>
        public bool IsEliminated =>
            HasDynamicBound
            || ConstantRange is { Start: > 1 }
            || AccessedRanges.Count > 1
            || (AccessedRanges.Count == 1 && AccessedRanges[0].Start > 1);

        /// <summary>
        /// True when it is certain every partition is read: there is no seek on the partition id to
        /// leave any out.  A constant range from 1 is very likely every partition, but not certain.
        /// </summary>
        public bool ReadsEveryPartition => !HasPartitionSeek && !IsEliminated;
    }
}
