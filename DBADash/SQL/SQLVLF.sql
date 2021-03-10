IF OBJECT_ID('sys.dm_db_log_info') IS NOT NULL
BEGIN
	SELECT d.database_id,COUNT(*) as VLFCount
	FROM sys.databases d 
	CROSS APPLY sys.dm_db_log_info(d.database_id)
	GROUP BY d.database_id
END
ELSE
BEGIN
	DECLARE @DBName SYSNAME
	DECLARE @SQL NVARCHAR(MAX)
	CREATE TABLE #VLF( 
		database_id INT NOT NULL,
		VLFCount INT NOT NULL,
		PRIMARY KEY (database_id)
	)

	DECLARE @ProductMajorVersion INT
	SET @ProductMajorVersion = CAST(SERVERPROPERTY('ProductMajorVersion') as INT)

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
	DECLARE @LogInfo TABLE(
		' + CASE WHEN @ProductMajorVersion>=11 THEN 'recoveryunitid INT ,' ELSE '' END + '
		FileID SMALLINT ,
		FileSize BIGINT ,
		StartOffset BIGINT ,
		FSeqNo BIGINT ,
		[Status] TINYINT ,
		Parity TINYINT ,
		CreateLSN NUMERIC(38)
		);
	INSERT INTO @LOGINFO
	EXEC sp_executesql N''DBCC LOGINFO() WITH NO_INFOMSGS'';
	INSERT INTO #VLF(database_id,VLFCount)
	SELECT DB_ID(),@@ROWCOUNT'

	EXEC  (	@SQL )

	FETCH NEXT FROM DBs INTO @DBName
	END
	CLOSE DBs
	DEALLOCATE DBs

	SELECT database_id,VLFCount
	FROM #VLF
END