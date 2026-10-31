/*
	The most recently captured plan with a given query_plan_hash, for MCP clients.  Looks in the running queries
	plan cache (dbo.QueryPlans, found through dbo.RunningQueries for its instance) and in the query stats plans
	(dbo.QueryStatsPlans), and returns the newest of either.  @QueryPlanHash is the "0x..." form that
	AI.RunningQueries_Get returns.  @InstanceID narrows it to one instance; a plan hash is a hash of the plan's
	shape, so the same one can turn up on several.
*/
CREATE PROC AI.QueryPlan_Get(
	@QueryPlanHash VARCHAR(18),
	@InstanceID INT = NULL
)
AS
SET NOCOUNT ON
DECLARE @Hash BINARY(8) = CONVERT(BINARY(8), @QueryPlanHash, 1)

SELECT TOP(1)
	P.InstanceID,
	I.InstanceDisplayName,
	P.Source,
	P.CaptureDate,
	CAST(DECOMPRESS(P.PlanCompressed) AS NVARCHAR(MAX)) AS QueryPlan
FROM (
	SELECT	RQ.InstanceID,
			'Running queries' AS Source,
			CAST(QP.SnapshotDate AS DATETIME2(3)) AS CaptureDate,
			QP.query_plan_compresed AS PlanCompressed
	FROM dbo.QueryPlans QP
	CROSS APPLY(
		SELECT TOP(1) RQ.InstanceID
		FROM dbo.RunningQueries RQ
		WHERE RQ.query_plan_hash = QP.query_plan_hash
		AND RQ.plan_handle = QP.plan_handle
		AND RQ.statement_start_offset = QP.statement_start_offset
		AND RQ.statement_end_offset = QP.statement_end_offset
		AND (@InstanceID IS NULL OR RQ.InstanceID = @InstanceID)
	) RQ
	WHERE QP.query_plan_hash = @Hash
	AND QP.query_plan_compresed IS NOT NULL
	UNION ALL
	SELECT	S.InstanceID,
			'Query stats',
			QSP.CaptureDate,
			QSP.query_plan_compressed
	FROM dbo.QueryStatsPlans QSP
	JOIN dbo.QueryStatements S ON S.StatementID = QSP.StatementID
	WHERE QSP.query_plan_hash = @Hash
	AND (@InstanceID IS NULL OR S.InstanceID = @InstanceID)
) P
JOIN dbo.Instances I ON I.InstanceID = P.InstanceID
ORDER BY P.CaptureDate DESC
