CREATE PROC dbo.PurgeData
AS
DECLARE @TableName SYSNAME,@RetentionDays INT
DECLARE cTables CURSOR FAST_FORWARD LOCAL FOR
	SELECT DR.TableName,DR.RetentionDays
	FROM dbo.DataRetention DR
	CROSS APPLY dbo.PartitionFunctionName(DR.TableName) PF
	WHERE DR.RetentionDays > 0
	

OPEN cTables
WHILE 1=1
BEGIN
	FETCH NEXT FROM cTables INTO @TableName,@RetentionDays
	IF @@FETCH_STATUS<>0
		BREAK
	EXEC dbo.PartitionTable_Cleanup @TableName=@TableName,@DaysToKeep=@RetentionDays
END

CLOSE cTables
DEALLOCATE cTables

/* Remove old data from CollectionErrorLog table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeCollectionErrorLog_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1
BEGIN
	EXEC dbo.PurgeCollectionErrorLog
	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeCollectionErrorLog_CompletedDate'
END

/* Remove old data from QueryPlans table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeQueryPlans_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1
BEGIN
	EXEC dbo.PurgeQueryPlans
	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeQueryPlans_CompletedDate'
END

/* Remove old data from QueryText table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeQueryText_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1
BEGIN
	EXEC dbo.PurgeQueryText
	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeQueryText_CompletedDate'
END