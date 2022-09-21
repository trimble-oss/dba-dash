DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT group_id,
       name,
       resource_id,
       resource_group_id,
       failure_condition_level,
       health_check_timeout,
       automated_backup_preference,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'version','ColumnID') IS NULL THEN ' CAST(NULL as SMALLINT) AS version,' ELSE 'version,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'basic_features','ColumnID') IS NULL THEN ' CAST(NULL as BIT) AS basic_features,' ELSE 'basic_features,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'dtc_support','ColumnID') IS NULL THEN ' CAST(NULL as BIT) AS dtc_support,' ELSE 'dtc_support,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'db_failover','ColumnID') IS NULL THEN ' CAST(NULL as BIT) AS db_failover,' ELSE 'db_failover,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'is_distributed','ColumnID') IS NULL THEN ' CAST(NULL as BIT) AS is_distributed,' ELSE 'is_distributed,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'cluster_type','ColumnID') IS NULL THEN ' CAST(NULL as TINYINT) AS cluster_type,' ELSE 'cluster_type,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'required_synchronized_secondaries_to_commit','ColumnID') IS NULL THEN ' CAST(NULL as INT) AS required_synchronized_secondaries_to_commit,' ELSE 'required_synchronized_secondaries_to_commit,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'sequence_number','ColumnID') IS NULL THEN ' CAST(NULL as BIGINT) AS sequence_number,' ELSE 'sequence_number,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'is_contained','ColumnID') IS NULL THEN ' CAST(NULL as BIT) AS is_contained' ELSE 'is_contained' END + '
FROM sys.availability_groups;'
       
EXEC sp_executesql @SQL