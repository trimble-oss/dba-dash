CREATE PROC Report.Procs_Get(@DatabaseID INT)
AS
SELECT NULL AS Value,'{ALL}' as Name
UNION ALL
SELECT DISTINCT object_name,object_name
FROM dbo.Procs
WHERE DatabaseID=@DatabaseID