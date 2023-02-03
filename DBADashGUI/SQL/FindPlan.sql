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
	Run this query on the SQL instance ({instance_name}) to collect the query plan for this query

	Query plan is collected from the plan cache and from query store using a combination of 
	plan handle, sql handle, sql hash and plan hash.

	Note: DBA Dash plan collection can be configured using the service configuration tool.

----------------------------------------------------------
*/
USE [{database_name}]

DECLARE @plan_handle BINARY(64)
DECLARE @sql_handle BINARY(64)
DECLARE @query_hash BINARY(8)
DECLARE @query_plan_hash BINARY(8)
DECLARE @statement_start_offset INT
DECLARE @statement_end_offset INT
DECLARE @SQL NVARCHAR(MAX)

SELECT	@plan_handle ='{plan_handle}',
		@sql_handle ='{sql_handle}',
		@query_hash ='{query_hash}',
		@query_plan_hash ='{query_plan_hash}',
		@statement_start_offset ='{statement_start_offset}',
		@statement_end_offset ='{statement_end_offset}'

DECLARE @ProductVersion NVARCHAR(128)
DECLARE @ProductMajorVersion INT
SET @ProductVersion = CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128))
SELECT @ProductMajorVersion = SUBSTRING(@ProductVersion, 1,CHARINDEX('.', @ProductVersion)-1)

/* Find query plan from cache using plan_handle and text from sql_handle */
SET @SQL = N'
SELECT	p.query_plan,
		(SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				N''--'' + NCHAR(13) + NCHAR(10) 
				+ t.text +
				NCHAR(13) + NCHAR(10) 
				+ N''--'' COLLATE Latin1_General_Bin2,
				NCHAR(31),N''?''),NCHAR(30),N''?''),NCHAR(29),N''?''),NCHAR(28),N''?''),NCHAR(27),N''?''),NCHAR(26),N''?''),NCHAR(25),N''?''),NCHAR(24),N''?''),NCHAR(23),N''?''),NCHAR(22),N''?''),
				NCHAR(21),N''?''),NCHAR(20),N''?''),NCHAR(19),N''?''),NCHAR(18),N''?''),NCHAR(17),N''?''),NCHAR(16),N''?''),NCHAR(15),N''?''),NCHAR(14),N''?''),NCHAR(12),N''?''),
				NCHAR(11),N''?''),NCHAR(8),N''?''),NCHAR(7),N''?''),NCHAR(6),N''?''),NCHAR(5),N''?''),NCHAR(4),N''?''),NCHAR(3),N''?''),NCHAR(2),N''?''),NCHAR(1),N''?''),
				NCHAR(0), N'''') AS [processing-instruction(query)] FOR XML PATH(''''),TYPE) as batch_text,
		(SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				N''--'' + NCHAR(13) + NCHAR(10) 
				+ SUBSTRING(t.text,ISNULL((NULLIF(@statement_start_offset,-1)/2)+1,0),ISNULL((NULLIF(NULLIF(@statement_end_offset,-1),0) - NULLIF(@statement_start_offset,-1))/2+1,2147483647))  +
				NCHAR(13) + NCHAR(10) 
				+ N''--'' COLLATE Latin1_General_Bin2,
				NCHAR(31),N''?''),NCHAR(30),N''?''),NCHAR(29),N''?''),NCHAR(28),N''?''),NCHAR(27),N''?''),NCHAR(26),N''?''),NCHAR(25),N''?''),NCHAR(24),N''?''),NCHAR(23),N''?''),NCHAR(22),N''?''),
				NCHAR(21),N''?''),NCHAR(20),N''?''),NCHAR(19),N''?''),NCHAR(18),N''?''),NCHAR(17),N''?''),NCHAR(16),N''?''),NCHAR(15),N''?''),NCHAR(14),N''?''),NCHAR(12),N''?''),
				NCHAR(11),N''?''),NCHAR(8),N''?''),NCHAR(7),N''?''),NCHAR(6),N''?''),NCHAR(5),N''?''),NCHAR(4),N''?''),NCHAR(3),N''?''),NCHAR(2),N''?''),NCHAR(1),N''?''),
				NCHAR(0), N'''') AS [processing-instruction(query)] FOR XML PATH(''''),TYPE) as statement_text,
		' + CASE WHEN @ProductMajorVersion<10 /* OBJECT_SCHEMA_NAME supported from SQL 2008 (10.0) onwards */
					THEN 'QUOTENAME(OBJECT_NAME(t.objectid))' 
					ELSE 'QUOTENAME(OBJECT_SCHEMA_NAME(t.objectid,DB_ID())) + ''.'' + QUOTENAME(OBJECT_NAME(t.objectid))' 
					END + ' AS object_name
FROM (
		SELECT	@sql_handle as sql_handle, 
				@plan_handle as plan_handle
	 ) h
OUTER APPLY sys.dm_exec_query_plan(h.plan_handle) p
OUTER APPLY sys.dm_exec_sql_text(h.sql_handle) t'

EXEC sp_executesql @SQL,N'@sql_handle BINARY(64),@plan_handle BINARY(64),@statement_start_offset INT,@statement_end_offset INT',@sql_handle,@plan_handle,@statement_start_offset,@statement_end_offset

/* Find query plan from query store using plan hash and query_hash. */
SET @SQL = N'
SELECT STUFF(CASE WHEN p.query_plan_hash = @query_plan_hash THEN '',1.PLAN HASH'' ELSE '''' END +
		CASE WHEN q.query_hash = @query_hash THEN '',2.QUERY HASH'' ELSE '''' END +
		CASE WHEN q.batch_sql_handle = @sql_handle THEN '',3.BATCH SQL HANDLE'' ELSE '''' END,1,1,'''') as MatchOn,
	CAST(p.query_plan AS XML) as query_plan,
	   (SELECT	REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
				N''--'' + NCHAR(13) + NCHAR(10) 
				+ qt.query_sql_text +
				NCHAR(13) + NCHAR(10) 
				+ N''--'' COLLATE Latin1_General_Bin2,
                NCHAR(31),N''?''),NCHAR(30),N''?''),NCHAR(29),N''?''),NCHAR(28),N''?''),NCHAR(27),N''?''),NCHAR(26),N''?''),NCHAR(25),N''?''),NCHAR(24),N''?''),NCHAR(23),N''?''),NCHAR(22),N''?''),
                NCHAR(21),N''?''),NCHAR(20),N''?''),NCHAR(19),N''?''),NCHAR(18),N''?''),NCHAR(17),N''?''),NCHAR(16),N''?''),NCHAR(15),N''?''),NCHAR(14),N''?''),NCHAR(12),N''?''),
                NCHAR(11),N''?''),NCHAR(8),N''?''),NCHAR(7),N''?''),NCHAR(6),N''?''),NCHAR(5),N''?''),NCHAR(4),N''?''),NCHAR(3),N''?''),NCHAR(2),N''?''),NCHAR(1),N''?''),
                NCHAR(0), N'''') AS [processing-instruction(query)] FOR XML PATH(''''),TYPE) as query_text,
	QUOTENAME(OBJECT_SCHEMA_NAME(q.object_id,DB_ID())) + ''.'' + QUOTENAME(OBJECT_NAME(q.object_id)) as object_name,
	p.plan_id' + (SELECT ',
	q.' + QUOTENAME(name) 
	FROM sys.all_columns
	WHERE object_id = OBJECT_ID('sys.query_store_query')
	FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)') +
	(SELECT ',' + '
	p.' + QUOTENAME(name) 
	FROM sys.all_columns
	WHERE object_id = OBJECT_ID('sys.query_store_plan')
	AND name NOT IN('plan_id','query_id','query_plan')
	FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)')
	+ '
FROM sys.query_store_plan p
JOIN sys.query_store_query q on q.query_id = p.query_id
JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
WHERE ( 
		p.query_plan_hash = @query_plan_hash
		OR q.query_hash = @query_hash
		OR q.batch_sql_handle = @sql_handle
)
ORDER BY	CASE WHEN p.query_plan_hash = @query_plan_hash THEN 50 ELSE 0 END +
				CASE WHEN q.query_hash = @query_hash THEN 20 ELSE 0 END +
				CASE WHEN q.batch_sql_handle = @sql_handle THEN 10 ELSE 0 END DESC,
			q.last_execution_time DESC,
			q.query_id,
			p.plan_id;

IF NOT EXISTS(
		SELECT * 
		FROM sys.databases 
		WHERE is_query_store_on=1
		AND database_id = DB_ID()
		)
BEGIN
	SELECT ''Warning: Query store is not enabled'' as Notice
END
'
IF OBJECT_ID('sys.query_store_query ') IS NOT NULL /* Check if we are on a version of SQL Server that supports query store */
BEGIN
	EXEC sp_executesql @SQL,N'@query_plan_hash BINARY(8),@query_hash BINARY(8), @sql_handle BINARY(64)',@query_plan_hash,@query_hash,@sql_handle
END
ELSE
BEGIN
	SELECT 'Warning: Query store is not supported on this version of SQL Server' as Notice
END