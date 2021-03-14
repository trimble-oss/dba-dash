SET NOCOUNT ON
DECLARE @EditionID BIGINT
SELECT @EditionID = CAST(SERVERPROPERTY('EditionID') as bigint) 

DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #DBPrincipals
(
	database_id INT NOT NULL,
    [name] NVARCHAR(128),
    [principal_id] INT,
    [type] CHAR(1),
    [type_desc] NVARCHAR(60),
    [default_schema_name] NVARCHAR(128),
    [create_date] DATETIME,
    [modify_date] DATETIME,
    [owning_principal_id] INT,
    [sid] VARBINARY(85),
    [is_fixed_role] BIT,
    [authentication_type] INT,
    [authentication_type_desc] NVARCHAR(60),
    [default_language_name] NVARCHAR(128),
    [default_language_lcid] INT,
    [allow_encrypted_value_modifications] BIT
);

DECLARE @PrincipalsSQL NVARCHAR(MAX)
SET @PrincipalsSQL = N'SELECT DB_ID(), 
	   name,
       principal_id,
       type,
       type_desc,
       default_schema_name,
       create_date,
       modify_date,
       owning_principal_id,
       sid,
       is_fixed_role,
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_principals'),'authentication_type','ColumnID') IS NULL THEN 'CAST(NULL as INT) as authentication_type,' ELSE 'authentication_type,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_principals'),'authentication_type_desc','ColumnID') IS NULL THEN 'CAST(NULL as NVARCHAR(60)) as authentication_type_desc,' ELSE 'authentication_type_desc,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_principals'),'default_language_name','ColumnID') IS NULL THEN 'CAST(NULL as SYSNAME) as default_language_name,' ELSE 'default_language_name,' END + '
	   ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_principals'),'default_language_lcid','ColumnID') IS NULL THEN 'CAST(NULL as NVARCHAR(60)) as default_language_lcid,' ELSE 'default_language_lcid,' END + '
       ' + CASE WHEN COLUMNPROPERTY(OBJECT_ID('sys.database_principals'),'allow_encrypted_value_modifications','ColumnID') IS NULL THEN 'CAST(NULL as BIT) as allow_encrypted_value_modifications' ELSE 'allow_encrypted_value_modifications' END + ' 
FROM sys.database_principals'

DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
SELECT name
FROM sys.databases
WHERE state  = 0
AND HAS_DBACCESS(name)=1

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN

	SET @SQL =  N'USE ' + QUOTENAME(@DBName) + ';' + @PrincipalsSQL
	IF HAS_DBACCESS(@DBName)=1
	BEGIN
		INSERT INTO #DBPrincipals
		EXEC  (	@SQL )
	END

	FETCH NEXT FROM DBs INTO @DBName
END
CLOSE DBs
DEALLOCATE DBs

SELECT database_id,
	   name,
       principal_id,
       type,
       type_desc,
       default_schema_name,
       create_date,
       modify_date,
       owning_principal_id,
       sid,
       is_fixed_role,
       authentication_type,
       authentication_type_desc,
       default_language_name,
       default_language_lcid,
       allow_encrypted_value_modifications
FROM #DBPrincipals

DROP TABLE #DBPrincipals