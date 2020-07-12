CREATE PROC [dbo].[DriveThreshold_Get](@DriveID INT,@InstanceID INT)
AS 
WITH T AS (
	SELECT DriveID,InstanceID,DriveWarningThreshold,DriveCriticalThreshold,DriveCheckType,CASE WHEN @DriveID=-1 AND @InstanceID =-1 THEN 'Root' WHEN @DriveID=-1 THEN 'Instance' ELSE 'Drive' END AS ConfiguredLevel,1 AS LevelSort,CAST(0 AS BIT) AS Inherited
	FROM dbo.DriveThresholds
	WHERE DriveID = @DriveID 
	AND InstanceID= @InstanceID
	UNION ALL
	SELECT DriveID,InstanceID,DriveWarningThreshold,DriveCriticalThreshold,DriveCheckType,'Instance',2 AS LevelSort,CAST(1 AS BIT) AS Inherited
	FROM dbo.DriveThresholds
	WHERE InstanceID= @InstanceID
	AND  DriveID=-1
	UNION ALL
	SELECT DriveID,InstanceID,DriveWarningThreshold,DriveCriticalThreshold,DriveCheckType,'Root',3 AS LevelSort,CAST(1 AS BIT) AS Inherited
	FROM dbo.DriveThresholds
	WHERE InstanceID= -1
	AND DriveID=-1
)
SELECT TOP(1) T.DriveID,
              T.InstanceID,
              T.DriveWarningThreshold,
              T.DriveCriticalThreshold,
              T.DriveCheckType,
			  T.ConfiguredLevel,
              T.LevelSort,
			  T.Inherited
FROM T 
ORDER BY LevelSort