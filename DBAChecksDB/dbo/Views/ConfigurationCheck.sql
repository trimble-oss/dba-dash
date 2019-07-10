CREATE VIEW ConfigurationCheck
AS
SELECT U.InstanceID,
	   U.Instance,
       U.name,
       U.chk
FROM
(
    SELECT I.Instance,
		   D.InstanceID,
           D.name,
           D.is_auto_close_on AutoClose,
           D.is_auto_shrink_on AutoShrink,
           CASE
               WHEN D.page_verify_option = 2 THEN
                   CAST(0 AS BIT)
               ELSE
                   CAST(1 AS BIT)
           END AS PageVerify,
		   ~D.is_auto_create_stats_on AutoCreateStats,
		   ~D.is_auto_update_stats_on AutoUpdateStats,
		   D.is_auto_update_stats_async_on AutoUpdateStatsAsync,
		   D.is_date_correlation_on DateCorrelation,
		   D.is_encrypted IsEncrypted,
		   ~D.IsOwnerSA DBOwner
    FROM dbo.Databases D
	JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
    WHERE D.IsActive = 1
    AND   D.state = 0
	AND D.is_in_standby=0
	AND I.IsActive=1
) T
    UNPIVOT
    (
        chkValue
        FOR chk IN (AutoClose, AutoShrink, PageVerify,AutoCreateStats,AutoUpdateStats,AutoUpdateStatsAsync,DateCorrelation,IsEncrypted,DBOwner)
    ) U
WHERE U.chkValue = 1
UNION ALL
SELECT U.InstanceID,
		U.Instance,
       U.name,
       U.chk
FROM (
	SELECT I.Instance,
		I.InstanceID,
		D.Name + ' - ' + ISNULL(F.filegroup_name,F.data_space_id) AS Name,
		MAX(CASE WHEN F.is_percent_growth=1 AND F.size>131072 THEN 1 ELSE 0 END) PercentGrowth,
		CASE WHEN COUNT(DISTINCT growth)>1 OR COUNT(DISTINCT F.is_percent_growth)>1 THEN 1 ELSE 0 END AS UnevenGrowth,
		MAX(CASE WHEN growth=128 AND F.size>131072 THEN 1 ELSE 0 END) AS SmallGrowth,
		MIN(CASE WHEN F.max_size NOT IN(268435456,-1) AND F.type IN(0,1) AND F.growth>0 THEN 1 ELSE 0 END) AS MaxSizeSet
	FROM dbo.DBFiles F 
	JOIN dbo.Databases D ON D.DatabaseID = F.DatabaseID
	JOIN dbo.Instances I ON I.InstanceID = D.InstanceID
	WHERE I.IsActive=1
	AND D.IsActive=1
	AND F.IsActive=1
	AND D.is_in_standby=0
	AND D.state=0
	GROUP BY I.InstanceID,F.DatabaseID,F.data_space_id,F.filegroup_name,D.name,I.Instance
) T
UNPIVOT(chkValue FOR chk IN(PercentGrowth,UnevenGrowth,SmallGrowth,MaxSizeSet)) U 
WHERE U.chkValue=1