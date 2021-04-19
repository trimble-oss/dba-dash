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
           suspend_reason,
		   replica_id,
		   group_id,
		   is_commit_participant,
		   database_state,
		   is_local,
		   ' + CASE WHEN @@VERSION LIKE '%SQL Server 2012%' OR @@VERSION LIKE '%SQL Server 2014%' THEN 'CAST(NULL as BIGINT) AS secondary_lag_seconds' ELSE 'secondary_lag_seconds' END + '
    FROM sys.dm_hadr_database_replica_states;'
	EXEC sp_executesql @SQL
END;
