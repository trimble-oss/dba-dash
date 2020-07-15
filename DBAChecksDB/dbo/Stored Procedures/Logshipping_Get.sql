CREATE PROC [dbo].[Logshipping_Get](
		@InstanceIDs VARCHAR(MAX)=NULL,
		@IncludeCritical BIT=1,
		@IncludeWarning BIT=1,
		@IncludeNA BIT=0,
		@IncludeOK BIT=0
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

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
SELECT LSS.InstanceID,
	   LSS.DatabaseID,
       LSS.Instance,
       LSS.name,
       LSS.restore_date,
       LSS.backup_start_date,
       LSS.TimeSinceLast,
       LSS.LatencyOfLast,
       LSS.TotalTimeBehind,
       LSS.SnapshotAge,
       LSS.LogRestoresDate,
       LSS.Status,
       LSS.StatusDescription,
	   f.FileName AS last_file,
	   LSS.ThresholdConfiguredLevel,
	   CASE WHEN LSS.SnapshotAge>1440 THEN 1 WHEN LSS.SnapshotAge>120 THEN 2 WHEN LSS.SnapshotAge<60 THEN 4 ELSE 3 END AS SnapshotAgeStatus
FROM dbo.LogShippingStatus LSS
CROSS APPLY dbo.ParseFileName(LSS.last_file) f
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = LSS.InstanceID)
AND EXISTS(SELECT 1 FROM Statuses s WHERE LSS.Status=s.Status)
ORDER BY LSS.Status,LSS.TotalTimeBehind DESC
OPTION(RECOMPILE)