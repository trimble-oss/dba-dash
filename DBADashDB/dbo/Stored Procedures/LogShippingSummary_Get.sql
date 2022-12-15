CREATE PROC dbo.LogShippingSummary_Get(
	@InstanceIDs IDs READONLY,
    @ShowHidden BIT=1
)
AS
DECLARE @SQL NVARCHAR(MAX)
SET @SQL =N'
SELECT InstanceID,
       InstanceDisplayName,
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
WHERE 1=1
'+ CASE WHEN EXISTS(SELECT 1 FROM @InstanceIDs) THEN 'AND EXISTS
(
    SELECT 1 FROM @InstanceIDs t WHERE t.ID = LS.InstanceID
)' ELSE '' END + '
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND LS.ShowInSummary=1' END

EXEC sp_executesql @SQL,N'@InstanceIDs IDs READONLY',@InstanceIDs
GO