CREATE PROC [dbo].[LogRestoreThresholds_Get](@InstanceID INT=NULL,@DatabaseID INT=NULL)
AS
SELECT T.InstanceID,
       T.DatabaseID,
	   CASE WHEN T.InstanceID=-1 THEN '{ALL}' ELSE I.Instance END AS Instance,
	   CASE WHEN T.DatabaseID=-1 THEN '{ALL}' ELSE D.Name END AS DatabaseName,
       T.LatencyCriticalThreshold,
       T.LatencyWarningThreshold,
       T.TimeSinceLastCriticalThreshold,
	   T.TimeSinceLastWarningThreshold
FROM dbo.LogRestoreThresholds T
LEFT JOIN dbo.Instances I ON I.InstanceID = T.InstanceID
LEFT JOIN dbo.Databases D ON D.DatabaseID = T.DatabaseID
WHERE (T.DatabaseID = @DatabaseID OR @DatabaseID IS NULL)
AND (T.InstanceID=@InstanceID OR @InstanceID IS NULL)
OPTION (RECOMPILE)