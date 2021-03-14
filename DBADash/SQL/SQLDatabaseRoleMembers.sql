SET NOCOUNT ON

DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #DBRoleMembers
(
	database_id INT NOT NULL,
    [role_principal_id] INT NOT NULL,
	[member_principal_id] INT NOT NULL
);



DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
SELECT name
FROM sys.databases
WHERE state  = 0
AND HAS_DBACCESS(name)=1

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN

	SET @SQL =  N'USE ' + QUOTENAME(@DBName)  + ' 
SELECT DB_ID(), 
	   	                   role_principal_id,
						   member_principal_id
FROM sys.database_role_members'

	IF HAS_DBACCESS(@DBName)=1
	BEGIN
		INSERT INTO #DBRoleMembers
		(
			database_id,
			role_principal_id,
			member_principal_id
		)
		EXEC  (	@SQL )
	END

	FETCH NEXT FROM DBs INTO @DBName
END
CLOSE DBs
DEALLOCATE DBs

SELECT database_id,
       role_principal_id,
       member_principal_id
FROM #DBRoleMembers

DROP TABLE #DBRoleMembers