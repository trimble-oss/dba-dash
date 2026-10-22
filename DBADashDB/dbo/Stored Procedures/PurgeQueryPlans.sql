CREATE PROC dbo.PurgeQueryPlans(
	@BatchSize INT=1000
)
AS
CREATE TABLE #oldPlans(
	ID INT IDENTITY(1,1) PRIMARY KEY,
	[plan_handle] [varbinary](64) NOT NULL,
	[statement_start_offset] [int] NOT NULL,
	[statement_end_offset] [int] NOT NULL,
	[query_plan_hash] [binary](8) NOT NULL
)
DECLARE @RetentionDays INT
SELECT @RetentionDays = RetentionDays 
FROM dbo.DataRetention
WHERE TableName = 'RunningQueries'

/* Live profiles are kept as long as the running queries they were captured from.  Their plans need no protecting here: each
   capture was taken from a running query in the same collection, whose row references the plan and is kept at least as long. */
/* A retention of 0 or less keeps running queries indefinitely (PurgeData skips them), so the captures are kept too */
IF @RetentionDays > 0
BEGIN
	DECLARE @LiveProfilesCutoff DATETIME2(3) = DATEADD(d,-@RetentionDays,GETUTCDATE())
	WHILE 1=1
	BEGIN
		/* In batches: each row carries a few KB of counters, and one large delete could escalate to a table lock that blocks imports */
		DELETE TOP(@BatchSize) dbo.RunningQueryLiveProfiles
		WHERE LastCapturedUTC < @LiveProfilesCutoff

		IF @@ROWCOUNT < @BatchSize
			BREAK
	END
END

INSERT INTO #oldPlans
SELECT plan_handle,statement_start_offset,statement_end_offset,query_plan_hash 
FROM dbo.QueryPlans QP
WHERE SnapshotDate< DATEADD(d,-@RetentionDays,GETUTCDATE()) 
AND NOT EXISTS(SELECT 1 
				FROM dbo.RunningQueries Q 
				WHERE Q.plan_handle = QP.plan_handle
				AND Q.statement_start_offset = QP.statement_start_offset
				AND Q.statement_end_offset = QP.statement_end_offset
				AND Q.query_plan_hash = QP.query_plan_hash
				)

DECLARE @From INT =1
DECLARE @To INT

WHILE 1=1
BEGIN
	SET @To = @From+ @BatchSize
	DELETE QP 
	FROM dbo.QueryPlans QP
	WHERE  EXISTS(SELECT 1 
				FROM #oldPlans O
				WHERE O.plan_handle = QP.plan_handle
				AND O.statement_start_offset = QP.statement_start_offset
				AND O.statement_end_offset = QP.statement_end_offset
				AND O.query_plan_hash = QP.query_plan_hash
				AND ID >= @From
				AND ID < @To
				)
	IF @@ROWCOUNT=0
		BREAK
	SET @From+=@BatchSize
END
