CREATE PROC dbo.QueryPlans_Upd(
	@QueryPlans dbo.QueryPlans READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
DECLARE @Ref VARCHAR(30)='QueryPlans'

INSERT INTO dbo.QueryPlans
(
    plan_handle,
    statement_start_offset,
    statement_end_offset,
    dbid,
    object_id,
    encrypted,
    query_plan_hash,
    query_plan_compresed,
    SnapshotDate
)
SELECT  plan_handle,
    statement_start_offset,
    statement_end_offset,
    dbid,
    object_id,
    encrypted,
    query_plan_hash,
    query_plan_compresed,
	@SnapshotDate
FROM @QueryPlans t 
WHERE NOT EXISTS(SELECT 1 
			FROM dbo.QueryPlans QP 
			WHERE QP.plan_handle =t.plan_handle 
			AND QP.query_plan_hash = t.query_plan_hash 
			AND QP.statement_start_offset= t.statement_start_offset 
			AND QP.statement_end_offset = t.statement_end_offset
			)

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
										@Reference = @Ref,
										@SnapshotDate = @SnapshotDate;