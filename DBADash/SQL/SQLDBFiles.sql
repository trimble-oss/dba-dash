DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #FileList ( 
	database_id INT,
	file_id INT,
	data_space_id INT,
	name SYSNAME,
	filegroup_name SYSNAME NULL,
	physical_name nvarchar(260),
	type TINYINT,
	size bigint,
	space_used bigint,
	max_size bigint,
	growth bigint,
	is_percent_growth bit,
	is_read_only BIT,
	state TINYINT
)

DECLARE DBs CURSOR FAST_FORWARD LOCAL FOR
SELECT name
FROM sys.databases
WHERE state  = 0
AND DATABASEPROPERTYEX(name, 'Updateability') = 'READ_WRITE'
AND HAS_DBACCESS(name)=1

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN

PRINT @DBName
SET @SQL =  N'USE ' + QUOTENAME(@DBName) + ';
SELECT
	DB_ID() database_id,
	file_id,
	f.data_space_id,
	f.name,
	CASE WHEN f.type=1 THEN ''LOG'' ELSE ISNULL(fg.name,f.name) END as filegroup_name,
	f.physical_name,
	f.type,
	f.size,
	CASE WHEN f.type=2 THEN f.size ELSE FILEPROPERTY(f.name,''spaceused'') END as spaceused,
	f.max_size,
	f.growth,
	f.is_percent_growth,
	f.is_read_only,
	f.state
FROM sys.database_files f
LEFT JOIN sys.filegroups fg on f.data_space_id = fg.data_space_id
WHERE f.type_desc <> ''FULLTEXT'''

INSERT INTO #FileList 
EXEC  (	@SQL )

FETCH NEXT FROM DBs INTO @DBName
END
CLOSE DBs
DEALLOCATE DBs

IF OBJECT_ID('sys.master_files') IS NULL
BEGIN
	SELECT database_id,
       file_id,
       data_space_id,
       name,
       filegroup_name,
       physical_name,
       type,
       size,
       space_used,
       max_size,
       growth,
       is_percent_growth,
       is_read_only,
	   state
	FROM #FileList
END
ELSE
BEGIN
	SELECT database_id,
		   file_id,
		   data_space_id,
		   name,
		   filegroup_name,
		   physical_name,
		   type,
		   size,
		   space_used,
		   max_size,
		   growth,
		   is_percent_growth,
		   is_read_only,
		   state
	FROM #FileList
	UNION ALL	
	SELECT database_id,
			file_id,
			data_space_id,
			name,
			CASE WHEN type=1 THEN 'LOG' ELSE NULL END AS filegroup_name,
			physical_name,
			type,
			CASE WHEN mf.state=3 THEN 0 ELSE size END,
			NULL AS SpaceUsed,
			max_size,
			growth,
			is_percent_growth,
			Is_Read_Only,
			state
	FROM sys.master_files mf
	WHERE NOT EXISTS(SELECT 1 FROM #FileList fl WHERE fl.database_id = mf.database_id AND fl.file_id = mf.file_id)
END
DROP TABLE #FileList