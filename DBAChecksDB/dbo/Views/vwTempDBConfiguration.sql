CREATE VIEW vwTempDBConfiguration
AS
SELECT i.InstanceID,
		i.Instance,
		SUM(CASE WHEN f.type=0 THEN 1 ELSE 0 END) NumberOfDataFiles,
		SUM(CASE WHEN f.type=1 THEN 1 ELSE 0 END) NumberOfLogFiles,
		CASE WHEN COUNT(DISTINCT CASE WHEN type= 0 THEN f.size ELSE NULL END) =1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsEvenlySized,
		CASE WHEN COUNT(DISTINCT CASE WHEN type= 0 THEN f.growth ELSE NULL END)=1 
					AND COUNT(DISTINCT CASE WHEN type= 0 THEN f.is_percent_growth ELSE NULL END)=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsEvenGrowth,
		SUM(CASE WHEN f.type=0 THEN f.size ELSE NULL END)/128 AS TotalSizeMB,
		SUM(CASE WHEN f.type=1 THEN f.size ELSE NULL END)/128 AS LogMB,
		MAX(CASE WHEN f.type=0 THEN f.size ELSE NULL END)/128 AS FileSizeMB
FROM dbo.Instances i 
JOIN dbo.Databases d ON d.InstanceID = i.InstanceID
JOIN dbo.DBFiles f ON d.DatabaseID = f.DatabaseID
WHERE d.name = 'tempdb'
AND i.IsActive=1
AND d.IsActive=1
GROUP BY i.Instance,i.InstanceID