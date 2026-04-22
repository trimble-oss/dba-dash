CREATE PROC DBADash.AI_InstanceMetadata_Get(
	@MaxRows INT = 1000,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = NULL
)
AS
SELECT TOP (@MaxRows)
	I.InstanceDisplayName,
	I.ConnectionID,
	I.ProductVersion,
	I.ProductMajorVersion,
	I.Edition,
	I.EngineEdition,
	I.host_platform,
	I.cpu_count,
	I.socket_count,
	I.cores_per_socket,
	I.physical_memory_kb,
	CAST(I.physical_memory_kb / 1024.0 / 1024.0 AS DECIMAL(18,2)) AS PhysicalMemoryGB
FROM dbo.Instances I
WHERE I.IsActive = 1
  AND (@InstanceFilter IS NULL OR I.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY I.InstanceDisplayName
