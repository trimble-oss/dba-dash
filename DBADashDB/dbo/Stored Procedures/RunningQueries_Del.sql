CREATE PROC dbo.RunningQueries_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT=100000
)
AS
/* 
	Partitioning switching is used to efficiently remove data from this table for all instances.  
	This proc provides an alternative way to remove data associated with a specific instance.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

DECLARE @deleted TABLE (
	sql_handle VARBINARY(64),
	query_plan_hash BINARY(8), 
	plan_handle VARBINARY(64), 
	statement_start_offset INT, 
	statement_end_offset INT
)

DECLARE @MaxDate DATETIME2(7)
SELECT @MaxDate = CASE WHEN @DaysToKeep  = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	BEGIN TRAN

	DELETE TOP(@BatchSize)  
	FROM dbo.RunningQueries
	OUTPUT DELETED.sql_handle,DELETED.query_plan_hash,DELETED.plan_handle,DELETED.statement_start_offset,DELETED.statement_end_offset INTO @deleted
	WHERE InstanceID = @InstanceID
	AND SnapshotDateUTC < @MaxDate
	OPTION(RECOMPILE)
	
	DELETE QT 
	FROM dbo.QueryText QT
	WHERE EXISTS(SELECT 1 FROM @deleted H WHERE H.sql_handle =QT.sql_handle)
	AND NOT EXISTS(SELECT 1 FROM dbo.RunningQueries RQ WHERE RQ.sql_handle = QT.sql_handle)

	DELETE QP
	FROM dbo.QueryPlans QP
	WHERE EXISTS(SELECT 1 
					FROM @deleted H 
					WHERE H.plan_handle = QP.plan_handle 
					AND H.query_plan_hash = QP.query_plan_hash 
					AND H.statement_start_offset = QP.statement_start_offset 
					AND H.statement_end_offset = QP.statement_end_offset
					)
	AND NOT EXISTS(SELECT 1 
					FROM dbo.RunningQueries RQ 	
					WHERE RQ.plan_handle = QP.plan_handle 
					AND RQ.query_plan_hash = QP.query_plan_hash 
					AND RQ.statement_start_offset = QP.statement_start_offset 
					AND RQ.statement_end_offset = QP.statement_end_offset
					)

	COMMIT

	IF @@ROWCOUNT < @BatchSize
		BREAK

	DELETE @deleted


END