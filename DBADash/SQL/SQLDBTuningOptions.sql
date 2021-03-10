DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #autotune( 
	database_id INT NOT NULL,
	[name] nvarchar(128), 
	[desired_state_desc] nvarchar(60), 
	[actual_state_desc] nvarchar(60), 
	[reason_desc] nvarchar(60) 
	PRIMARY KEY (database_id,name)
)

IF OBJECT_ID('sys.database_automatic_tuning_options') IS NOT NULL
BEGIN
	DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
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
	SELECT DB_ID(), name, desired_state_desc, actual_state_desc, reason_desc
	FROM sys.database_automatic_tuning_options;'

	INSERT INTO #autotune(database_id,name,desired_state_desc,actual_state_desc,reason_desc)
	EXEC  (	@SQL )

	FETCH NEXT FROM DBs INTO @DBName
	END
	CLOSE DBs
	DEALLOCATE DBs

END


SELECT database_id,
       name,
       desired_state_desc,
       actual_state_desc,
       reason_desc 
FROM #autotune

DROP TABLE #autotune