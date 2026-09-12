CREATE PROC dbo.QueryStatsCollection_Del (
    @InstanceID INT,
    @DaysToKeep INT,
    @BatchSize INT = 500000
)
AS
/*
    Partition switching removes old data for all instances, by the table's own retention.  This proc is the
    alternative path for removing the rows belonging to one instance, used when an instance is deleted.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(3)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d, -@DaysToKeep, GETUTCDATE()) END

WHILE 1 = 1
BEGIN
    DELETE TOP (@BatchSize)
    FROM dbo.QueryStatsCollection
    WHERE InstanceID = @InstanceID
    AND SnapshotDate < @MaxDate
    OPTION (RECOMPILE)

    IF @@ROWCOUNT < @BatchSize
        BREAK
END
