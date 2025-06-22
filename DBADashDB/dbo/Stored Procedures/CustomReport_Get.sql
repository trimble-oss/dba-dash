CREATE PROC dbo.CustomReport_Get
AS
DECLARE @CanEditReport BIT = CASE WHEN IS_ROLEMEMBER ('db_owner')=1 OR IS_ROLEMEMBER ('db_ddladmin')=1 THEN 1 ELSE 0 END;
SELECT	p.name AS ProcedureName,
		s.name AS SchemaName,
		QUOTENAME(s.name) + '.' + QUOTENAME(p.name) AS QualifiedName,
		(SELECT par.name as [@ParamName],
				UPPER(T.name) As [@ParamType]
		FROM sys.parameters par
		JOIN sys.types T ON par.user_type_id = T.user_type_id
		WHERE p.object_id = par.object_id
		FOR XML PATH('Param'),ROOT('Params'),TYPE) Params,
		CR.MetaData,
		@CanEditReport AS CanEditReport
FROM sys.procedures p
JOIN sys.schemas s on p.schema_id = s.schema_id
LEFT JOIN dbo.CustomReport CR ON CR.SchemaName = s.name AND CR.ProcedureName = p.name AND CR.Type = 'CustomReport'
WHERE s.name = 'UserReport'
AND HAS_PERMS_BY_NAME(QUOTENAME(SCHEMA_NAME(p.schema_id)) + '.' + QUOTENAME(p.name),'OBJECT','EXECUTE')=1