CREATE PROC dbo.DriveSnapshot_Del(
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
	DELETE TOP(@BatchSize) DSS
	FROM dbo.DriveSnapshot DSS
	WHERE EXISTS(
				SELECT 1 
				FROM dbo.Drives D 
				WHERE D.InstanceID = @InstanceID 
				AND D.DriveID = DSS.DriveID
				)
	AND DSS.SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END