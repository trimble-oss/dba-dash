CREATE PROC dbo.DBIOStats_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT=500000
)
AS
/* 
	Partitioning switching is used to efficiently remove data from this table for all instances.  
	This proc provides an alternative way to remove data associated with a specific instance.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(2)
SELECT @MaxDate = CASE WHEN @DaysToKeep  = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize)  
	FROM dbo.DBIOStats
	WHERE InstanceID = @InstanceID
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END
