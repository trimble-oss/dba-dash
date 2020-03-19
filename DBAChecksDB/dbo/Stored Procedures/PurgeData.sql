CREATE PROC PurgeData
AS
DECLARE @TableName SYSNAME,@RetentionDays INT
DECLARE cTables CURSOR FAST_FORWARD LOCAL FOR
	SELECT TableName,RetentionDays
	FROM dbo.DataRetention
	WHERE RetentionDays IS NOT NULL
	AND TableName IN('ProcStats','FunctionStats','Waits','IOStats','CPU','BlockingSnapshot')

OPEN cTables
WHILE 1=1
BEGIN
	FETCH NEXT FROM cTables INTO @TableName,@RetentionDays
	IF @@FETCH_STATUS<>0
		BREAK
	EXEC [dbo].[PartitionTable_Cleanup] @TableName=@TableName,@DaysToKeep=@RetentionDays
END

CLOSE cTables
DEALLOCATE cTables