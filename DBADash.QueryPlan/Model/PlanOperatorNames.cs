namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// The caption for each <see cref="PlanOperatorKind"/>.
    ///
    /// Mostly the same as the physical operator name, with three deliberate differences: a Hash
    /// Match says whether it is joining or aggregating, a lookup says Key Lookup rather than
    /// Clustered Index Seek, and the exchange operators name which exchange they are.  In each case
    /// showplan's own name hides the thing the reader is looking for.
    ///
    /// Where one kind covers several physical operators - a JSON Index Seek drawn as an index seek, a
    /// Foreign Key References Check as a constraint check - the operator's own name is kept, so the
    /// kind only ever chooses the icon and never renames what the plan said.
    /// </summary>
    public static class PlanOperatorNames
    {
        public static string For(PlanOperatorKind kind, string? physicalOp) => kind switch
        {
            PlanOperatorKind.StatementRoot => "Query",

            PlanOperatorKind.TableScan => "Table Scan",
            PlanOperatorKind.ClusteredIndexScan => "Clustered Index Scan",
            PlanOperatorKind.NonClusteredIndexScan => "Index Scan",
            PlanOperatorKind.ColumnstoreIndexScan => "Columnstore Index Scan",
            PlanOperatorKind.ClusteredIndexSeek => "Clustered Index Seek",
            PlanOperatorKind.NonClusteredIndexSeek => physicalOp ?? "Index Seek",
            PlanOperatorKind.KeyLookup => "Key Lookup",
            PlanOperatorKind.RidLookup => "RID Lookup",
            PlanOperatorKind.ConstantScan => physicalOp ?? "Constant Scan",
            PlanOperatorKind.RemoteQuery => physicalOp ?? "Remote Query",
            PlanOperatorKind.TableValuedFunction => "Table Valued Function",

            PlanOperatorKind.NestedLoops => "Nested Loops",
            PlanOperatorKind.HashMatchJoin => "Hash Match (Join)",
            PlanOperatorKind.MergeJoin => "Merge Join",
            PlanOperatorKind.AdaptiveJoin => "Adaptive Join",
            PlanOperatorKind.Apply => "Nested Loops (Apply)",
            PlanOperatorKind.Concatenation => "Concatenation",
            PlanOperatorKind.MergeInterval => "Merge Interval",

            PlanOperatorKind.StreamAggregate => "Stream Aggregate",
            PlanOperatorKind.HashMatchAggregate => "Hash Match (Aggregate)",
            PlanOperatorKind.Sort => "Sort",
            PlanOperatorKind.TopSort => "Top N Sort",
            PlanOperatorKind.Top => "Top",
            PlanOperatorKind.Filter => "Filter",
            PlanOperatorKind.ComputeScalar => "Compute Scalar",
            PlanOperatorKind.Segment => "Segment",
            PlanOperatorKind.SequenceProject => "Sequence Project",
            PlanOperatorKind.WindowAggregate => "Window Aggregate",
            PlanOperatorKind.Split => "Split",
            PlanOperatorKind.Collapse => "Collapse",
            PlanOperatorKind.Switch => "Switch",
            PlanOperatorKind.Sequence => "Sequence",
            PlanOperatorKind.Assert => "Assert",
            PlanOperatorKind.Bitmap => physicalOp ?? "Bitmap",

            PlanOperatorKind.TableSpool => "Table Spool",
            PlanOperatorKind.IndexSpool => "Index Spool",
            PlanOperatorKind.RowCountSpool => "Row Count Spool",
            PlanOperatorKind.WindowSpool => "Window Spool",

            PlanOperatorKind.GatherStreams => "Gather Streams",
            PlanOperatorKind.RepartitionStreams => "Repartition Streams",
            PlanOperatorKind.DistributeStreams => "Distribute Streams",

            PlanOperatorKind.Insert => physicalOp ?? "Insert",
            PlanOperatorKind.Update => physicalOp ?? "Update",
            PlanOperatorKind.Delete => physicalOp ?? "Delete",
            PlanOperatorKind.Merge => physicalOp ?? "Merge",
            PlanOperatorKind.ClusteredUpdate => "Clustered Update",
            PlanOperatorKind.ConstraintCheck => physicalOp ?? "Constraint Check",

            PlanOperatorKind.Cursor => physicalOp ?? "Cursor",
            PlanOperatorKind.LanguageConstruct => physicalOp ?? "Language Construct",
            PlanOperatorKind.UdxOperator => "UDX",

            // An operator we have no shape for still has a name, and showing it is better than
            // showing "Unknown" over a node the reader can plainly see is doing something.
            _ => physicalOp ?? "Unknown"
        };
    }
}
