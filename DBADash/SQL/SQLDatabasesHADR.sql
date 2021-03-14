IF OBJECT_ID('sys.dm_hadr_database_replica_states') IS NOT NULL
BEGIN
	DECLARE @SQL NVARCHAR(MAX)
	SET @SQL =N'
    SELECT database_id,
           group_database_id,
           ' + CASE WHEN @@VERSION LIKE '%SQL Server 2012%' THEN 'CAST(NULL as BIT) AS is_primary_replica,' ELSE 'is_primary_replica,' END + '
           synchronization_state,
           synchronization_health,
           is_suspended,
           suspend_reason
    FROM sys.dm_hadr_database_replica_states
    WHERE is_local = 1;'
	EXEC sp_executesql @SQL
END;