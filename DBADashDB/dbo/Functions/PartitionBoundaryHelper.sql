CREATE FUNCTION PartitionBoundaryHelper(@PartitionFunction SYSNAME,@TableName SYSNAME)
RETURNS TABLE
AS
RETURN
SELECT CAST(lbprv.value AS datetime) lb,CAST(ubprv.value AS datetime) ub,p.partition_number,p.rows
FROM sys.partitions p
LEFT JOIN sys.partition_range_values lbprv 
	INNER JOIN sys.partition_functions AS lbpf ON  lbpf.function_id = lbprv.function_id 
									 AND lbpf.name = @PartitionFunction
									ON p.partition_number = lbprv.boundary_id+1
LEFT JOIN sys.partition_range_values ubprv 
	INNER JOIN sys.partition_functions AS ubpf ON  ubpf.function_id = ubprv.function_id 
								 AND ubpf.name = @PartitionFunction
									ON p.partition_number = ubprv.boundary_id
where p.object_id = object_id(@TableName)
AND p.index_id = 1