CREATE PROC dbo.DataRetention_Get(@AllTables BIT=0)
AS
WITH A AS (
	SELECT 'dbo' as [SchemaName], DR.TableName,DR.RetentionDays, DATEDIFF(d,MIN(ub),GETUTCDATE()) ActualRetention,MIN(ub) DataFrom
	FROM dbo.DataRetention DR
	OUTER APPLY dbo.PartitionFunctionName(DR.TableName) PF
	OUTER APPLY[dbo].[PartitionBoundaryHelper](PF.PartitionFunctionName,DR.TableName) PB
	WHERE (PB.partition_number=1 OR PB.partition_number IS NULL)
	GROUP BY DR.TableName,DR.RetentionDays
)
, B AS (
	SELECT s.name as SchemaName, t.[name] AS TableName,
				SUM(ps.[used_page_count]) / 128.0 AS SizeMB
	FROM sys.dm_db_partition_stats AS ps
	INNER JOIN sys.indexes AS si ON ps.[object_id] = si.[object_id] 
				AND ps.[index_id] = si.[index_id]
	INNER JOIN sys.tables t ON t.object_id = si.object_id
	JOIN sys.schemas s on s.schema_id = t.schema_id
	GROUP BY s.[name], t.[name]
)
SELECT B.SchemaName,B.TableName,B.SizeMB,A.RetentionDays,A.ActualRetention,A.DataFrom 
FROM B
LEFT JOIN A ON A.TableName = B.TableName AND A.SchemaName = B.SchemaName
WHERE (A.TableName IS NOT NULL OR @AllTables=1)
ORDER BY B.SizeMB DESC