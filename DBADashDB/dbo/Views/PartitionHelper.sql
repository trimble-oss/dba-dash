CREATE VIEW dbo.PartitionHelper
AS
SELECT t.object_id,
	   s.schema_id,
	   ps.data_space_id,
	   p.data_compression_desc,
	   s.name AS SchemaName,
	   t.name AS TableName,
       ps.name AS PartitionSchemeName,
       pf.name AS PartitionFunctionName,
	   TRY_CAST(LAG(prv.value) OVER(PARTITION BY t.object_id ORDER BY p.partition_number) AS DATETIME2) AS LowerBound,
	   TRY_CAST(prv.value AS DATETIME2) AS UpperBound,
	   p.rows,
	   p.partition_number
FROM sys.tables AS t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.indexes AS i ON t.object_id = i.object_id
INNER JOIN sys.partition_schemes AS ps ON i.data_space_id = ps.data_space_id
INNER JOIN sys.partition_functions AS pf ON ps.function_id = pf.function_id
INNER JOIN sys.partition_parameters pp ON pf.function_id = pp.function_id
INNER JOIN sys.partitions p ON p.object_id = i.object_id AND p.index_id = i.index_id
LEFT JOIN sys.partition_range_values prv ON prv.function_id = pf.function_id AND prv.boundary_id = p.partition_number
WHERE i.index_id < 2
AND ps.type = 'PS'
AND pp.system_type_id IN(40,42,43,58,61); /* datetime */
