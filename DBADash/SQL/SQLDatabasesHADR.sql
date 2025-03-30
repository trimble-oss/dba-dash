IF OBJECT_ID('sys.dm_hadr_database_replica_states') IS NOT NULL
BEGIN
	DECLARE @OffsetConversionTemplate NVARCHAR(MAX)
	SELECT @OffsetConversionTemplate = CASE WHEN CAST(SERVERPROPERTY('ProductMajorVersion') AS INT)>=16 OR SERVERPROPERTY('EngineEdition') IN(5,8) 
										THEN '{column_name} AT TIME ZONE CURRENT_TIMEZONE_ID() AS {column_name}' 
										ELSE 'TODATETIMEOFFSET({column_name}, DATEPART(TZOFFSET,SYSDATETIMEOFFSET())) AS {column_name}'
										END										

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
		   ' + CASE WHEN @@VERSION LIKE '%SQL Server 2012%' OR @@VERSION LIKE '%SQL Server 2014%' THEN 'CAST(NULL as BIGINT) AS secondary_lag_seconds' ELSE 'secondary_lag_seconds' END + ',
		   ' + REPLACE(@OffsetConversionTemplate,'{column_name}','last_sent_time') + ',
		   ' + REPLACE(@OffsetConversionTemplate,'{column_name}','last_received_time') + ',
		   ' + REPLACE(@OffsetConversionTemplate,'{column_name}','last_hardened_time') + ',
		   ' + REPLACE(@OffsetConversionTemplate,'{column_name}','last_redone_time') + ',
		   log_send_queue_size,
		   log_send_rate,
		   redo_queue_size,
		   redo_rate,	
		   filestream_send_rate,
		   ' + REPLACE(@OffsetConversionTemplate,'{column_name}','last_commit_time') + '		   
    FROM sys.dm_hadr_database_replica_states;'
	EXEC sp_executesql @SQL
END;
