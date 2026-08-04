/*
	Decipher a wait resource string on the source instance.
	Called via messaging (DecipherWaitResourceMessage).

	Unlike the manual script shown in the GUI, this version does NOT use the undocumented DBCC PAGE command.
	For PAGE/RID resources it uses the documented sys.dm_db_page_info dynamic management function (SQL 2019+ / Azure SQL).
	The dm_db_page_info call is wrapped in dynamic SQL so this batch still parses on older versions where the function
	does not exist.  When the function is unavailable, a single row with RequiresScript = 1 is returned so the caller
	can fall back to the manual script.

	@WaitResource is supplied as a parameter.
*/
SET NOCOUNT ON;

IF @WaitResource = '0:0:0'
BEGIN
	SELECT @WaitResource AS wait_resource,
	       'https://www.sqlskills.com/blogs/paul/the-curious-case-of-what-is-the-wait-resource-000/' AS info;
	RETURN;
END

DECLARE @DBID INT,
        @DBName SYSNAME,
        @SQL NVARCHAR(MAX),
        @HOBTID BIGINT,
        @ObjectID INT,
        @FileID INT,
        @PageID INT,
        @IndexID INT,
        @idx1 INT,
        @idx2 INT,
        @PageTypeDesc NVARCHAR(60),
        @NormalizedWaitResource NVARCHAR(256);

IF @WaitResource LIKE 'KEY: %'
BEGIN
	/*
		Format KEY: DatabaseID:Hobt_id
		Use sys.partitions to map HOBTID to table/index.  Works on all versions.
	*/
	SELECT @DBID = SUBSTRING(@WaitResource, 6, CHARINDEX(':', @WaitResource, 5) - 6);
	SET @DBName = DB_NAME(@DBID);
	SELECT @HOBTID = SUBSTRING(@WaitResource,
	                           CHARINDEX(':', @WaitResource, 5) + 1,
	                           CHARINDEX(' ', @WaitResource, 6) - CHARINDEX(':', @WaitResource, 5) - 1);

	SET @SQL = 'USE ' + QUOTENAME(@DBName) + '
	SELECT	@WaitResource AS wait_resource,
			DB_NAME() AS database_name,
			s.name AS schema_name,
			o.name AS object_name,
			i.name AS index_name
	FROM (SELECT @HOBTID AS hobt_id) AS t
	LEFT JOIN sys.partitions p ON t.hobt_id = p.hobt_id
	LEFT JOIN sys.objects o ON p.object_id = o.object_id
	LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id
	LEFT JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id';

	EXEC sp_executesql @SQL, N'@HOBTID BIGINT,@WaitResource NVARCHAR(256)', @HOBTID, @WaitResource;
	RETURN;
END

IF @WaitResource LIKE 'OBJECT: %'
BEGIN
	/* Format: OBJECT: DatabaseID:ObjectID.  Works on all versions. */
	SELECT @idx1 = CHARINDEX(':', @WaitResource, 8);
	SELECT @idx2 = CHARINDEX(':', @WaitResource, @idx1 + 1);

	SELECT @DBID = SUBSTRING(@WaitResource, 9, @idx1 - 9),
	       @ObjectID = SUBSTRING(@WaitResource, @idx1 + 1, @idx2 - @idx1 - 1);

	SET @DBName = DB_NAME(@DBID);

	SET @SQL = 'USE ' + QUOTENAME(@DBName) + '
	SELECT	@WaitResource AS wait_resource,
			DB_NAME() AS database_name,
			s.name AS schema_name,
			o.name AS object_name
	FROM (SELECT @ObjectID AS object_id) AS t
	LEFT JOIN sys.objects o ON t.object_id = o.object_id
	LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id';

	EXEC sp_executesql @SQL, N'@ObjectID INT,@WaitResource NVARCHAR(256)', @ObjectID, @WaitResource;
	RETURN;
END

IF @WaitResource LIKE 'RID: %' OR @WaitResource LIKE '[0-9]%:%:%' OR @WaitResource LIKE 'PAGE: %'
BEGIN
	SET @NormalizedWaitResource = @WaitResource;
	IF @WaitResource LIKE 'RID: %'
	BEGIN
		/* Format: RID: DatabaseID:FileID:PageID:Slot(row).  Strip RID: prefix and slot. */
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource, 6, LEN(@NormalizedWaitResource));
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource, 1, LEN(@NormalizedWaitResource) - CHARINDEX(':', REVERSE(@NormalizedWaitResource)));
	END
	ELSE IF @WaitResource LIKE 'PAGE: %'
	BEGIN
		/* Format: PAGE: DatabaseID:FileID:PageID.  Strip PAGE: prefix. */
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource, 7, LEN(@NormalizedWaitResource));
	END
	ELSE IF @WaitResource LIKE '[0-9]%:%:%(%)'
	BEGIN
		/* Format: DatabaseID:FileID:PageID (Additional Info).  Strip (Additional Info). */
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource, 1, CHARINDEX('(', @NormalizedWaitResource) - 1);
	END

	SELECT @idx1 = CHARINDEX(':', @NormalizedWaitResource);
	SELECT @idx2 = CHARINDEX(':', @NormalizedWaitResource, @idx1 + 1);

	SELECT @DBID = SUBSTRING(@NormalizedWaitResource, 1, @idx1 - 1),
	       @FileID = SUBSTRING(@NormalizedWaitResource, @idx1 + 1, @idx2 - @idx1 - 1),
	       @PageID = SUBSTRING(@NormalizedWaitResource, @idx2 + 1, LEN(@NormalizedWaitResource));

	/*
		sys.dm_db_page_info is available from SQL Server 2019 (and Azure SQL).  If it isn't available, signal the
		caller to fall back to the manual script (which uses DBCC PAGE).
	*/
	IF OBJECT_ID('sys.dm_db_page_info') IS NULL
	BEGIN
		SELECT CAST(1 AS BIT) AS RequiresScript,
		       @WaitResource AS wait_resource,
		       @DBID AS database_id,
		       @FileID AS file_id,
		       @PageID AS page_id,
		       'sys.dm_db_page_info is not available on this SQL version.  Use the script option to decipher using DBCC PAGE.' AS Notes;
		RETURN;
	END

	SET @DBName = DB_NAME(@DBID);

	/* dm_db_page_info is called via dynamic SQL so this batch parses on versions where the function does not exist. */
	DECLARE @PageInfo TABLE (object_id INT, index_id INT, page_type_desc NVARCHAR(60));
	INSERT INTO @PageInfo (object_id, index_id, page_type_desc)
	EXEC sp_executesql
		N'SELECT object_id, index_id, page_type_desc FROM sys.dm_db_page_info(@DBID, @FileID, @PageID, ''DETAILED'')',
		N'@DBID INT,@FileID INT,@PageID INT', @DBID, @FileID, @PageID;

	SELECT @ObjectID = object_id, @IndexID = index_id, @PageTypeDesc = page_type_desc FROM @PageInfo;

	SET @SQL = 'USE ' + QUOTENAME(@DBName) + '
	SELECT	@WaitResource AS wait_resource,
			DB_NAME() AS database_name,
			s.name AS schema_name,
			o.name AS object_name,
			i.name AS index_name,
			@PageTypeDesc AS page_type
	FROM (SELECT @ObjectID AS object_id, @IndexID AS index_id) AS t
	LEFT JOIN sys.objects o ON t.object_id = o.object_id
	LEFT JOIN sys.indexes i ON t.object_id = i.object_id AND t.index_id = i.index_id
	LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id;

	SELECT	file_id,
			name,
			physical_name
	FROM sys.database_files
	WHERE file_id = @FileID';

	EXEC sp_executesql @SQL,
		N'@ObjectID INT,@IndexID INT,@FileID INT,@WaitResource NVARCHAR(256),@PageTypeDesc NVARCHAR(60)',
		@ObjectID, @IndexID, @FileID, @WaitResource, @PageTypeDesc;
	RETURN;
END

SELECT @WaitResource AS wait_resource, 'Wait resource type is not yet supported for decoding' AS info;
