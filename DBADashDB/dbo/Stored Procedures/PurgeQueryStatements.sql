CREATE PROC dbo.PurgeQueryStatements (
    @BatchSize INT = 10000
)
AS
/*
    Remove statements that no longer have any statistics behind them.

    The fact tables are partitioned and purged by date, but the dimension is not: a statement keeps its row
    when it stops running, which is what lets it keep its identity and its history if it runs again.  Without
    this proc that is a one way street - every statement ever stored would keep its row, its row in
    dbo.QueryText and its plans, for good.  The plans go with the statement, here.

    A row is removed only once both conditions hold: nothing in the hourly rollup refers to it, and it has not
    been seen for the longer of the raw and the hourly retention periods.  The rollup answers for dbo.QueryStats
    too, since every collection's rows are rolled into it as they are stored and it is kept longer.  The second
    test matters because a statement that ran outside what is still kept has no rows anywhere, and deleting it
    would split its history the next time it runs.
*/
SET NOCOUNT ON

DECLARE @RawDays INT
DECLARE @HourlyDays INT

SELECT @RawDays = MAX(CASE WHEN TableName = 'QueryStats' THEN RetentionDays END),
       @HourlyDays = MAX(CASE WHEN TableName = 'QueryStats_60MIN' THEN RetentionDays END)
FROM dbo.DataRetention
WHERE SchemaName = 'dbo'
AND TableName IN ('QueryStats', 'QueryStats_60MIN')

/* Zero or missing retention means keep everything, as it does for the partitioned tables */
IF ISNULL(@RawDays, 0) <= 0 OR ISNULL(@HourlyDays, 0) <= 0
    RETURN

DECLARE @Cutoff DATETIME2(3) = DATEADD(d, -(CASE WHEN @RawDays > @HourlyDays THEN @RawDays ELSE @HourlyDays END), GETUTCDATE())

CREATE TABLE #Deleted (StatementID BIGINT NOT NULL PRIMARY KEY);

WHILE 1 = 1
BEGIN
    DELETE TOP (@BatchSize) S
    OUTPUT deleted.StatementID INTO #Deleted (StatementID)
    FROM dbo.QueryStatements S
    WHERE S.LastSeen < @Cutoff
    AND NOT EXISTS (SELECT 1
                    FROM dbo.QueryStats_60MIN H
                    WHERE H.StatementID = S.StatementID)
    OPTION (RECOMPILE)

    IF @@ROWCOUNT = 0
        BREAK

    DELETE P
    FROM dbo.QueryStatsPlans P
    WHERE P.StatementID IN (SELECT D.StatementID FROM #Deleted D)

    TRUNCATE TABLE #Deleted
END
