DECLARE @SQL NVARCHAR(MAX)
DECLARE @DBName SYSNAME
DECLARE @DBID INT
IF DATABASEPROPERTYEX(DB_NAME(),'LastGoodCheckDbTime') IS NULL AND IS_SRVROLEMEMBER('sysadmin')=1
BEGIN
	DECLARE DBs CURSOR FAST_FORWARD READ_ONLY FOR
		SELECT name,database_id
		FROM sys.databases
		WHERE state  = 0
		AND DATABASEPROPERTYEX(name, 'Updateability') = 'READ_WRITE'

	DECLARE @dbinfo TABLE
			( ParentObject VARCHAR(255) ,
			  Object VARCHAR(255) ,
			  Field VARCHAR(255) ,
			  Value VARCHAR(255) 
			);
	DECLARE @LastGoodDBCC TABLE(
		database_id INT PRIMARY KEY,
		LastGoodCheckDbTime DATETIME
	)

	OPEN DBs

	WHILE 1=1
	BEGIN
		FETCH NEXT FROM DBs INTO @DBName,@DBID
		IF @@FETCH_STATUS<>0
			BREAK

		SET @SQL =  N'USE ' + QUOTENAME(@DBName) + ';
		DBCC DBINFO() WITH TABLERESULTS, NO_INFOMSGS'

		INSERT INTO @dbinfo(ParentObject,Object,Field,Value)
		EXEC  (	@SQL )

		INSERT INTO @LastGoodDBCC(database_id,LastGoodCheckDbTime)
		SELECT TOP(1) @DBID, CONVERT(DATETIME,Value,120)
		FROM @dbinfo
		WHERE Field = 'dbi_dbccLastKnownGood'

		DELETE @dbinfo
	END
	CLOSE DBs
	DEALLOCATE DBs

	SELECT database_id,LastGoodCheckDbTime
	FROM @LastGoodDBCC

END
ELSE
BEGIN
	SELECT database_id,CAST(DATABASEPROPERTYEX(name,'LastGoodCheckDbTime') as DATETIME) as LastGoodCheckDbTime
	FROM sys.databases
END