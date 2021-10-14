CREATE PROC dbo.DatabasesByInstance_Get(
	@Instance SYSNAME
) 
AS
SELECT D.DatabaseID,
		D.name,
		O.ObjectID,
		D.InstanceID,
		CASE WHEN D.state = 1 THEN 'Restoring...' 
				WHEN D.state=2 THEN 'Recovering...' 
				WHEN D.state=3 THEN 'Recovery Pending' 
				WHEN D.state = 4 THEN 'Suspect' 
				WHEN D.state=5 THEN 'Emergency' 
				WHEN D.state =6 THEN 'Offline' 
				WHEN D.state =7 THEN 'Copying' 
				WHEN D.state=10 THEN 'Offline Secondary'
				WHEN D.is_in_standby=1 THEN 'Standby / Read-Only'
				WHEN D.is_read_only =1THEN 'Readonly'
				WHEN D.user_access=1 THEN 'Single User'
				WHEN D.user_access=2 THEN 'Restricted User'
				WHEN HADR.synchronization_state=0 THEN 'Not synchronizing'
				WHEN HADR.synchronization_state=1 THEN 'Synchronizing'
				WHEN HADR.synchronization_state=2 THEN 'Synchronized'
				WHEN HADR.synchronization_state=3 THEN 'Reverting'
				WHEN HADR.synchronization_state=4 THEN 'Initializing'
		ELSE '' END AS Status
FROM dbo.Databases D
JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
LEFT JOIN dbo.DBObjects O ON O.DatabaseID = D.DatabaseID AND O.ObjectType='DB'
LEFT JOIN dbo.DatabasesHADR HADR ON HADR.DatabaseID = D.DatabaseID AND HADR.is_local=1
WHERE I.IsActive=1
AND D.IsActive=1
AND D.source_database_id IS NULL
AND I.Instance = @Instance
ORDER BY D.Name