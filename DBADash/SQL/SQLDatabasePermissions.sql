SET NOCOUNT ON

DECLARE @DBName SYSNAME
DECLARE @SQL NVARCHAR(MAX)
CREATE TABLE #permissions
(
	database_id INT NOT NULL,
    [class] TINYINT NOT NULL,
    [class_desc] NVARCHAR(60) NULL,
    [major_id] INT NOT NULL,
    [minor_id] INT NOT NULL,
    [grantee_principal_id] INT NOT NULL,
    [grantor_principal_id] INT NOT NULL,
    [type] CHAR(4) NOT NULL,
    [permission_name] NVARCHAR(128) NULL,
    [state] CHAR(1) NOT NULL,
    [state_desc] NVARCHAR(60) NULL,
    [schema_name] NVARCHAR(128) NULL,
    [object_name] NVARCHAR(128) NULL,
    [column_name] NVARCHAR(128) NULL
);



DECLARE DBs CURSOR FAST_FORWARD READ_ONLY LOCAL FOR
    SELECT name
    FROM sys.databases
    WHERE state  = 0
    AND HAS_DBACCESS(name)=1
    AND compatibility_level >=80

OPEN DBs
FETCH NEXT FROM DBs INTO @DBName

WHILE @@FETCH_STATUS = 0
BEGIN

	SET @SQL =  N'USE ' + QUOTENAME(@DBName)  + ' 
SELECT DB_ID() AS database_id,
		p.class,
       p.class_desc,
       p.major_id,
       p.minor_id,
       p.grantee_principal_id,
       p.grantor_principal_id,
       p.type,
       p.permission_name,
       p.state,
       p.state_desc,
	   COALESCE(os.name,xs.name) AS schema_name,
	   COALESCE(o.name,s.name,t.name,dbp.name,a.name,x.name,mt.name,sc.name,svc.name,rsb.name,rt.name,ftc.name,sym.name,ct.name,asym.name) COLLATE Latin1_General_BIN AS object_name,
	   c.name AS column_name
FROM sys.database_permissions p
LEFT JOIN sys.objects o ON p.major_id = o.object_id AND p.class=1
LEFT JOIN sys.schemas os ON o.schema_id = os.schema_id AND p.class=1
LEFT JOIN sys.columns c ON c.object_id = p.major_id AND c.column_id = p.minor_id AND p.class=1
LEFT JOIN sys.schemas s ON p.major_id = s.schema_id AND p.class = 3
LEFT JOIN sys.types t ON t.user_type_id = p.major_id AND p.class=6
LEFT JOIN sys.database_principals dbp ON p.major_id = dbp.principal_id AND p.class=4
LEFT JOIN sys.assemblies a ON p.major_id = a.assembly_id AND p.class=5
LEFT JOIN sys.xml_schema_collections x ON p.major_id = x.xml_collection_id AND p.class=10
LEFT JOIN sys.schemas xs ON x.schema_id = xs.schema_id AND p.class=10
LEFT JOIN sys.service_message_types mt ON p.major_id = mt.message_type_id AND p.class=15
LEFT JOIN sys.service_contracts sc ON p.major_id = sc.service_contract_id AND p.class=16
LEFT JOIN sys.services svc ON p.major_id = svc.service_id AND p.class = 17
LEFT JOIN sys.remote_service_bindings rsb ON p.major_id = rsb.remote_service_binding_id AND p.class=18
LEFT JOIN sys.routes rt ON p.major_id = rt.route_id AND p.class=19
LEFT JOIN sys.fulltext_catalogs ftc ON p.major_id = ftc.fulltext_catalog_id AND p.class=23
LEFT JOIN sys.symmetric_keys sym ON p.major_id = sym.symmetric_key_id AND p.class=24
LEFT JOIN sys.certificates ct ON p.major_id = ct.certificate_id AND p.class=25
LEFT JOIN sys.asymmetric_keys asym ON p.major_id = asym.asymmetric_key_id AND p.class=26
WHERE NOT(p.major_id<0 AND p.grantee_principal_id=0 AND p.type = ''SL'') --Ignore Select on system objects to public
AND NOT(p.major_id<0 AND DB_ID()=1 AND p.type=''EX'') --ignore execute on system objects in master db'

	IF HAS_DBACCESS(@DBName)=1
	BEGIN
		INSERT INTO #permissions
		EXEC  (	@SQL )
	END

	FETCH NEXT FROM DBs INTO @DBName
END
CLOSE DBs
DEALLOCATE DBs

SELECT database_id,
       class,
       class_desc,
       major_id,
       minor_id,
       grantee_principal_id,
       grantor_principal_id,
       type,
       permission_name,
       state,
       state_desc,
       schema_name,
       object_name,
       column_name 
FROM #permissions

DROP TABLE #permissions