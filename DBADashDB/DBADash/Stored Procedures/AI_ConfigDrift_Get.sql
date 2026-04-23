CREATE PROC DBADash.AI_ConfigDrift_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 168
)
AS
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	o.name AS ConfigName,
	h.value AS PreviousValue,
	h.new_value AS NewValue,
	h.ValidTo AS ChangedUtc,
	o.description
FROM dbo.SysConfigHistory h
INNER JOIN dbo.SysConfigOptions o ON o.configuration_id = h.configuration_id
INNER JOIN dbo.Instances i ON i.InstanceID = h.InstanceID
WHERE h.ValidTo >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY h.ValidTo DESC
