DECLARE @SQL NVARCHAR(MAX)
DECLARE @IsAzure BIT
SELECT @IsAzure = CASE WHEN SERVERPROPERTY('EditionID') =1674378470 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
IF OBJECT_ID('sys.dm_exec_procedure_stats') IS NOT NULL
BEGIN
	SET @SQL = N'
			SELECT object_id,
				   database_id,
				   ISNULL(OBJECT_NAME(object_id, database_id),'''') object_name,
				   total_worker_time,
				   total_elapsed_time,
				   total_logical_reads,
				   total_logical_writes,
				   total_physical_reads,
				   cached_time,
				   execution_count,
				   GETUTCDATE() AS current_time_utc,
				   type,
				   ISNULL(OBJECT_SCHEMA_NAME(object_id, database_id),'''') AS schema_name
			FROM sys.dm_exec_procedure_stats
			WHERE database_id ' + CASE WHEN @IsAzure=1 THEN '= DB_ID()' ELSE '<> 32767' END + '
			'
END
IF OBJECT_ID('sys.dm_exec_function_stats') IS NOT NULL
BEGIN
	SET @SQL = @SQL + 'UNION ALL
		SELECT object_id,
		   database_id,
		   ISNULL(OBJECT_NAME(object_id, database_id),'''') object_name,
		   total_worker_time,
		   total_elapsed_time,
		   total_logical_reads,
		   total_logical_writes,
		   total_physical_reads,
		   cached_time,
		   execution_count,
		   GETUTCDATE() AS current_time_utc,
		   type,
		   ISNULL(OBJECT_SCHEMA_NAME(object_id,database_id),'''') schema_name
	FROM sys.dm_exec_function_stats
	WHERE database_id ' + CASE WHEN @IsAzure=1 THEN '= DB_ID()' ELSE '<> 32767' END + '
	'
END
IF OBJECT_ID('sys.dm_exec_trigger_stats') IS NOT NULL
BEGIN
	SET @SQL = @SQL + 'UNION ALL
		SELECT object_id,
		   database_id,
		   ISNULL(OBJECT_NAME(object_id, database_id),'''') object_name,
		   total_worker_time,
		   total_elapsed_time,
		   total_logical_reads,
		   total_logical_writes,
		   total_physical_reads,
		   cached_time,
		   execution_count,
		   GETUTCDATE() AS current_time_utc,
		   type,
		   ISNULL(OBJECT_SCHEMA_NAME(object_id,database_id),'''') schema_name
	FROM sys.dm_exec_trigger_stats
	WHERE database_id ' + CASE WHEN @IsAzure=1 THEN '= DB_ID()' ELSE '<> 32767' END + '
	'
END
PRINT @SQL
EXEC sp_executesql @SQL