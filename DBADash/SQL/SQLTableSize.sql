/*
DECLARE @SizeThresholdMB INT = 100 /* Table is excluded if the size is under this threshold */
DECLARE @TableSizeDatabases NVARCHAR(MAX)='*' /* Comma separated list of databases to include. - character can be used to exclude */
DECLARE @MaxDatabases INT = 500 /* If server has a large number of databases, fail the collection */
DECLARE @MaxTables INT = 2000 /* Skip databases that have a very large number of tables */
*/
SET NOCOUNT ON
DECLARE @SizeThresholdPages INT = @SizeThresholdMB * 128
CREATE TABLE #tablesize ( 
		[DB] nvarchar(128) NOT NULL, 
		[database_id] smallint NOT NULL, 
		[schema_name] nvarchar(128) NOT NULL, 
		[object_name] nvarchar(128) NOT NULL, 
		[object_id] int NOT NULL, 
		[type] char(2) NOT NULL, 
		[row_count] bigint NOT NULL, 
		[reserved_pages] bigint, 
		[used_pages] bigint, 
		[data_pages] bigint 
)
CREATE TABLE #Databases(
	name NVARCHAR(128),

)

INSERT INTO #Databases(name)
SELECT D.name
FROM sys.databases D
WHERE D.state  = 0
AND HAS_DBACCESS(D.name)=1
AND D.database_id <> 2
AND EXISTS(
		SELECT 1 
		FROM STRING_SPLIT(@TableSizeDatabases,',') SS
		WHERE (TRIM(SS.value) = D.name OR SS.value ='*')
)
AND NOT EXISTS(
			SELECT 1 
			FROM STRING_SPLIT(@TableSizeDatabases,',') SS
			WHERE STUFF(TRIM(SS.value),1,1,'') = D.name 
			AND TRIM(SS.value) LIKE '-%'
	)

IF @@ROWCOUNT > @MaxDatabases
BEGIN
	RAISERROR('Max databases exceeded for Table Size collection',11,1)
	RETURN
END

DECLARE @SQL NVARCHAR(MAX)
DECLARE @DBName SYSNAME
DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
	SELECT D.name
	FROM #Databases D

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN
	SET @SQL =  N'USE ' + QUOTENAME(@DBName)  + '
	IF (SELECT COUNT(*) 
		FROM (
				/* 
					Using TOP to avoid running expensive has_access check more often then needed.  
					Useful if there is a very large number of tables and the user isn''t db_owner
				*/
				SELECT TOP(@MaxTables+1) object_id 
				FROM sys.objects 
				WHERE type = ''U''
			 ) T
		) > @MaxTables
	BEGIN
		SELECT	DB_NAME() AS DB,
			DB_ID() as database_id,
			''{DBADashError}'' as schema_name,
			''{TableCountExceededThreshold}'' as object_name,
			-1 AS object_id,
			''E'' AS type,
			-1 AS row_count,
			-1 AS reserved_pages,
			-1 AS used_pages,
			-1 AS data_pages
		RETURN
	END
	SELECT	DB_NAME() AS DB,
			DB_ID() as database_id,
			s.name as schema_name,
			so.name as object_name,
			ps.object_id,
			so.type,
			SUM (CASE WHEN (ps.index_id < 2) THEN row_count ELSE 0 END) AS row_count,
			SUM (ps.reserved_page_count) AS reserved_pages,
			SUM (ps.used_page_count) AS used_pages,
			SUM (CASE WHEN (ps.index_id < 2) THEN (ps.in_row_data_page_count + ps.lob_used_page_count + ps.row_overflow_used_page_count)
						ELSE ps.lob_used_page_count + ps.row_overflow_used_page_count
						END
					) AS data_pages
	FROM sys.dm_db_partition_stats ps
	INNER JOIN sys.objects so ON ps.object_id = so.object_id
	INNER JOIN sys.schemas s ON so.schema_id = s.schema_id
	WHERE so.type = ''U''
	GROUP BY ps.object_id,so.name,so.type_desc,s.name,so.type
	HAVING SUM (ps.used_page_count) > @SizeThresholdPages'


	IF HAS_DBACCESS(@DBName)=1
	BEGIN
		INSERT INTO #tablesize ([DB], [database_id], [schema_name], [object_name], [object_id], [type], [row_count], [reserved_pages], [used_pages], [data_pages])
		EXEC sp_executesql  @SQL,N'@SizeThresholdPages INT,@MaxTables INT',@SizeThresholdPages,@MaxTables
	END

	FETCH NEXT FROM DBs INTO @DBName
END
CLOSE DBs
DEALLOCATE DBs

SELECT SYSUTCDATETIME() AS SnapshotDate,*
FROM #tablesize
