CREATE PROC dbo.QueryStats_60MIN_Del (
    @InstanceID INT,
    @DaysToKeep INT,
    @BatchSize INT = 500000
)
AS
/*
    Partition switching removes old data for all instances, by the table's own retention.  This proc is the
    alternative path for removing the rows belonging to one instance, used when an instance is deleted.  It runs
    before dbo.QueryStats_Del, which removes the statements these rows refer to.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(3)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d, -@DaysToKeep, GETUTCDATE()) END

WHILE 1 = 1
BEGIN
    DELETE TOP (@BatchSize)
    FROM dbo.QueryStats_60MIN
    WHERE InstanceID = @InstanceID
    AND SnapshotDate < @MaxDate
    OPTION (RECOMPILE)

    IF @@ROWCOUNT < @BatchSize
        BREAK
END
