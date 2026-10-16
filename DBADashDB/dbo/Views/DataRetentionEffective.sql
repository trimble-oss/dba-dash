/*
	The retention each table is purged by: its own row in dbo.DataRetention, or for a table in dbo.DataRetentionLink,
	the retention of the table it follows.  A linked table whose source has no retention row has none either, so it is
	kept for as long as its source is.
*/
CREATE VIEW dbo.DataRetentionEffective
AS
SELECT	DR.SchemaName,
		DR.TableName,
		DR.RetentionDays,
		CAST(NULL AS SYSNAME) AS SourceSchemaName,
		CAST(NULL AS SYSNAME) AS SourceTableName
FROM dbo.DataRetention DR
WHERE NOT EXISTS(	SELECT 1
					FROM dbo.DataRetentionLink L
					WHERE L.SchemaName = DR.SchemaName
					AND L.TableName = DR.TableName
					)
UNION ALL
SELECT	L.SchemaName,
		L.TableName,
		S.RetentionDays,
		L.SourceSchemaName,
		L.SourceTableName
FROM dbo.DataRetentionLink L
JOIN dbo.DataRetention S ON S.SchemaName = L.SourceSchemaName AND S.TableName = L.SourceTableName
