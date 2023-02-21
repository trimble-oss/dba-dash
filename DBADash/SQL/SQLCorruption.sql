SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED
DECLARE @Corruption TABLE(
		SourceTable TINYINT NOT NULL,
		database_id INT NOT NULL,
		last_update_date DATETIME NOT NULL,
		CountOfRows INT NOT NULL
)
IF OBJECT_ID('msdb.dbo.suspect_pages') IS NOT NULL
BEGIN
	INSERT INTO @Corruption(SourceTable,database_id,last_update_date,CountOfRows)
	SELECT CAST(1 AS TINYINT) AS SourceTable,
		   database_id,
		   MAX(last_update_date) last_update_date,
		   COUNT(*) AS CountOfRows
	FROM msdb.dbo.suspect_pages
	GROUP BY database_id
END
IF OBJECT_ID('msdb.sys.dm_db_mirroring_auto_page_repair') IS NOT NULL
BEGIN
	--Query can be blocked by a database restore even if mirroring is not used, so check if mirroring is being used before running this query
	IF EXISTS(select * from sys.database_mirroring_endpoints)
	BEGIN
		INSERT INTO @Corruption(SourceTable,database_id,last_update_date,CountOfRows)
		SELECT CAST(2 AS TINYINT) AS SourceTable,
			   database_id,
			   MAX(modification_time),
			   COUNT(*) AS CountOfRows
		FROM sys.dm_db_mirroring_auto_page_repair
		GROUP BY database_id
	END
END
IF OBJECT_ID('msdb.sys.dm_hadr_auto_page_repair') IS NOT NULL
BEGIN
	INSERT INTO @Corruption(SourceTable,database_id,last_update_date,CountOfRows)
	SELECT CAST(3 AS TINYINT) AS SourceTable,
		   database_id,
		   MAX(modification_time),
		   COUNT(*) AS CountOfRows
	FROM sys.dm_hadr_auto_page_repair
	GROUP BY database_id;
END
SELECT SourceTable,
       database_id,
       last_update_date,
	   CountOfRows
FROM @Corruption