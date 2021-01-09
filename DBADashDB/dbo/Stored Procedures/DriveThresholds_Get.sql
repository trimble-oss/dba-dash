CREATE PROC DriveThresholds_Get(@InstanceID INT=NULL,@DriveID INT=NULL)
AS
SELECT DT.InstanceID,
       DT.DriveID,
	   CASE WHEN DT.InstanceID=-1 THEN '{ALL}' ELSE I.Instance END AS Instance,
	   CASE WHEN DT.DriveID =-1 THEN '{ALL}' ELSE D.Name END AS Drive,
       DT.DriveWarningThreshold,
       DT.DriveCriticalThreshold,
       DT.DriveCheckType
FROM dbo.DriveThresholds DT
LEFT JOIN dbo.Instances I ON I.InstanceID = DT.InstanceID
LEFT JOIN dbo.Drives D ON D.DriveID = DT.DriveID
WHERE (DT.DriveID = @DriveID OR @DriveID IS NULL)
AND (DT.InstanceID=@InstanceID OR @InstanceID IS NULL)
OPTION (RECOMPILE)