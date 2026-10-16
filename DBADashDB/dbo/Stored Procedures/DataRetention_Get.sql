CREATE PROC dbo.DataRetention_Get(
	@AllTables BIT=0
)
AS
WITH A AS (
	SELECT	DR.SchemaName, 
			DR.TableName,
			DR.RetentionDays, 
			/* The table a linked table's retention follows - it can't be set on the linked table itself */
			DR.SourceSchemaName + '.' + DR.SourceTableName AS RetentionLinkedTo,
			DATEDIFF(d,MIN(PH.UpperBound),GETUTCDATE()) ActualRetention,
			MIN(UpperBound) DataFrom
	FROM dbo.DataRetentionEffective DR
	LEFT JOIN dbo.PartitionHelper PH ON PH.SchemaName = DR.SchemaName AND PH.TableName = DR.TableName AND PH.partition_number=1
	GROUP BY DR.SchemaName,DR.TableName,DR.RetentionDays,DR.SourceSchemaName,DR.SourceTableName
)
, B AS (
	SELECT	s.name as SchemaName, 
			t.[name] AS TableName,
			SUM(ps.[used_page_count]) / 128.0 AS SizeMB
	FROM sys.dm_db_partition_stats AS ps
	INNER JOIN sys.indexes AS si ON ps.[object_id] = si.[object_id] 
				AND ps.[index_id] = si.[index_id]
	INNER JOIN sys.tables t ON t.object_id = si.object_id
	JOIN sys.schemas s on s.schema_id = t.schema_id
	GROUP BY s.[name], t.[name]
)
SELECT	B.SchemaName,
		B.TableName,
		B.SizeMB,
		A.RetentionDays,
		A.RetentionLinkedTo,
		A.ActualRetention,
		A.DataFrom 
FROM B
LEFT JOIN A ON A.TableName = B.TableName AND A.SchemaName = B.SchemaName
WHERE (A.TableName IS NOT NULL OR @AllTables=1)
ORDER BY B.SizeMB DESC