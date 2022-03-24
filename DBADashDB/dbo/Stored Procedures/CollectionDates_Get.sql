CREATE PROC dbo.CollectionDates_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=1,
	@IncludeOK BIT=1
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
       CD.ConfiguredLevel
FROM dbo.CollectionDatesStatus CD
JOIN dbo.Instances I ON I.InstanceID = CD.InstanceID
WHERE EXISTS(SELECT 1 FROM Statuses s WHERE CD.Status=s.Status)
' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'AND EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,'','') ss WHERE SS.value =  CD.InstanceID)' END

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX),@IncludeCritical BIT,@IncludeWarning BIT,@IncludeNA BIT,@IncludeOK BIT',@InstanceIDs,@IncludeCritical,@IncludeWarning,@IncludeNA,@IncludeOK