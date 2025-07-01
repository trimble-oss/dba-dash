DECLARE @SQL NVARCHAR(MAX) 

DECLARE @SelectSQL NVARCHAR(MAX)
SET @SelectSQL = N'SELECT	DB_NAME() AS database_name,
			CASE WHEN DB_NAME()=''master'' THEN 1 ELSE 0 END AS from_master,
			S.name AS schema_name,
			O.name AS object_name,
			(SELECT P.name AS [@name],
					T.name AS [@type]
			FROM sys.parameters P
			JOIN sys.types T ON P.system_type_id = T.user_type_id
			WHERE P.object_id =  O.object_id
			FOR XML PATH(''parameter''),TYPE, ROOT(''parameters'')
			) parameters
	FROM sys.procedures O
	JOIN sys.schemas S ON O.schema_id = S.schema_id
	WHERE O.is_ms_shipped=0
	'

/* Get procs for current DB and procs from master that can be called from any database (name starting 'sp_') */
SET @SQL = N'
DECLARE @Procs TABLE(
	database_name SYSNAME,
	from_master BIT,
	schema_name SYSNAME,
	object_name SYSNAME,
	parameters XML
)
DECLARE @DB SYSNAME
SET @DB = DB_NAME()
WHILE 1=1
BEGIN
	INSERT INTO @Procs(
		database_name,
		from_master,
		schema_name,
		object_name,
		parameters
	)
	' + @SelectSQL + '
	AND (DB_NAME() COLLATE DATABASE_DEFAULT = @DB 
			OR (
				O.name COLLATE DATABASE_DEFAULT LIKE ''sp_%'' 
				AND S.name COLLATE DATABASE_DEFAULT = ''dbo''
				)
		)
	AND NOT EXISTS(	SELECT 1 
					FROM @Procs T 
					WHERE T.schema_name COLLATE DATABASE_DEFAULT = S.name
					AND T.object_name COLLATE DATABASE_DEFAULT = O.name
					)

	IF DB_NAME()=''master''
		BREAK
	USE [master]
END

SELECT	database_name,
		from_master,
		schema_name,
		object_name,
		parameters
FROM @Procs'

IF SERVERPROPERTY('EngineEdition')=5 /* Azure DB. Get procs for the current DB only */
BEGIN
	SET @SQL = @SelectSQL
END

EXEC sp_executesql @SQL