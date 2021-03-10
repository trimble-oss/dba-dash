DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #DBConfig( 
	database_id INT NOT NULL,
	configuration_id INT NOT NULL,
	name NVARCHAR(60) NOT NULL,
	value NVARCHAR(128) NULL,
	value_for_secondary NVARCHAR(128) NULL,
	PRIMARY KEY (database_id,configuration_id)
)

IF OBJECT_ID('sys.database_scoped_configurations') IS NOT NULL
BEGIN
	DECLARE DBs CURSOR FAST_FORWARD READ_ONLY FOR
	SELECT name
	FROM sys.databases
	WHERE state  = 0
	AND DATABASEPROPERTYEX(name, 'Updateability') = 'READ_WRITE'
	AND HAS_DBACCESS(name)=1

	OPEN DBs
	FETCH NEXT FROM DBs INTO @DBName

	WHILE @@FETCH_STATUS = 0
	BEGIN

	SET @SQL =  N'USE ' + QUOTENAME(@DBName) + ';
	SELECT DB_ID(),configuration_id,name,CAST(value as NVARCHAR(128)),CAST(value_for_secondary as NVARCHAR(128))
	FROM sys.database_scoped_configurations'

	INSERT INTO #DBConfig
	EXEC  (	@SQL )

	FETCH NEXT FROM DBs INTO @DBName
	END
	CLOSE DBs
	DEALLOCATE DBs

END


SELECT database_id,
       configuration_id,
       name,
       value,
       value_for_secondary
FROM #DBConfig

DROP TABLE #DBConfig