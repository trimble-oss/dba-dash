CREATE PROC dbo.DBFileSnapshot_Del(
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

CREATE TABLE #Files(
	FileID INT PRIMARY KEY
)

INSERT INTO #Files
(
    FileID
)
SELECT F.FileID
FROM dbo.Databases D 
JOIN dbo.DBFiles F ON F.DatabaseID = D.DatabaseID
WHERE D.InstanceID = @InstanceID 

WHILE 1=1
BEGIN
	DELETE TOP(@BatchSize) FSS
	FROM dbo.DBFileSnapshot FSS
	WHERE EXISTS(SELECT 1 
				FROM #Files F 
				WHERE F.FileID = FSS.FileID
				)
	AND SnapshotDate < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END