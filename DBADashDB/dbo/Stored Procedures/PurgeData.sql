﻿CREATE PROC dbo.PurgeData(
	@Force BIT=0 /* Option to force purge of non-partitioned tables which usually run once per day */
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON
DECLARE @SchemaName SYSNAME
DECLARE @TableName SYSNAME
DECLARE @RetentionDays INT
DECLARE @Errors VARCHAR(MAX) = ''
DECLARE @Error VARCHAR(MAX)
/*	Prevent simultaneous execution of partition cleanup.
	After 20min, assume previous execution failed.
*/
UPDATE S
	SET SettingValue = GETUTCDATE()
FROM dbo.Settings S
WHERE S.SettingName = 'PurgePartitions_StartDate'
AND (S.SettingValue < DATEADD(mi,-20,GETUTCDATE())
	OR EXISTS(SELECT 1 
			FROM dbo.Settings S2
			WHERE S2.SettingName = 'PurgePartitions_CompletedDate'
			AND S2.SettingValue > S.SettingValue
			)
	)

IF @@ROWCOUNT= 1
BEGIN
	DECLARE cTables CURSOR FAST_FORWARD LOCAL FOR
		SELECT	DR.SchemaName,
				DR.TableName,
				DR.RetentionDays
		FROM dbo.DataRetention DR
		WHERE DR.RetentionDays > 0
		AND EXISTS(	SELECT 1
					FROM dbo.PartitionHelper PH
					WHERE PH.SchemaName = DR.SchemaName
					AND PH.TableName = DR.TableName
					)
		ORDER BY CASE WHEN DR.SchemaName = 'dbo' THEN 1 ELSE 2 END, DR.TableName
	
	OPEN cTables
	WHILE 1=1
	BEGIN
		FETCH NEXT FROM cTables INTO @SchemaName, @TableName, @RetentionDays
		IF @@FETCH_STATUS<>0
			BREAK
		PRINT 'Cleanup ' + @TableName

		BEGIN TRY
			EXEC dbo.PartitionTable_Cleanup @SchemaName = @SchemaName, @TableName=@TableName, @DaysToKeep=@RetentionDays
		END TRY 
		BEGIN CATCH 
			/* Continue processing if there is an issue with partition switching for a particular table and throw error at end */
			SET @Error = 'Error running cleanup for ' + @TableName + ':' + ERROR_MESSAGE()
			PRINT @Error 
			SET @Errors += @Error + CHAR(13) + CHAR(10)
		END CATCH
	END

	CLOSE cTables
	DEALLOCATE cTables

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgePartitions_CompletedDate'

END
ELSE
BEGIN
	PRINT 'Skipping PartitionTable_Cleanup (Already Running)'
END

/* Remove old data from CollectionErrorLog table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeCollectionErrorLog_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1 OR @Force=1
BEGIN
	PRINT 'Cleanup CollectionErrorLog'
	EXEC dbo.PurgeCollectionErrorLog

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeCollectionErrorLog_CompletedDate'
END
ELSE
BEGIN
	PRINT 'Skipping CollectionErrorLog (Ran withing last 24hrs)'
END

/* Remove old data from QueryPlans table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeQueryPlans_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1 OR @Force=1
BEGIN
	PRINT 'Cleanup QueryPlans'
	EXEC dbo.PurgeQueryPlans

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeQueryPlans_CompletedDate'
END
ELSE
BEGIN
	PRINT 'Skipping QueryPlans (Ran withing last 24hrs)'
END

/* Remove old data from QueryText table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeQueryText_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1 OR @Force=1
BEGIN
	PRINT 'Cleanup QueryText'
	EXEC dbo.PurgeQueryText

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeQueryText_CompletedDate'
END
ELSE
BEGIN
	PRINT 'Skipping QueryText (Ran withing last 24hrs)'
END

/* Remove old data from BlockingSnapshotSummary table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeBlockingSnapshotSummary_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1 OR @Force=1
BEGIN
	PRINT 'Cleanup QueryText'
	EXEC dbo.PurgeBlockingSnapshotSummary

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeBlockingSnapshotSummary_CompletedDate'
END
ELSE
BEGIN
	PRINT 'Skipping BlockingSnapshotSummary (Ran withing last 24hrs)'
END

/* Remove old data from ClosedAlerts table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeClosedAlerts_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1 OR @Force=1
BEGIN
	PRINT 'Cleanup ClosedAlerts'
	DECLARE @KeepNotes BIT = 1
	IF EXISTS(	SELECT 1 
				FROM dbo.Settings 
				WHERE SettingName = 'ExcludeClosedAlertsWithNotesFromPurge' 
				AND SettingValue = CAST(0 AS BIT)
				)
	BEGIN
		PRINT 'ClosedAlerts with notes will be purged'
		SET @KeepNotes=0
	END
	EXEC dbo.PurgeClosedAlerts @KeepNotes=@KeepNotes

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeClosedAlerts_CompletedDate'
END
ELSE
BEGIN
	PRINT 'Skipping ClosedAlerts (Ran withing last 24hrs)'
END

/* Remove old data from OfflineInstances table.  Run once per day */
UPDATE dbo.Settings
	SET SettingValue = GETUTCDATE()
WHERE SettingName = 'PurgeOfflineInstances_StartDate'
AND SettingValue < DATEADD(d,-1,GETUTCDATE())

IF @@ROWCOUNT =1 OR @Force=1
BEGIN
	PRINT 'Cleanup OfflineInstances'

	EXEC dbo.PurgeOfflineInstances

	UPDATE dbo.Settings
		SET SettingValue = GETUTCDATE()
	WHERE SettingName = 'PurgeOfflineInstances_CompletedDate'
END
ELSE
BEGIN
	PRINT 'Skipping OfflineInstances (Ran withing last 24hrs)'
END

/* Service Broker Cleanup */
EXEC Messaging.Cleanup

/* Remove instances that have been soft deleted after threshold days if enaabled */
DECLARE @HardDeleteThresholdDays INT
SELECT @HardDeleteThresholdDays = TRY_CAST(SettingValue AS INT)
FROM dbo.Settings
WHERE SettingName = 'HardDeleteThresholdDays'

IF @HardDeleteThresholdDays >= 1
BEGIN
	PRINT CONCAT('Running Instances_HardDelete with threshold ',@HardDeleteThresholdDays,' days')
	EXEC dbo.Instances_HardDelete @HardDeleteThresholdDays=@HardDeleteThresholdDays
END

IF @Errors <> ''
BEGIN;
	THROW 51000,@Errors,1;
END