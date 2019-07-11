CREATE PROC Report.SnapshotDates(@InstanceIDs VARCHAR(MAX)=NULL)
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
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END

SELECT I.Instance, SSD.AgentJobsDate,
				  DATEDIFF(mi,SSD.AgentJobsDate,GETUTCDATE()) AgentJobSnapshotAge,
                  SSD.BackupsDate,
				  DATEDIFF(mi,SSD.BackupsDate,GETUTCDATE()) BackupsSnapshotAge,
                  SSD.DatabasesDate,
				  DATEDIFF(mi,SSD.DatabasesDate,GETUTCDATE()) DatabaseSnapshotAge,
                  SSD.DrivesDate,
				  DATEDIFF(mi,SSD.DrivesDate,GETUTCDATE()) DrivesSnapshotAge,
                  SSD.LogRestoresDate,
				  DATEDIFF(mi,SSD.LogRestoresDate,GETUTCDATE()) LogRestoresSnapshotAge,
                  SSD.DBFilesDate,
				  DATEDIFF(mi,SSD.DBFilesDate,GETUTCDATE()) DBFilesSnapshotAge,
                  SSD.ServerPropertiesDate,
				  DATEDIFF(mi,SSD.ServerPropertiesDate,GETUTCDATE()) ServerPropertiesSnapshotAge,
                  SSD.InstanceDate,
				  DATEDIFF(mi,SSD.DatabasesDate,GETUTCDATE()) InstanceSnapshotAge
FROM dbo.Instances I 
JOIN dbo.SnapshotDates SSD ON SSD.InstanceID = I.InstanceID
WHERE I.IsActive=1
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)