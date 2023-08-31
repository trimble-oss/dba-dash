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
	/*	
		Only query sys.dm_db_mirroring_auto_page_repair if database mirroring is in use.   
		* Query can be blocked by a database restore even if mirroring is not used
		* Query can generate a dump if not run as sysadmin and there are databases in a RESTORING state. #682
	*/
	IF EXISTS(SELECT 1 FROM sys.database_mirroring WHERE mirroring_role IS NOT NULL)
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