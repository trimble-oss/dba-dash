namespace DBADash.QueryPlan.Live
{
    /// <summary>
    /// One operator on one thread of a query that is still running, as sys.dm_exec_query_profiles reports it.
    ///
    /// The counters are cumulative from the start of the request, so the latest set says everything the earlier ones
    /// did.  Under lightweight profiling - the default from SQL 2019 - the times, reads and timestamps are not collected
    /// and come back as zero or null, which is why everything past the row and execution counts is optional.
    /// </summary>
    public sealed record PlanLiveCounters
    {
        public int NodeId { get; init; }

        public int Thread { get; init; }

        public long Rows { get; init; }

        public long Rebinds { get; init; }

        public long Rewinds { get; init; }

        public long EndOfScans { get; init; }

        /// <summary>
        /// How many times the operator has run: rebinds plus rewinds, which is how showplan's ActualExecutions is made
        /// up - the inner side of a nested loop runs once per outer row, and the other operators once.
        /// </summary>
        public long Executions => Rebinds + Rewinds;

        public long? ElapsedMs { get; init; }

        public long? CpuMs { get; init; }

        public long? Scans { get; init; }

        public long? LogicalReads { get; init; }

        public long? PhysicalReads { get; init; }

        public long? ReadAheads { get; init; }

        public long? LobLogicalReads { get; init; }

        public long? LobPhysicalReads { get; init; }

        /// <summary>Rows read before the residual predicate, where the instance reports it.</summary>
        public long? RowsRead { get; init; }

        /// <summary>When the operator was opened, in ms.  Zero or null where it hasn't been, or under lightweight profiling.</summary>
        public long? OpenTimeMs { get; init; }

        /// <summary>When the operator was closed, in ms.  Zero or null while it is open, or under lightweight profiling.</summary>
        public long? CloseTimeMs { get; init; }
    }
}
