/*
    The plan of one statement's plan shape, for the query stats grid, with what the grid needs to fetch the plan from
    the plan cache where none was captured: the statement's handle and offsets, its query hash and its database.

    One row for a statement that exists, its plan NULL where none has been captured - the collection caps the plans
    it fetches, and can have plan capture switched off.  No rows means the statement has been purged.

    For an ad hoc shape the plan is an example.  Its operators are those of every variant that ran under the plan
    shape, but its statement text and the literal values in it are one variant's, which IsExample is there to say.
*/
CREATE PROC dbo.QueryStatsPlan_Get (
    @StatementID BIGINT,
    @query_plan_hash BINARY(8)
)
AS
SET NOCOUNT ON

SELECT S.InstanceID,
       S.StatementType,
       IsExample = CONVERT(BIT, CASE WHEN S.StatementType = 5 THEN 1 ELSE 0 END),
       DatabaseName = D.name,
       S.sql_handle,
       S.statement_start_offset,
       S.statement_end_offset,
       S.query_hash,
       P.query_plan_compressed,
       P.CaptureDate
FROM dbo.QueryStatements S
LEFT JOIN dbo.Databases D ON D.DatabaseID = S.DatabaseID
LEFT JOIN dbo.QueryStatsPlans P ON P.StatementID = S.StatementID
                               AND P.query_plan_hash = @query_plan_hash
WHERE S.StatementID = @StatementID;
