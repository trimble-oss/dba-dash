CREATE PROC dbo.FileGroup_Get(@DatabaseID INT)
AS
SELECT data_space_id,ISNULL(filegroup_name, 'data space id:' + CAST(data_space_id AS SYSNAME)) AS FileGroup
FROM dbo.DBFiles F 
WHERE DatabaseID=@DatabaseID
AND IsActive=1
GROUP BY F.data_space_id,F.filegroup_name
ORDER BY filegroup_name,F.data_space_id