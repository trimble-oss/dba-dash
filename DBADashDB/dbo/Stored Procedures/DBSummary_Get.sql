CREATE PROC [dbo].[DBSummary_Get](@InstanceIDs VARCHAR(MAX)=NULL)
AS
SELECT I.Instance, 
	SUM(CASE WHEN page_verify_option<>2 THEN 1 ELSE 0 END) as [Page Verify Not Optimal],
	SUM(CASE WHEN D.is_auto_close_on =1 THEN 1 ELSE 0 END) as [Auto Close],
	SUM(CASE WHEN D.is_auto_shrink_on =1 THEN 1 ELSE 0 END) as [Auto Shrink],
	SUM(CASE WHEN D.is_auto_create_stats_on=0 THEN 1 ELSE 0 END) as [Auto Create Stats Disabled],
	SUM(CASE WHEN D.is_auto_update_stats_on=0 THEN 1 ELSE 0 END) as [Auto Update Stats Disabled],
	SUM(CASE WHEN I.EditionID=1674378470 AND D.compatibility_level<150 THEN 1 WHEN D.compatibility_level < TRY_CAST(I.ProductMajorVersion AS INT)*10 THEN 1 ELSE 0 END) AS [Old Compat Level],
	SUM(CASE WHEN D.is_trustworthy_on=1 AND D.name<>'msdb' THEN 1 ELSE 0 END) AS [Trustworthy],
	SUM(CASE WHEN D.state IN(2,3) THEN 1 ELSE 0 END) AS [In Recovery],
	SUM(CASE WHEN D.state=4 THEN 1 ELSE 0 END) AS Suspect,
	SUM(CASE WHEN D.state=5 THEN 1 ELSE 0 END) AS Emergency,
	SUM(CASE WHEN D.state IN(6,10) THEN 1 ELSE 0 END) AS Offline,
	SUM(CASE WHEN D.database_id >4 THEN 1 ELSE 0 END) AS [User Database Count]	
FROM dbo.Instances I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
WHERE I.IsActive=1
AND D.IsActive=1
AND (EXISTS(SELECT 1 FROM STRING_SPLIT(@InstanceIDs,',') ss WHERE ss.value = I.InstanceID)
		OR @InstanceIDs IS NULL)
GROUP BY I.Instance