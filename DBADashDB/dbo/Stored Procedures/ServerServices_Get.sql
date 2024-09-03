CREATE PROC dbo.ServerServices_Get(
	@InstanceIDs IDs READONLY,
	@ServiceType NVARCHAR(256)=NULL,
	@ServiceAccount NVARCHAR(256)=NULL
)
AS
SELECT	SS.InstanceID,
		I.InstanceDisplayName,
		SUBSTRING(SS.servicename,0,CHARINDEX(' (',SS.servicename)) AS service_type,		
		SS.servicename,
		SS.startup_type,
		SS.startup_type_desc,
		SS.status,
		SS.status_desc,
		SS.process_id,
		CAST(SS.last_startup_time AT TIME ZONE 'UTC' AS DATETIME2) AS last_startup_time,
		SS.service_account,
		CASE WHEN SS.service_account LIKE '%\%$' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS is_managed_service_account,
		SS.filename,
		SS.is_clustered,
		SS.cluster_nodename,
		SS.instant_file_initialization_enabled,
		CASE WHEN SS.servicename NOT LIKE 'SQL Server (%' THEN 3
			WHEN SS.instant_file_initialization_enabled = 'Y' THEN 4
			ELSE 2 END AS instant_file_initialization_enabled_status,
		SS.SnapshotDate,
		ISNULL(CDS.Status,3) AS SnapshotStatus
FROM dbo.ServerServices SS
JOIN dbo.Instances I ON I.InstanceID = SS.InstanceID
LEFT JOIN dbo.CollectionDatesStatus CDS ON CDS.InstanceID = I.InstanceID AND CDS.Reference ='ServerServices'
WHERE EXISTS(
			SELECT 1 
			FROM @InstanceIDs T 
			WHERE T.ID = SS.InstanceID
			)
AND (SS.servicename LIKE @ServiceType + ' (%' OR @ServiceType IS NULL) 
AND (SS.service_account = @ServiceAccount OR @ServiceAccount IS NULL)