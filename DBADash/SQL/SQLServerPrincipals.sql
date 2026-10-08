DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT name,
       principal_id,
       sid,
       type,
       type_desc,
       is_disabled,
       create_date,
       modify_date,
       default_database_name,
       default_language_name,
       credential_id,
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.server_principals'),'owning_principal_id','ColumnID') IS NULL THEN 'CAST(NULL AS INT) as owning_principal_id,' ELSE 'owning_principal_id,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.server_principals'),'is_fixed_role','ColumnID') IS NULL THEN 'CASE WHEN type=''R'' AND name IN(''sysadmin'',''securityadmin'',''serveradmin'',''setupadmin'',''processadmin'',''diskadmin'',''dbcreator'',''bulkadmin'') THEN CAST(1 as BIT) ELSE CAST(0 as BIT) END as is_fixed_role' ELSE 'is_fixed_role' END + ',
	   /* Treat 1900-01-01 as unknown. */
	   CASE WHEN PWD.PasswordLastSetTime >= ''19000102'' THEN PWD.PasswordLastSetTime ELSE NULL END AS password_last_set_time
FROM sys.server_principals
OUTER APPLY(SELECT CONVERT(DATETIME,LOGINPROPERTY(name,''PasswordLastSetTime'')) AS PasswordLastSetTime
			WHERE type = ''S''
			) PWD'

exec sp_executesql @SQL