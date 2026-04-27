SET NOCOUNT ON
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)

CREATE TABLE #ExtendedProperties (
    database_id   INT NOT NULL,
    name          SYSNAME NOT NULL,
    value         NVARCHAR(MAX) NULL
)

DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
    SELECT D.name
    FROM sys.databases D
    WHERE D.state = 0
    AND HAS_DBACCESS(D.name) = 1
    AND D.is_in_standby = 0
    AND D.database_id <> 2

OPEN DBs

WHILE 1=1
BEGIN
    FETCH NEXT FROM DBs INTO @DBName
    IF @@FETCH_STATUS <> 0
        BREAK
    IF HAS_DBACCESS(@DBName) = 1
    BEGIN
        SET @SQL = N'USE ' + QUOTENAME(@DBName) + N';
SELECT DB_ID() AS database_id,
    ep.name,
    CAST(ep.value AS NVARCHAR(MAX)) AS value
FROM sys.extended_properties ep
WHERE ep.class = 0'
        INSERT INTO #ExtendedProperties(database_id, name, value)
        EXEC sp_executesql @SQL
    END
END

CLOSE DBs
DEALLOCATE DBs

SELECT database_id, name, value
FROM #ExtendedProperties

DROP TABLE #ExtendedProperties
