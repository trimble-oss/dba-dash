CREATE PROC dbo.QueryStats_Del (
    @InstanceID INT,
    @DaysToKeep INT,
    @BatchSize INT = 500000
)
AS
/*
    Partition switching removes old data for all instances.  This proc is the alternative path for removing
    data belonging to one instance, used when an instance is deleted.

    The statement dimension goes with it when @DaysToKeep is 0, and the statements' plans before it, while there
    are still statements to find them by.  The hourly rollup refers to it too, so dbo.QueryStats_60MIN_Del runs
    first.  At any other retention it is left alone: a statement that stops running keeps its row, which is what
    lets it keep its history and its identity if it runs again.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2(3)
SELECT @MaxDate = CASE WHEN @DaysToKeep = 0 THEN '30000101' ELSE DATEADD(d, -@DaysToKeep, GETUTCDATE()) END

WHILE 1 = 1
BEGIN
    DELETE TOP (@BatchSize)
    FROM dbo.QueryStats
    WHERE InstanceID = @InstanceID
    AND SnapshotDate < @MaxDate
    OPTION (RECOMPILE)

    IF @@ROWCOUNT < @BatchSize
        BREAK
END

IF @DaysToKeep = 0
BEGIN
    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize) P
        FROM dbo.QueryStatsPlans P
        WHERE EXISTS (SELECT 1
                      FROM dbo.QueryStatements S
                      WHERE S.StatementID = P.StatementID
                      AND S.InstanceID = @InstanceID)
        OPTION (RECOMPILE)

        IF @@ROWCOUNT < @BatchSize
            BREAK
    END

    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize)
        FROM dbo.QueryStatements
        WHERE InstanceID = @InstanceID
        OPTION (RECOMPILE)

        IF @@ROWCOUNT < @BatchSize
            BREAK
    END
END
