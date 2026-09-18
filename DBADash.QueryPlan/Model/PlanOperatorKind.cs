namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// The operators worth telling apart at a glance, which is a coarser list than SQL Server's
    /// physical operators.
    ///
    /// The point of this enum is the icon: a reader scanning a plan is asking "where is the scan,
    /// where is the sort, where did it go parallel", and a distinct shape answers that faster than
    /// reading the label.  Operators nobody distinguishes by shape share a kind, and
    /// <see cref="PlanOperator.PhysicalOp"/> still carries the exact name for the label, the tooltip
    /// and the properties.
    ///
    /// Several kinds are not a physical operator at all but a combination that reads as its own
    /// thing: a Hash Match is a join or an aggregate depending on its logical operation, and a
    /// Clustered Index Seek doing a lookup is the key lookup everyone hunts for.
    /// </summary>
    public enum PlanOperatorKind
    {
        /// <summary>A physical operator we have no shape for.  Drawn with a neutral glyph.</summary>
        Unknown,

        /// <summary>
        /// The synthetic node at the head of the tree, carrying the statement type.  Not a RelOp -
        /// SQL Server does not emit one - but a plan reads as a pipeline ending somewhere, and
        /// without it the outermost operator has an arrow pointing into nothing.
        /// </summary>
        StatementRoot,

        // ---------------------------------------------------------------- reading data

        TableScan,
        ClusteredIndexScan,
        NonClusteredIndexScan,
        ColumnstoreIndexScan,
        ClusteredIndexSeek,
        NonClusteredIndexSeek,

        /// <summary>A seek on the base table to fetch columns an index did not cover.</summary>
        KeyLookup,

        /// <summary>The heap equivalent of a key lookup.</summary>
        RidLookup,

        /// <summary>Constant Scan, and the other scans over something that is not a user table.</summary>
        ConstantScan,

        RemoteQuery,

        /// <summary>Table Valued Function, and the inline function operators.</summary>
        TableValuedFunction,

        // ---------------------------------------------------------------- combining rows

        NestedLoops,
        HashMatchJoin,
        MergeJoin,
        AdaptiveJoin,
        Concatenation,
        MergeInterval,

        /// <summary>Nested Loops' correlated cousin, drawn the same way but labelled Apply.</summary>
        Apply,

        // ---------------------------------------------------------------- reshaping rows

        StreamAggregate,
        HashMatchAggregate,
        Sort,
        TopSort,
        Top,
        Filter,
        ComputeScalar,
        Segment,
        SequenceProject,
        WindowAggregate,
        Split,
        Collapse,
        Switch,
        Sequence,
        Assert,
        Bitmap,

        // ---------------------------------------------------------------- caching rows

        TableSpool,
        IndexSpool,
        RowCountSpool,
        WindowSpool,

        // ---------------------------------------------------------------- parallelism

        GatherStreams,
        RepartitionStreams,
        DistributeStreams,

        // ---------------------------------------------------------------- writing data

        Insert,
        Update,
        Delete,
        Merge,

        /// <summary>The wrapper operator over a set of per-index modifications.</summary>
        ClusteredUpdate,

        /// <summary>Foreign key or check constraint validation inside a modification plan.</summary>
        ConstraintCheck,

        // ---------------------------------------------------------------- cursors and flow

        Cursor,
        LanguageConstruct,
        UdxOperator
    }

    /// <summary>
    /// The colour family an operator is drawn in.  Coarser than <see cref="PlanOperatorKind"/>
    /// because colour carries less information than shape before it becomes noise: seven or eight
    /// families are recognisable, forty are not.
    /// </summary>
    public enum PlanOperatorCategory
    {
        /// <summary>The statement root.</summary>
        Root,

        /// <summary>Anything that reads rows from storage.</summary>
        DataAccess,

        /// <summary>Joins and the other operators that bring two inputs together.</summary>
        Join,

        /// <summary>Aggregation, sorting, windowing - operators that reshape a stream.</summary>
        Transform,

        /// <summary>Spools, which cache rows and are usually worth a second look.</summary>
        Spool,

        /// <summary>Exchange operators.</summary>
        Parallelism,

        /// <summary>Inserts, updates, deletes and merges.</summary>
        DataModification,

        /// <summary>Cheap per-row work: compute scalar, filter, assert, bitmap.</summary>
        Compute,

        /// <summary>Everything else.</summary>
        Other
    }
}
