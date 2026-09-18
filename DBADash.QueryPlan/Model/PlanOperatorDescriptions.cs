using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// What each operator does, in a sentence or three, for readers who don't know every operator by
    /// heart.  Shown on the operator's tooltip and in the properties panel, and on the operator
    /// reference page built alongside the viewer.
    ///
    /// Written to be accurate first and short second: someone learning plans from these will take
    /// them at their word.  Where a kind covers several physical operators that do different things -
    /// the inserted and deleted scans in a trigger, the cursor types, the insert into a heap and into
    /// a clustered index - each physical operator has its own description, and the kind's is the
    /// fallback for the rest.
    /// </summary>
    public static class PlanOperatorDescriptions
    {
        /// <summary>
        /// Physical operators with a description of their own, by the name showplan gives them, with
        /// the kind they belong to so a name the classifier maps elsewhere is never described as this.
        /// </summary>
        private static readonly Dictionary<string, (PlanOperatorKind Kind, string Text)> Variants =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["JSON Index Seek"] = (PlanOperatorKind.NonClusteredIndexSeek,
                    "Uses a JSON index (SQL Server 2025 and later) to find the rows whose JSON documents hold the values or paths being searched for, without reading and parsing every document."),

                ["Deleted Scan"] = (PlanOperatorKind.ConstantScan,
                    "Reads the deleted table inside a trigger: the old versions of the rows affected by the DELETE or UPDATE that fired it."),
                ["Inserted Scan"] = (PlanOperatorKind.ConstantScan,
                    "Reads the inserted table inside a trigger: the new versions of the rows affected by the INSERT or UPDATE that fired it."),
                ["Parameter Table Scan"] = (PlanOperatorKind.ConstantScan,
                    "Reads the rows returned by the stored procedure or dynamic SQL in an INSERT ... EXEC statement, so they can be inserted into the target table."),
                ["Log Row Scan"] = (PlanOperatorKind.ConstantScan,
                    "Reads rows from the transaction log."),

                ["Remote Query"] = (PlanOperatorKind.RemoteQuery,
                    "Sends a query to a linked server or other remote source and returns the rows that come back. How the remote server runs it is not part of this plan."),
                ["Remote Scan"] = (PlanOperatorKind.RemoteQuery,
                    "Reads rows one after another from a remote source, such as a query run on a linked server with OPENQUERY or a file read with OPENROWSET(BULK ...)."),
                ["Remote Index Scan"] = (PlanOperatorKind.RemoteQuery,
                    "Scans an index on a linked server."),
                ["Remote Index Seek"] = (PlanOperatorKind.RemoteQuery,
                    "Seeks an index on a linked server for the rows in its seek range."),
                ["Remote Insert"] = (PlanOperatorKind.RemoteQuery,
                    "Inserts rows into a table on a linked server."),
                ["Remote Update"] = (PlanOperatorKind.RemoteQuery,
                    "Updates rows in a table on a linked server."),
                ["Remote Delete"] = (PlanOperatorKind.RemoteQuery,
                    "Deletes rows from a table on a linked server."),
                ["Put"] = (PlanOperatorKind.RemoteQuery,
                    "Writes rows to an external table through PolyBase: Azure or S3-compatible storage from SQL Server 2022, Hadoop before that. It returns no rows to the operator above it."),

                ["Batch Hash Table Build"] = (PlanOperatorKind.Bitmap,
                    "Builds a batch mode bitmap filter from a hash join's build input, so rows that cannot match can be discarded early. Seen in SQL Server 2012 only: later versions build the bitmap inside the join itself."),

                ["Table Insert"] = (PlanOperatorKind.Insert,
                    "Inserts rows into a heap (a table with no clustered index) or a memory-optimized table, and can maintain the table's nonclustered indexes in the same step."),
                ["Clustered Index Insert"] = (PlanOperatorKind.Insert,
                    "Inserts rows into a clustered index, which holds the table's data, and can maintain the table's nonclustered indexes in the same step."),
                ["Index Insert"] = (PlanOperatorKind.Insert,
                    "Inserts rows into one nonclustered index. Seen in wide plans, which maintain each index with an operator of its own rather than all at once."),
                ["Columnstore Index Insert"] = (PlanOperatorKind.Insert,
                    "Inserts rows into a columnstore index."),
                ["JSON Index Insert"] = (PlanOperatorKind.Insert,
                    "Adds entries to a JSON index for new rows (SQL Server 2025 and later)."),
                ["Online Index Insert"] = (PlanOperatorKind.Insert,
                    "Writes rows into the new copy of an index while it is being built or rebuilt online. It returns no rows to the operator above it."),

                ["Table Update"] = (PlanOperatorKind.Update,
                    "Updates rows in a heap (a table with no clustered index) or a memory-optimized table, and can maintain the table's nonclustered indexes in the same step."),
                ["Clustered Index Update"] = (PlanOperatorKind.Update,
                    "Updates rows in a clustered index, which holds the table's data, and can maintain the table's nonclustered indexes in the same step."),
                ["Index Update"] = (PlanOperatorKind.Update,
                    "Updates rows in one nonclustered index. Seen in wide plans, which maintain each index with an operator of its own rather than all at once."),
                ["Columnstore Index Update"] = (PlanOperatorKind.Update,
                    "Updates rows in a columnstore index."),
                ["JSON Index Update"] = (PlanOperatorKind.Update,
                    "Updates the entries in a JSON index for changed rows (SQL Server 2025 and later)."),

                ["Table Delete"] = (PlanOperatorKind.Delete,
                    "Deletes rows from a heap (a table with no clustered index) or a memory-optimized table, and can maintain the table's nonclustered indexes in the same step."),
                ["Clustered Index Delete"] = (PlanOperatorKind.Delete,
                    "Deletes rows from a clustered index, which holds the table's data, and can maintain the table's nonclustered indexes in the same step."),
                ["Index Delete"] = (PlanOperatorKind.Delete,
                    "Deletes rows from one nonclustered index. Seen in wide plans, which maintain each index with an operator of its own rather than all at once."),
                ["Columnstore Index Delete"] = (PlanOperatorKind.Delete,
                    "Deletes rows from a columnstore index."),
                ["JSON Index Delete"] = (PlanOperatorKind.Delete,
                    "Removes the entries in a JSON index for deleted rows (SQL Server 2025 and later)."),

                ["Table Merge"] = (PlanOperatorKind.Merge,
                    "Applies a mix of inserts, updates and deletes to a heap in one operator, each input row carrying which of the three to do."),
                ["Clustered Index Merge"] = (PlanOperatorKind.Merge,
                    "Applies a mix of inserts, updates and deletes to a clustered index in one operator, each input row carrying which of the three to do. Unlike the separate insert, update and delete operators, it changes only that one index."),
                ["Index Merge"] = (PlanOperatorKind.Merge,
                    "Applies a mix of inserts, updates and deletes to one nonclustered index, each input row carrying which of the three to do."),
                ["Columnstore Index Merge"] = (PlanOperatorKind.Merge,
                    "Applies a mix of inserts, updates and deletes to a columnstore index, each input row carrying which of the three to do."),

                ["Foreign Key References Check"] = (PlanOperatorKind.ConstraintCheck,
                    "After rows are deleted, or key values updated, in a table that many foreign keys refer to, checks that no row in a referencing table still points at a value that has gone. Used when a table is referenced by more than 253 foreign keys, which SQL Server 2016 and later allow."),

                ["Dynamic"] = (PlanOperatorKind.Cursor,
                    "The plan for a dynamic cursor. Each FETCH runs the query beneath it, so the cursor sees changes made to the data while it is open."),
                ["Keyset"] = (PlanOperatorKind.Cursor,
                    "The plan for a keyset-driven cursor. Opening it saves the keys of the qualifying rows in tempdb; each FETCH then reads the current values of the row with the next key."),
                ["Snapshot"] = (PlanOperatorKind.Cursor,
                    "The plan for a static cursor. Opening it copies the whole result into tempdb, and each FETCH reads from that copy, so changes made after it opened are not seen."),
                ["Fast Forward"] = (PlanOperatorKind.Cursor,
                    "The plan for a fast forward cursor: forward-only and read-only. Depending on the query it works like a dynamic cursor or like a static one."),
                ["Population Query"] = (PlanOperatorKind.Cursor,
                    "The query run when the cursor is opened, which saves the cursor's rows, or their keys, in tempdb for the fetches to read."),
                ["Fetch Query"] = (PlanOperatorKind.Cursor,
                    "The query run for each FETCH, returning the next row of the cursor."),
                ["Refresh Query"] = (PlanOperatorKind.Cursor,
                    "Reads the current values of the rows in the cursor's fetch buffer.")
            };

        /// <summary>What an operator of this kind does.</summary>
        public static string For(PlanOperatorKind kind) => kind switch
        {
            PlanOperatorKind.StatementRoot =>
                "The statement this plan belongs to. It does no work of its own: its properties and costs describe the whole statement, such as its memory grant and degree of parallelism.",

            PlanOperatorKind.TableScan =>
                "Reads every row of a heap - a table with no clustered index - and returns the ones that pass any predicate it was given. The rows come back in no particular order. An operator above it, such as a Top, can stop it early.",
            PlanOperatorKind.ClusteredIndexScan =>
                "Reads the rows of a clustered index, which holds the table's data, and returns the ones that pass any predicate it was given. It reads the whole index unless an operator above it, such as a Top, stops asking for rows. Ordered = True means the plan relies on it returning rows in index key order.",
            PlanOperatorKind.NonClusteredIndexScan =>
                "Reads the rows of a nonclustered index and returns the ones that pass any predicate it was given. The index holds only its key columns, any included columns and a pointer to the table row, so it is usually much smaller than the table. It reads the whole index unless an operator above it stops asking for rows.",
            PlanOperatorKind.ColumnstoreIndexScan =>
                "Reads a columnstore index, which stores each column separately in compressed segments, so only the columns the query uses are read. Rowgroups that cannot hold matching rows can be skipped, and some filters and aggregates are applied inside the scan. In the plan XML it is an Index Scan or Clustered Index Scan on columnstore storage.",
            PlanOperatorKind.ClusteredIndexSeek =>
                "Uses the clustered index's key order to go straight to the rows it needs - one key value, or one or more ranges, given by its seek predicate - instead of reading the whole table. Any other predicate is then tested against the rows it finds.",
            PlanOperatorKind.NonClusteredIndexSeek =>
                "Uses a nonclustered index's key order to go straight to the rows it needs - one key value, or one or more ranges, given by its seek predicate. Any other predicate is then tested against the rows it finds. Columns the index does not hold have to be fetched separately, by a Key Lookup or RID Lookup.",
            PlanOperatorKind.KeyLookup =>
                "Fetches columns the query needs that the index it used does not hold, by looking each row up in the clustered index by its clustering key. It runs once per row on the inner side of a Nested Loops join, so it gets expensive when many rows need it. An index that includes those columns avoids it.",
            PlanOperatorKind.RidLookup =>
                "The heap version of a Key Lookup. Fetches columns the query needs that the index it used does not hold, by going to the row in the heap through its row identifier (RID): the file, page and slot the row is stored in. It runs once per row on the inner side of a Nested Loops join.",
            PlanOperatorKind.ConstantScan =>
                "Produces rows from values held in the plan itself rather than read from a table: often a single empty row that a Compute Scalar then adds values to, or the rows of a VALUES list. It can also produce no rows at all, where the optimizer has worked out that the query cannot return any.",
            PlanOperatorKind.RemoteQuery =>
                "Work done on another server - a linked server or external data source. The remote side's own plan is not part of this one.",
            PlanOperatorKind.TableValuedFunction =>
                "Runs a table-valued function that is not expanded into the plan - a multi-statement function or a built-in one - and stores the rows it returns in an internal table that another operator reads. The function's own statements are not shown in this plan, and the estimate of how many rows it returns is often a fixed guess.",

            PlanOperatorKind.NestedLoops =>
                "Joins two inputs by reading the top (outer) input a row at a time and, for each row, running the bottom (inner) input to find its matches. Efficient when the outer input is small and the inner side can seek; slow when the inner side runs many times over many rows.",
            PlanOperatorKind.Apply =>
                "A Nested Loops join run as an apply: for each row of the top (outer) input, the bottom (inner) input runs again with values from that row passed into it. CROSS APPLY, OUTER APPLY and many correlated subqueries run this way.",
            PlanOperatorKind.HashMatchJoin =>
                "Joins two inputs with a hash table. It reads all of the top (build) input into a hash table in memory, then reads the bottom (probe) input and looks each row up in it to find its matches. The hash table needs a memory grant, and spills to tempdb if the build input is bigger than expected.",
            PlanOperatorKind.MergeJoin =>
                "Joins two inputs that are both sorted on the join columns by reading them side by side, in step, and matching rows as it goes. Fast and light on memory when the inputs are already in order; Sort operators may be added below it to put them in order. A many-to-many merge join also uses a worktable in tempdb.",
            PlanOperatorKind.AdaptiveJoin =>
                "Chooses at run time between a hash join and a nested loops join. It reads the top (build) input first; if the number of rows reaches its adaptive threshold it carries on as a hash join using the middle input, and otherwise runs the bottom input as the inner side of nested loops. Only one of the two runs. Batch mode only, from SQL Server 2017.",
            PlanOperatorKind.Concatenation =>
                "Returns all the rows of its first input, then all the rows of the next, and so on, unchanged. This is UNION ALL; for UNION, other operators remove the duplicates.",
            PlanOperatorKind.MergeInterval =>
                "Takes seek ranges that are only known when the query runs - built from variables or parameters, say - and combines any that overlap into one, so an index seek does not read the same rows twice. Its input has to be sorted.",

            PlanOperatorKind.StreamAggregate =>
                "Computes aggregates over input that arrives sorted or grouped by the grouping columns, returning each group's result as soon as the next group starts, so it needs very little memory. With no GROUP BY it returns a single row for all of its input.",
            PlanOperatorKind.HashMatchAggregate =>
                "Groups rows with a hash table in memory: each row's grouping values are hashed to find its group, and the group's aggregates are updated as rows arrive. Unlike a Stream Aggregate it needs no sorted input, but it needs a memory grant and spills to tempdb if there are more groups than expected. Also used to remove duplicates.",
            PlanOperatorKind.WindowAggregate =>
                "Computes window functions, such as SUM(...) OVER (...), in batch mode, in a single pass over sorted input - the work that takes a Segment, a Window Spool and a Stream Aggregate in row mode. SQL Server 2016 and later.",
            PlanOperatorKind.Sort =>
                "Puts its input in the order given by its Order By property. It has to read all of its input before it can return the first row, and needs a memory grant to do it in; if the grant is too small it spills to tempdb.",
            PlanOperatorKind.TopSort =>
                "Returns only the first N rows in a given order, as for TOP (N) ... ORDER BY. For a small N it keeps just the best N rows seen so far rather than sorting everything, but it still reads all of its input before returning a row.",
            PlanOperatorKind.Top =>
                "Returns the first rows of its input - a number of them, or a percentage - and then stops asking for more, so the operators below it may never read all of theirs. Also used for OFFSET ... FETCH, SET ROWCOUNT, and UPDATE or DELETE with TOP.",
            PlanOperatorKind.Filter =>
                "Tests each row against a condition and passes on only the rows that meet it. Most conditions are tested inside a scan or seek; a separate Filter is used where that is not possible, such as a condition on an aggregate. A startup expression predicate is tested once, to decide whether to run its input at all.",
            PlanOperatorKind.ComputeScalar =>
                "Works out new values from each row's columns and constants - expressions, conversions, function calls - and adds them as columns. The work is often put off until an operator above needs the value, which is why a Compute Scalar can show little cost or time of its own.",
            PlanOperatorKind.Segment =>
                "Adds a column marking where each new group starts in input sorted by the grouping columns; the rows themselves pass through unchanged. Operators above it, such as Sequence Project, use the mark to know when a group begins.",
            PlanOperatorKind.SequenceProject =>
                "Computes the ranking functions - ROW_NUMBER, RANK, DENSE_RANK and NTILE - for each row, working through input sorted and segmented by the PARTITION BY and ORDER BY columns.",
            PlanOperatorKind.Split =>
                "Turns each update into two rows, a delete of the old values and an insert of the new, so updates that change key values can be sorted and applied without breaking a unique constraint part way through. Works with Sort and Collapse.",
            PlanOperatorKind.Collapse =>
                "Recombines a delete and an insert of the same key, made by a Split and sorted together, into a single update, so a statement that changes unique key values does not fail on a duplicate that exists only part way through.",
            PlanOperatorKind.Switch =>
                "Runs exactly one of its inputs, chosen when the query runs by an expression, and returns that input's rows. Rare: seen in some inserts into partitioned views and some XQuery plans.",
            PlanOperatorKind.Sequence =>
                "Runs each of its inputs in turn, top to bottom, and returns only the rows from the last one. The earlier inputs do the preparation, such as maintaining each index in a wide update plan or filling the table for a table-valued function.",
            PlanOperatorKind.Assert =>
                "Checks a condition on each row and stops the statement with an error if it fails. Used to enforce CHECK and FOREIGN KEY constraints when data changes, and to make sure a subquery used as a single value returns no more than one row.",
            PlanOperatorKind.Bitmap =>
                "Builds a bitmap filter from the values in a hash join's build input, for the probe side to discard rows that cannot match - often in the scan, before they reach the join. A bitmap can let through a row that does not match, but never throws away one that does. Seen in parallel plans.",

            PlanOperatorKind.TableSpool =>
                "Saves the rows from its input in a worktable in tempdb so they can be returned again without running the operators below it again - each time the inner side of a Nested Loops runs, say, or in another branch of the plan. An eager spool reads all of its input at once, which also keeps an update's reads apart from its writes; a lazy spool saves rows as they are asked for.",
            PlanOperatorKind.IndexSpool =>
                "Saves the rows from its input in a worktable in tempdb and builds an index on it, so later executions - usually of the inner side of a Nested Loops - can seek just the rows they need. Often a sign that a permanent index on the underlying table would help.",
            PlanOperatorKind.RowCountSpool =>
                "Counts the rows from its input rather than saving them, and can then return that many empty rows again without running its input again. Used where only whether there are rows, or how many, matters, as in some NOT EXISTS checks.",
            PlanOperatorKind.WindowSpool =>
                "Keeps the rows of each window frame so window aggregates with a ROWS or RANGE frame can be computed in row mode. A ROWS frame can often use a worktable in memory; RANGE, the default when ORDER BY is given with no frame, uses one in tempdb, which is slower.",

            PlanOperatorKind.GatherStreams =>
                "Brings the rows from all the parallel threads below it together into one stream, for the serial part of the plan above. If its input is sorted it can keep that order by merging the threads' rows.",
            PlanOperatorKind.RepartitionStreams =>
                "Moves rows between parallel threads, usually by hashing the columns of a join or grouping above it, so that rows that belong together end up on the same thread.",
            PlanOperatorKind.DistributeStreams =>
                "Takes rows from a single thread and shares them out across several parallel threads.",

            PlanOperatorKind.Insert => "Adds rows to a table or index.",
            PlanOperatorKind.Update => "Changes rows in a table or index.",
            PlanOperatorKind.Delete => "Removes rows from a table or index.",
            PlanOperatorKind.Merge =>
                "Applies a mix of inserts, updates and deletes to a table or index in one operator, each input row carrying which of the three to do.",
            PlanOperatorKind.ClusteredUpdate => "Changes rows in a clustered index.",
            PlanOperatorKind.ConstraintCheck =>
                "Checks that a change to the data has not broken a constraint.",

            PlanOperatorKind.Cursor =>
                "Part of a cursor's plan, shown in an estimated plan: a container for the queries the cursor runs, rather than an operator that processes rows.",
            PlanOperatorKind.LanguageConstruct =>
                "A T-SQL statement that does not read or change rows, such as DECLARE, SET or PRINT, shown in an estimated plan as a single node.",
            PlanOperatorKind.UdxOperator =>
                "Runs one of SQL Server's built-in extended operators, which carry out XQuery and XPath on the xml data type and some JSON functions. Depending on the function it combines many rows into one, adds columns to each row, or returns several rows for each.",

            _ =>
                "An operator this viewer does not recognise. The name is the one SQL Server gave it, and the properties panel shows everything the plan recorded about it."
        };

        /// <summary>
        /// What one operator in a plan does: its physical operator's own description where it has
        /// one, otherwise its kind's, with a word on the variant where the logical operator changes
        /// the behaviour - an eager or a lazy spool, a sort that also removes duplicates.
        /// </summary>
        public static string For(PlanOperatorKind kind, string? physicalOp, string? logicalOp)
        {
            if (physicalOp is not null &&
                Variants.TryGetValue(physicalOp, out var variant) &&
                variant.Kind == kind)
            {
                return variant.Text;
            }

            var text = For(kind);

            if (kind is PlanOperatorKind.TableSpool or PlanOperatorKind.IndexSpool or PlanOperatorKind.RowCountSpool)
            {
                if (IsLogical(logicalOp, "Eager Spool")) return text + " This one is eager: it reads all of its input before returning its first row.";
                if (IsLogical(logicalOp, "Lazy Spool")) return text + " This one is lazy: it reads its input only as rows are asked for.";
            }

            if (kind == PlanOperatorKind.Sort && IsLogical(logicalOp, "Distinct Sort"))
            {
                return text + " This one is a Distinct Sort, so it also removes duplicate rows.";
            }

            return text;
        }

        /// <summary>What <paramref name="op"/> does.</summary>
        public static string For(PlanOperator op) => For(op.Kind, op.PhysicalOp, op.LogicalOp);

        /// <summary>
        /// The physical operators of a kind that have a description of their own, for the operator
        /// reference, in name order.
        /// </summary>
        public static IReadOnlyList<(string PhysicalOp, string Description)> VariantsOf(PlanOperatorKind kind) =>
            Variants
                .Where(v => v.Value.Kind == kind)
                .OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase)
                .Select(v => (v.Key, v.Value.Text))
                .ToList();

        private static bool IsLogical(string? logicalOp, string name) =>
            string.Equals(logicalOp, name, StringComparison.OrdinalIgnoreCase);
    }
}
