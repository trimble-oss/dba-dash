using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// A broad family of operators to filter the operator list by: "everything that reads a table",
    /// "the sorts", "whatever needs memory".
    ///
    /// Coarser than <see cref="PlanOperatorKind"/>, which has forty-odd members, and not the same as
    /// <see cref="PlanOperatorCategory"/>, which is the colour family.  The colour families put every
    /// operator in exactly one place; a filter is a question, and the questions overlap - a seek is
    /// reading data and is also a seek, and a hash join is a join that also needs a memory grant.  So
    /// an operator can be in several groups, and some operators are in none.
    /// </summary>
    public sealed class PlanOperatorGroup
    {
        private readonly Func<PlanOperator, bool> _contains;

        private PlanOperatorGroup(string name, string description, Func<PlanOperator, bool> contains)
        {
            Name = name;
            Description = description;
            _contains = contains;
        }

        /// <summary>What the filter is called on the menu.</summary>
        public string Name { get; }

        /// <summary>What it picks out, for the menu's tooltip.</summary>
        public string Description { get; }

        public bool Contains(PlanOperator op) => _contains(op ?? throw new ArgumentNullException(nameof(op)));

        public override string ToString() => Name;

        public static readonly PlanOperatorGroup ReadingData = new(
            "Reading Data",
            "Scans, seeks and lookups, and the other operators that read rows from a table, index, function or remote server.",
            op => op.Category == PlanOperatorCategory.DataAccess && op.Kind != PlanOperatorKind.ConstantScan);

        public static readonly PlanOperatorGroup Scans = new(
            "Scans",
            "Table, clustered index, nonclustered index and columnstore scans.",
            op => op.Kind is PlanOperatorKind.TableScan
                or PlanOperatorKind.ClusteredIndexScan
                or PlanOperatorKind.NonClusteredIndexScan
                or PlanOperatorKind.ColumnstoreIndexScan);

        public static readonly PlanOperatorGroup Seeks = new(
            "Seeks",
            "Clustered and nonclustered index seeks, not counting the seeks that are key lookups.",
            op => op.Kind is PlanOperatorKind.ClusteredIndexSeek or PlanOperatorKind.NonClusteredIndexSeek);

        public static readonly PlanOperatorGroup Lookups = new(
            "Lookups",
            "Key and RID lookups: a trip back to the table for columns the index did not have.",
            op => op.Kind is PlanOperatorKind.KeyLookup or PlanOperatorKind.RidLookup);

        public static readonly PlanOperatorGroup Joins = new(
            "Joins",
            "Nested loops, hash, merge and adaptive joins, and the other operators that combine inputs.",
            op => op.Category == PlanOperatorCategory.Join);

        public static readonly PlanOperatorGroup Aggregates = new(
            "Aggregates",
            "Stream, hash and window aggregates.",
            op => op.Kind is PlanOperatorKind.StreamAggregate
                or PlanOperatorKind.HashMatchAggregate
                or PlanOperatorKind.WindowAggregate);

        public static readonly PlanOperatorGroup Sorts = new(
            "Sorts",
            "Sorts, including Top N sorts.",
            op => op.Kind is PlanOperatorKind.Sort or PlanOperatorKind.TopSort);

        public static readonly PlanOperatorGroup Spools = new(
            "Spools",
            "Table, index, row count and window spools.",
            op => op.Category == PlanOperatorCategory.Spool);

        public static readonly PlanOperatorGroup Parallelism = new(
            "Parallelism",
            "Gather, repartition and distribute streams.",
            op => op.Category == PlanOperatorCategory.Parallelism);

        public static readonly PlanOperatorGroup ChangingData = new(
            "Changing Data",
            "Inserts, updates, deletes and merges, and the constraint checks that go with them.",
            op => op.Category == PlanOperatorCategory.DataModification);

        public static readonly PlanOperatorGroup Computing = new(
            "Computing and Filtering",
            "Compute scalars, filters, asserts, bitmaps and the other per-row work.",
            op => op.Category == PlanOperatorCategory.Compute);

        public static readonly PlanOperatorGroup MemoryConsuming = new(
            "Using Memory",
            "Operators with a share of the memory grant, such as sorts and hashes.",
            op => op.MemoryFractionInput is > 0 || op.MemoryFractionOutput is > 0 || op.Runtime?.UsedMemoryGrantKb is > 0);

        /// <summary>Every group, in the order the menu offers them.</summary>
        public static IReadOnlyList<PlanOperatorGroup> All { get; } =
        [
            ReadingData, Scans, Seeks, Lookups, Joins, Aggregates, Sorts, Spools, Parallelism, ChangingData, Computing, MemoryConsuming
        ];

        /// <summary>The groups <paramref name="op"/> belongs to - none, one or several.</summary>
        public static IEnumerable<PlanOperatorGroup> Of(PlanOperator op) => All.Where(group => group.Contains(op));
    }
}
