CREATE PROC dbo.BlockingSnapshotSummary_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT = 500000
)
AS
/* 
	Partitioning switching is used to efficiently remove data from this table for all instances.  
	This proc provides an alternative way to remove data associated with a specific instance.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2
SELECT @MaxDate = CASE WHEN @DaysToKeep  = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

SELECT @MaxDate = DATEADD(d,-RetentionDays,GETUTCDATE())
FROM dbo.DataRetention
WHERE TableName = 'RunningQueries'

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize) 
	FROM dbo.BlockingSnapshotSummary
	WHERE SnapshotDateUTC < @MaxDate
	AND InstanceID  = @InstanceID

	IF @@ROWCOUNT<@BatchSize
		BREAK
END