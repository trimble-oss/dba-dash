CREATE PROC dbo.CollectionDates_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=1,
	@IncludeOK BIT=1,
	@ShowHidden BIT=1
)
AS
DECLARE @SQL NVARCHAR(MAX)
SET @SQL =N'
WITH Statuses AS(
	SELECT 1 AS Status
	WHERE @IncludeCritical=1
	UNION ALL
	SELECT 2
	WHERE @IncludeWarning=1
	UNION ALL
	SELECT 3
	WHERE @IncludeNA=1
	UNION ALL
	SELECT 4 
	WHERE @IncludeOK=1
)
SELECT CD.InstanceID,
	   I.ConnectionID,
	   I.InstanceDisplayName,
       CD.Reference,
       CD.Status,
       CD.WarningThreshold,
       CD.CriticalThreshold,
       CD.SnapshotAge,
	   CD.HumanSnapshotAge,
       CD.SnapshotDate,
       CD.ConfiguredLevel,
	   ImportAgentID,
	   CollectAgentID,
	   CASE WHEN IA.MessagingEnabled=1 AND CA.MessagingEnabled=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS MessagingEnabled
FROM dbo.CollectionDatesStatus CD
JOIN dbo.Instances I ON I.InstanceID = CD.InstanceID
JOIN dbo.DBADashAgent IA ON I.ImportAgentID = IA.DBADashAgentID
JOIN dbo.DBADashAgent CA ON I.CollectAgentID = CA.DBADashAgentID
WHERE EXISTS(SELECT 1 FROM Statuses s WHERE CD.Status=s.Status)
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,'','') ss WHERE SS.value =  CD.InstanceID)' END + '
' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND I.ShowInSummary=1' END + ''

EXEC sp_executesql @SQL,
				N'@InstanceIDs VARCHAR(MAX),@IncludeCritical BIT,@IncludeWarning BIT,@IncludeNA BIT,@IncludeOK BIT',
				@InstanceIDs,@IncludeCritical,@IncludeWarning,@IncludeNA,@IncludeOK