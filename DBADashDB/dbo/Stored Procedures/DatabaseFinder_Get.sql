CREATE PROC dbo.DatabaseFinder_Get(
	@InstanceIDs IDs READONLY,
	@SearchString NVARCHAR(MAX)='%',
	@Top INT=1000,
	@ExcludeSystemDatabases BIT=1
)
AS
SET @SearchString = ISNULL(NULLIF(@SearchString,''),'%')

SELECT TOP(@Top) I.InstanceID,
	D.DatabaseID,
	I.InstanceDisplayName, 
	I.InstanceGroupName,
	D.name AS [Database],
	'Performance' AS [Performance],
	'Object Execution' AS [Object Execution],
	'Slow Queries' AS [Slow Queries],
	'Files' AS [Files],
	'Snapshot Summary' AS [Snapshot Summary],
	'DB Space' AS [DB Space],
	'DB Configuration' AS [DB Configuration],
	'DB Options' AS [DB Options],
	'QS' AS [QS],
	'Top Queries (Query Store)' AS [Top Queries (Query Store)],
	'Forced Plans (Query Store)' AS [Forced Plans (Query Store)]
FROM dbo.Databases D
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
WHERE D.name LIKE @SearchString
AND D.IsActive=1
AND I.IsActive=1
AND (D.database_id > 4 OR @ExcludeSystemDatabases=0) 
AND EXISTS(	SELECT 1 
			FROM @InstanceIDs T 
			WHERE T.ID = I.InstanceID
			)
ORDER BY I.InstanceDisplayName, D.name