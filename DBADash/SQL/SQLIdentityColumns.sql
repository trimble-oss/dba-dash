SET NOCOUNT ON 
SET TRAN ISOLATION LEVEL READ UNCOMMITTED

DECLARE @DBName SYSNAME
DECLARE @IdentSQL NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)
SET @IdentSQL = N'SELECT DB_ID() AS database_id,
	IC.object_id,
	OBJECT_NAME(IC.object_id) AS object_name,
	IC.name AS column_name,
	CAST(IC.last_value AS BIGINT) AS last_value,
	RC.row_count,
	IC.system_type_id,
	IC.user_type_id,
	IC.max_length,
	CAST(IC.increment_value AS BIGINT) AS increment_value,
	CAST(IC.seed_value AS BIGINT) AS seed_value
FROM sys.identity_columns IC
OUTER APPLY(SELECT	CASE IC.max_length
						WHEN 1 THEN POWER(2.,IC.max_length*8) 
							ELSE POWER(2.,IC.max_length*8-1)-1 
					END AS max_ident,
					POWER(2.,IC.max_length*8) AS max_rows,
					CAST(IC.last_value AS BIGINT) as last_value_big
			) calc
OUTER APPLY(SELECT SUM(PS.row_count) row_count
			FROM sys.dm_db_partition_stats PS
			WHERE PS.object_id = IC.object_id
			AND PS.index_id < 2 -- HEAP/CLUSTERED
			) RC
WHERE (
	/* last_value is more than threshold percent of the max identity value */
	calc.last_value_big / calc.max_ident * 100 > @IdentityCollectionThreshold 
	/* Table row count is more than the threshold percent of the max number of rows (taking negative values into account)  
	   This is useful if identity was started with a negative number or if the identity was reseeded later with a negative number
	*/
	OR RC.row_count  / calc.max_rows * 100 > @IdentityCollectionThreshold 
	)
AND IC.max_length < 9 /* Exclude decimal types that would be larger than BIGINT and break calculations */
ORDER BY object_name;'

CREATE TABLE #ident(
    database_id SMALLINT NOT NULL,
    object_id INT NOT NULL,
    object_name NVARCHAR(128)  NULL,
    column_name NVARCHAR(128) NULL,
    last_value BIGINT NULL,
    row_count BIGINT NULL,
    system_type_id TINYINT NOT NULL,
    user_type_id INT NOT NULL,
    max_length SMALLINT NOT NULL,
    increment_value BIGINT NULL,
    seed_value BIGINT NULL
);


DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
			SELECT D.name
			FROM sys.databases D
			WHERE state = 0
			AND HAS_DBACCESS(D.name) = 1
			AND D.is_in_standby = 0
			AND D.database_id <> 2
			AND DATABASEPROPERTYEX(D.name, 'Updateability') = 'READ_WRITE';

OPEN DBs;

WHILE 1=1
BEGIN
	FETCH NEXT FROM DBs INTO @DBName
	IF @@FETCH_STATUS<>0
		BREAK
	IF HAS_DBACCESS(@DBName)=1
	BEGIN
		SET @SQL =  N'USE ' + QUOTENAME(@DBName)  + '
		' + @IdentSQL

		INSERT INTO #ident
		(
		    database_id,
		    object_id,
		    object_name,
		    column_name,
		    last_value,
		    row_count,
		    system_type_id,
		    user_type_id,
		    max_length,
		    increment_value,
		    seed_value
		)
		EXEC sp_executesql @SQL,N'@IdentityCollectionThreshold INT',@IdentityCollectionThreshold
	END
END
CLOSE DBs 
DEALLOCATE DBs

SELECT database_id,
       object_id,
       object_name,
       column_name,
       last_value,
       row_count,
       system_type_id,
       user_type_id,
       max_length,
       increment_value,
       seed_value 
FROM #ident

DROP TABLE #ident