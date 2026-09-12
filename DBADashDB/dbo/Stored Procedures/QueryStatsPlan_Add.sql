/*
    Keep a plan the query stats grid fetched from the plan cache on demand, for a statement and plan shape the
    collection had not captured one for: past its cap, or with plan capture switched off.  Stored as the collection
    stores its own, so it is there the next time it is wanted, whether or not it is still cached - and only where
    there is none yet, and for a statement that still exists.
*/
CREATE PROC dbo.QueryStatsPlan_Add (
    @StatementID BIGINT,
    @query_plan_hash BINARY(8),
    @query_plan_compressed VARBINARY(MAX)
)
AS
SET NOCOUNT ON

/* Locked while checked, so two people fetching the same plan at once store it once rather than one of them failing */
INSERT INTO dbo.QueryStatsPlans (StatementID, query_plan_hash, query_plan_compressed, CaptureDate)
SELECT @StatementID,
       @query_plan_hash,
       @query_plan_compressed,
       SYSUTCDATETIME()
WHERE EXISTS (SELECT 1
              FROM dbo.QueryStatements S
              WHERE S.StatementID = @StatementID)
AND NOT EXISTS (SELECT 1
                FROM dbo.QueryStatsPlans P WITH (UPDLOCK, HOLDLOCK)
                WHERE P.StatementID = @StatementID
                AND P.query_plan_hash = @query_plan_hash);
