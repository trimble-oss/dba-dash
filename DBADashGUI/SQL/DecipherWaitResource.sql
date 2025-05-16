/*
----------------------------------------------------------
	 ____   ____     _      ____               _          
	|  _ \ | __ )   / \    |  _ \   __ _  ___ | |__       
	| | | ||  _ \  / _ \   | | | | / _` |/ __|| '_ \      
	| |_| || |_) |/ ___ \  | |_| || (_| |\__ \| | | |     
	|____/ |____//_/   \_\ |____/  \__,_||___/|_| |_|     
                                                        
	SQL Server Monitoring by David Wiseman			     
	Copyright 2022 Trimble, Inc.                          
	https://dbadash.com                                   
                                                              
	**Instructions**
	Run this query on the SQL instance ({instance_name}) to decipher the wait resource.

	!!WARNING!!: This script uses the undocumented DBCC PAGE command.

	Returns database_name,schema_name,object_name,index_name & page_type depending on the 
	type of wait resource string.  Also returns output from DBCC PAGE command where applicable.

----------------------------------------------------------
*/
DECLARE @WaitResource NVARCHAR(256)

SELECT @WaitResource='{wait_resource}'

DECLARE @DBID INT
DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
DECLARE @HOBTID BIGINT
DECLARE @ObjectID INT
DECLARE @FileID INT
DECLARE @PageID INT
DECLARE @IndexID INT
DECLARE @idx1 INT
DECLARE @idx2 INT
DECLARE @m_type INT
DECLARE @PageType VARCHAR(100)
DECLARE @NormalizedWaitResource NVARCHAR(256)

IF @WaitResource='0:0:0'
BEGIN
	SELECT @WaitResource AS wait_resource,'https://www.sqlskills.com/blogs/paul/the-curious-case-of-what-is-the-wait-resource-000/' AS info
END
ELSE IF @WaitResource LIKE 'KEY: %'
BEGIN
	/*  
		Format KEY: DatabaseID:Hobt_id
		Parse to find database ID and HOBTID.
		Use sys.partitions to map HOBTID to table/index
	*/
	SELECT @DBID =  SUBSTRING(@WaitResource,6,CHARINDEX(':',@WaitResource,5)-6)
	SET @DBName = DB_NAME(@DBID)
	SELECT  @HOBTID = SUBSTRING(@WaitResource,
					CHARINDEX(':',@WaitResource,5)+1,
					CHARINDEX(' ',@WaitResource,6)-CHARINDEX(':',@WaitResource,5)-1
					)
	
	SET @SQL = 'USE ' + QUOTENAME(@DBName) + '
	SELECT	@WaitResource as wait_resource,
			DB_NAME() AS database_name,
			s.name AS schema_name,
			o.name as object_name, 
			i.name AS index_name
	FROM (SELECT @HOBTID AS hobt_id) AS t
	LEFT JOIN sys.partitions p ON t.hobt_id = p.hobt_id
	LEFT JOIN sys.objects o ON p.object_id = o.object_id 
	LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id
	LEFT JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id'

	EXEC sp_executesql @SQL, N'@HOBTID BIGINT,@WaitResource NVARCHAR(256)',@HOBTID,@WaitResource
END
ELSE IF @WaitResource LIKE 'RID: %' OR @WaitResource LIKE '[0-9]%:%:%' OR @WaitResource LIKE 'PAGE: %'
BEGIN
	DECLARE @DBCCPAGE TABLE(
		ParentObject NVARCHAR(255) NOT NULL,
		[Object] NVARCHAR(255) NOT NULL,
		Field NVARCHAR(255) NOT NULL,
		Value NVARCHAR(255) NOT NULL
	)

	SET @NormalizedWaitResource = @WaitResource
	IF @WaitResource LIKE 'RID: %'
	BEGIN
		/*  
			Format: RID: DatabaseID:FileID:PageID:Slot(row)
			Strip RID: prefix and slot
		*/
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource ,6,LEN(@NormalizedWaitResource))
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource ,1,LEN(@NormalizedWaitResource ) - CHARINDEX(':',REVERSE(@NormalizedWaitResource)))
	END
	ELSE IF @WaitResource LIKE 'PAGE: %'
	BEGIN
		/*  
			Format: PAGE: DatabaseID:FileID:PageID
			Strip PAGE: prefix.
		*/
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource ,7,LEN(@NormalizedWaitResource))
	END
	ELSE IF @WaitResource LIKE '[0-9]%:%:%(%)'
	BEGIN
		/*  
			Format: DatabaseID:FileID:PageID (Additial Info)
			Example: 6:3:749442168 (LATCH 0x000005E1CE68AE98: Class: BUFFER KP: 0 SH: 0 UP: 0 EX: 1 DT: 0 Sublatch: 0 HasWaiters: 1 Task: 0x000001F2D801C8C8 AnyReleasor: 1)
			Strip (Additional Info)
		*/
		SET @NormalizedWaitResource = SUBSTRING(@NormalizedWaitResource ,1,CHARINDEX('(',@NormalizedWaitResource)-1)
	END

	SELECT @idx1 = CHARINDEX(':',@NormalizedWaitResource)
	SELECT @idx2 = CHARINDEX(':',@NormalizedWaitResource,@idx1+1)
	
	SELECT	@DBID = SUBSTRING(@NormalizedWaitResource,1,@idx1-1),
			@FileID =SUBSTRING(@NormalizedWaitResource,@idx1+1,@idx2-@idx1-1),
			@PageID = SUBSTRING(@NormalizedWaitResource,@idx2+1,LEN(@NormalizedWaitResource))

	IF SERVERPROPERTY('EngineEdition') NOT IN(1,2,3,4)
	BEGIN
		SELECT	@DBID database_id,
				@FileID AS file_id,
				@PageID AS page_id,
				'DBCC PAGE not supported on this version of SQL Server' AS Notes

		RETURN
	END
				
	SET @DBName = DB_NAME(@DBID)
	
	SET @SQL = 'DBCC PAGE(' + CAST(@DBID as NVARCHAR(MAX)) + ',' 
						+ CAST(@FileID as NVARCHAR(MAX)) +',' 
						+ CAST(@PageID as NVARCHAR(MAX)) + ') WITH TABLERESULTS'

	INSERT INTO @DBCCPAGE(ParentObject,[Object],Field,Value)
	EXEC sp_executesql @SQL
	
	SELECT @IndexID = Value
	FROM @DBCCPAGE 
	WHERE Field = 'Metadata: IndexId'

	SELECT @ObjectID = Value 
	FROM @DBCCPAGE 
	WHERE Field = 'Metadata: ObjectId'

	SELECT @m_type = Value 
	FROM @DBCCPAGE 
	WHERE Field = 'm_type'

	/* 
		Get page type description from m_type
		See: https://www.sqlskills.com/blogs/paul/inside-the-storage-engine-anatomy-of-a-page/
	*/
	SELECT @PageType =  CAST(@m_type AS VARCHAR(100)) + ' - ' + 
		CASE @m_type
		WHEN 1 THEN 'Data'
		WHEN 2 THEN 'Index'
		WHEN 3 THEN 'Text mix'
		WHEN 4 THEN 'Text tree'
		WHEN 7 THEN 'Sort'
		WHEN 8 THEN 'GAM'
		WHEN 9 THEN 'SGAM'
		WHEN 10 THEN 'IAM'
		WHEN 11 THEN 'PFS'
		WHEN 13 THEN 'Boot'
		WHEN 15 THEN 'File header'
		WHEN 16 THEN 'Diff map'
		WHEN 17 THEN 'ML map'
		WHEN 18 THEN 'Deallocated DBCC CHECKDB'
		WHEN 19 THEN 'Index reorg temp page'
		WHEN 20 THEN 'Bulk Load pre-allocation'
		ELSE 'Other' END

	SET @SQL = 'USE ' + QUOTENAME(@DBName) + '
	SELECT	@WaitResource AS wait_resource,
			DB_NAME() AS database_name,
			s.name as schema_name, 
			o.name as object_name, 
			i.name AS index_name,
			@PageType AS page_type
	FROM (SELECT	@ObjectID as object_id,
					@IndexID AS index_id
			) AS t
	LEFT JOIN sys.objects o ON t.object_id = o.object_id
	LEFT JOIN sys.indexes i ON t.object_id = i.object_id AND t.index_id = i.index_id
	LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id
	
	SELECT	file_id,
			name,
			physical_name 
	FROM sys.database_files 
	WHERE file_id = @FileID'

	EXEC sp_executesql @SQL,N'@ObjectID INT,@IndexID INT, @FileID INT,@WaitResource NVARCHAR(256),@PageType VARCHAR(100)',@ObjectID,@IndexID, @FileID,@WaitResource,@PageType

	/* Return output from DBCC PAGE */
	SELECT ParentObject,
           Object,
           Field,
           Value 
	FROM @DBCCPAGE

END
ELSE IF @WaitResource LIKE 'OBJECT: %'
BEGIN
	/* 
		Format: OBJECT: DatabaseID:ObjectID
	*/
	SELECT @idx1 = CHARINDEX(':',@WaitResource,8)
	SELECT @idx2 = CHARINDEX(':',@WaitResource,@idx1+1)
	
	SELECT	@DBID = 	SUBSTRING(@WaitResource,9,@idx1-9),
			@ObjectID = SUBSTRING(@WaitResource,@idx1+1,@idx2-@idx1-1	)
			
	SET @DBName = DB_NAME(@DBID) 
	
	SET @SQL = 'USE ' + QUOTENAME(@DBName) + '
	SELECT  @WaitResource AS wait_resource,
			DB_NAME() as database_name,
			s.name AS schema_name,
			o.name AS object_name
	FROM (SELECT @ObjectID AS object_id) AS t
	LEFT JOIN sys.objects o ON t.object_id = o.object_id
	LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id'
	
	EXEC sp_executesql @SQL,N'@ObjectID INT,@IndexID INT,@WaitResource NVARCHAR(256)',@ObjectID,@IndexID,@WaitResource
END
ELSE
BEGIN
	SELECT @WaitResource as wait_resource,'Wait resource type is not yet supported for decoding' AS info
END
