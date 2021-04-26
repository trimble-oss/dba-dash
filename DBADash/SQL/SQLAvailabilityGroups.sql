DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT group_id,
       name,
       resource_id,
       resource_group_id,
       failure_condition_level,
       health_check_timeout,
       automated_backup_preference,
       version,
       basic_features,
       dtc_support,
       db_failover,
       is_distributed,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'cluster_type','ColumnID') IS NULL THEN ' CAST(NULL as BIGINT) AS cluster_type,' ELSE 'cluster_type,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'required_synchronized_secondaries_to_commit','ColumnID') IS NULL THEN ' CAST(NULL as BIGINT) AS required_synchronized_secondaries_to_commit,' ELSE 'required_synchronized_secondaries_to_commit,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'sequence_number','ColumnID') IS NULL THEN ' CAST(NULL as BIGINT) AS sequence_number,' ELSE 'sequence_number,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.availability_groups'),'is_contained','ColumnID') IS NULL THEN ' CAST(NULL as BIT) AS is_contained' ELSE 'is_contained' END + '
FROM sys.availability_groups;'
       
EXEC sp_executesql @SQL
