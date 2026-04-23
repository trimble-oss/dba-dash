CREATE PROC DBADash.AI_SlowQueries_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = 24
)
AS
SELECT TOP (@MaxRows)
	i.InstanceDisplayName,
	ISNULL(d.name, '') AS DatabaseName,
	ISNULL(NULLIF(sq.object_name, ''), '{ad-hoc}') AS ObjectName,
	COUNT_BIG(*) AS ExecCount,
	SUM(sq.duration) / 1000000.0 AS TotalDurationSec,
	SUM(sq.cpu_time) / 1000000.0 AS TotalCpuSec,
	SUM(sq.logical_reads) AS TotalLogicalReads,
	SUM(sq.physical_reads) AS TotalPhysicalReads,
	SUM(sq.writes) AS TotalWrites,
	SUM(sq.row_count) AS TotalRowCount,
	MAX(sq.timestamp) AS LastSeenUtc,
	MAX(sq.username) AS SampleUsername,
	MAX(sq.client_app_name) AS SampleAppName,
	MAX(sq.client_hostname) AS SampleHostname,
	LEFT(MAX(sq.text), 500) AS SampleQueryText
FROM dbo.SlowQueries sq
INNER JOIN dbo.Instances i ON i.InstanceID = sq.InstanceID
LEFT JOIN dbo.Databases d ON d.DatabaseID = sq.DatabaseID
WHERE sq.timestamp >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName, ISNULL(d.name, ''), ISNULL(NULLIF(sq.object_name, ''), '{ad-hoc}')
ORDER BY TotalDurationSec DESC
