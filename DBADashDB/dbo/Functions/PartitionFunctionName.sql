/* 
	Created in post deployment. See: https://github.com/trimble-oss/dba-dash/issues/554
*/
IF OBJECT_ID('dbo.PartitionFunctionName') IS NULL
BEGIN
	EXEC sp_executesql N'CREATE FUNCTION dbo.PartitionFunctionName() RETURNS TABLE AS RETURN SELECT 1 a'
END
GO
ALTER FUNCTION dbo.PartitionFunctionName(@TableName SYSNAME)
RETURNS TABLE
AS
RETURN
SELECT TOP(1) pf.name AS PartitionFunctionName
FROM sys.indexes i
JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
JOIN sys.partition_functions pf  ON pf.function_id = ps.function_id
WHERE i.object_id = OBJECT_ID(@TableName);
GO