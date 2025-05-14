SELECT MAX(T.date_modified) AS date_modified
FROM (
	SELECT MAX(date_modified) AS date_modified 
	FROM msdb.dbo.sysjobs
	UNION ALL
	SELECT MAX(date_modified) AS date_modified 
	FROM msdb.dbo.sysschedules
) AS T