CREATE PROC dbo.LogShippingSummary_Get(
	@InstanceIDs IDs READONLY
)
AS
DECLARE @SQL NVARCHAR(MAX)
SET @SQL =N'
SELECT InstanceID,
       Instance,
       Status,
       StatusDescription,
       LogShippedDBCount,
       WarningCount,
       CriticalCount,
       MaxTotalTimeBehind,
       MaxLatencyOfLast,
       TimeSinceLast,
       SnapshotAge,
       SnapshotAgeStatus,
       MinDateOfLastBackupRestored,
       MinLastRestoreCompleted,
       InstanceLevelThreshold,
       DatabaseLevelThresholds
FROM dbo.LogShippingStatusSummary LS
'+ CASE WHEN EXISTS(SELECT 1 FROM @InstanceIDs) THEN 'WHERE EXISTS
(
    SELECT 1 FROM @InstanceIDs t WHERE t.ID = LS.InstanceID
)' ELSE '' END 

EXEC sp_executesql @SQL,N'@InstanceIDs IDs READONLY',@InstanceIDs
GO