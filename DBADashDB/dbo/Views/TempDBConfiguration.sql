CREATE VIEW dbo.TempDBConfiguration
AS
SELECT i.InstanceID,
		i.Instance,
		i.InstanceDisplayName,
		SUM(CASE WHEN f.type=0 THEN 1 ELSE 0 END) NumberOfDataFiles,
	    calc.MinimumRecommendedFiles,
		CASE WHEN SUM(CASE WHEN f.type=0 THEN 1 ELSE 0 END) < calc.MinimumRecommendedFiles THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS InsufficientFiles,
		SUM(CASE WHEN f.type=1 THEN 1 ELSE 0 END) NumberOfLogFiles,
		CASE WHEN COUNT(DISTINCT CASE WHEN type= 0 THEN f.size ELSE NULL END) =1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsEvenlySized,
		CASE WHEN COUNT(DISTINCT CASE WHEN type= 0 THEN f.growth ELSE NULL END)=1 
					AND COUNT(DISTINCT CASE WHEN type= 0 THEN f.is_percent_growth ELSE NULL END)=1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsEvenGrowth,
		SUM(CASE WHEN f.type=0 THEN f.size ELSE NULL END)/128 AS TotalSizeMB,
		SUM(CASE WHEN f.type=1 THEN f.size ELSE NULL END)/128 AS LogMB,
		MAX(CASE WHEN f.type=0 THEN f.size ELSE NULL END)/128 AS FileSizeMB,
		MAX(CASE WHEN f.type=0 AND f.is_percent_growth=0 THEN f.growth/128 ELSE NULL END) AS MaxGrowthMB,
		MAX(CASE WHEN f.type=1 AND f.is_percent_growth=0 THEN f.growth/128 ELSE NULL END) AS MaxLogGrowthMB,
		MAX(CASE WHEN f.type=0 AND f.is_percent_growth=1 THEN f.growth ELSE NULL END) AS MaxGrowthPct,
		MAX(CASE WHEN f.type=1 AND f.is_percent_growth=1 THEN f.growth ELSE NULL END) AS MaxLogGrowthPct,
		STUFF((SELECT DISTINCT ', ' + LEFT(f2.physical_name,3) FROM dbo.DBFiles f2 WHERE f2.DatabaseID =d.DatabaseID AND f2.physical_name LIKE '_:\%' FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'') AS TempDBVolumes,
		ISNULL(i.cpu_core_count,i.cpu_count) AS cpu_core_count,
		CAST(ISNULL(T.T1117,0) AS BIT) AS T1117,
		CAST(ISNULL(T.T1118,0) AS BIT) AS T1118,
		CASE WHEN I.ProductMajorVersion<13 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsTraceFlagRequired,
		I.IsTempDBMetadataMemoryOptimized
FROM dbo.InstanceInfo i 
JOIN dbo.Databases d ON d.InstanceID = i.InstanceID
JOIN dbo.DBFiles f ON d.DatabaseID = f.DatabaseID
OUTER APPLY(SELECT CASE WHEN ISNULL(i.cpu_core_count,i.cpu_count) >8 THEN 8 ELSE COALESCE(i.cpu_core_count,i.cpu_count,8) END AS MinimumRecommendedFiles) calc
OUTER APPLY(SELECT	MAX(CASE WHEN TF.TraceFlag=1117 THEN 1 ELSE 0 END) AS T1117, 
					MAX(CASE WHEN TF.TraceFlag=1118 THEN 1 ELSE 0 END) T1118
			FROM dbo.TraceFlags TF 
			WHERE TF.InstanceID = i.InstanceID 
			AND TF.TraceFlag IN(1118,1117)
			) T
WHERE d.name = 'tempdb'
AND i.IsActive=1
AND d.IsActive=1
AND f.IsActive=1
GROUP BY	i.Instance,
			i.InstanceID,
			i.InstanceDisplayName,
			i.cpu_core_count,
			d.DatabaseID, 
			T.T1118,
			T.T1117,
			I.ProductMajorVersion,
			calc.MinimumRecommendedFiles,
			I.IsTempDBMetadataMemoryOptimized,
			I.cpu_count