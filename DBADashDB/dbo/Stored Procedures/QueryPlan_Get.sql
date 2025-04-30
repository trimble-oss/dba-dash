CREATE PROCEDURE dbo.QueryPlan_Get (
	@plan_handle varbinary(64),
	@query_plan_hash binary(8),
	@statement_start_offset int,
	@statement_end_offset int
)
AS
BEGIN;
	SELECT QP.query_plan_compresed
	FROM dbo.QueryPlans QP
	WHERE QP.plan_handle = @plan_handle
		AND QP.query_plan_hash = @query_plan_hash
		AND QP.statement_start_offset = @statement_start_offset
		AND QP.statement_end_offset = @statement_end_offset
END;