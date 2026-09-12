/*
    The plans behind the query stats: one per statement and plan shape, which is the grain of dbo.QueryStats and
    dbo.QueryStats_60MIN, so a row of either - or of a report over them - finds its plan by its own key.

    A plan here is the cached plan of one plan cache entry: the one that did the most work under the plan shape in
    the interval it was fetched in.  Every entry under one query_plan_hash runs the same operators, so it stands for
    all of them, but its estimates and compiled parameter values are its own - and for an ad hoc shape so are its
    statement text and the literal values in it, which are one variant's among however many the shape ran with.
    It is the plan the optimizer compiled, without the figures of any execution.

    The collector fetches them after the statistics, capped per collection and heaviest first, and sends each plan
    shape's once a day rather than once per interval - see dbo.QueryStats_Upd.  The first to arrive is kept: one
    sent again is the same operators, and changes nothing worth a write.  The grid can also fetch a missing one from
    the plan cache on demand, while the plan is still there - see dbo.QueryStatsPlan_Add.

    No foreign key to dbo.QueryStatements, as the fact tables have none: the purge removes a statement's plans along
    with it - see dbo.PurgeQueryStatements.
*/
CREATE TABLE dbo.QueryStatsPlans (
    StatementID BIGINT NOT NULL,
    query_plan_hash BINARY(8) NOT NULL,
    /* The plan's XML as UTF-16, GZip compressed, as dbo.QueryPlans keeps it: CAST(DECOMPRESS(...) AS NVARCHAR(MAX)) reads it */
    query_plan_compressed VARBINARY(MAX) NOT NULL,
    /* The snapshot of the collection that sent it, or when it was fetched on demand */
    CaptureDate DATETIME2(3) NOT NULL,
    CONSTRAINT PK_QueryStatsPlans PRIMARY KEY CLUSTERED (StatementID ASC, query_plan_hash ASC)
);
