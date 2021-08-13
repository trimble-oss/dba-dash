CREATE PROC dbo.InstanceUptimeThresholds_Get(
	@InstanceID INT 
)
AS
SELECT CASE WHEN @InstanceID=-1 THEN '{Root}' ELSE I.Instance END AS Instance,
	CASE WHEN T.InstanceID IS NULL THEN RT.WarningThreshold ELSE T.WarningThreshold END AS WarningThreshold,
	CASE WHEN T.InstanceID IS NULL THEN RT.CriticalThreshold ELSE T.CriticalThreshold END AS CriticalThreshold,
	CASE WHEN T.InstanceID IS NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsInherited,
	DATEADD(mi,I.UTCOffset,I.sqlserver_start_time) AS sqlserver_start_time_utc,
	I.sqlserver_start_time,
	I.UptimeAckDate
FROM dbo.InstanceUptimeThresholds RT  
LEFT JOIN dbo.Instances I ON I.InstanceID = @InstanceID
LEFT JOIN dbo.InstanceUptimeThresholds T ON T.InstanceID = @InstanceID
WHERE RT.InstanceID = -1