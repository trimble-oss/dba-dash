CREATE PROC dbo.DBSummary_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ShowHidden BIT=1
)
AS
SELECT I.InstanceGroupName AS Instance, 
	SUM(CASE WHEN D.page_verify_option<>2 THEN 1 ELSE 0 END) as [Page Verify Not Optimal],
	SUM(CASE WHEN D.is_auto_close_on =1 THEN 1 ELSE 0 END) as [Auto Close],
	SUM(CASE WHEN D.is_auto_shrink_on =1 THEN 1 ELSE 0 END) as [Auto Shrink],
	SUM(CASE WHEN D.is_auto_create_stats_on=0 THEN 1 ELSE 0 END) as [Auto Create Stats Disabled],
	SUM(CASE WHEN D.is_auto_update_stats_on=0 THEN 1 ELSE 0 END) as [Auto Update Stats Disabled],
	SUM(CASE WHEN D.compatibility_level < I.MaxSupportedCompatibilityLevel THEN 1 ELSE 0 END) AS [Old Compat Level],
	SUM(CASE WHEN D.is_trustworthy_on=1 AND D.name<>'msdb' THEN 1 ELSE 0 END) AS [Trustworthy],
	SUM(CASE WHEN D.state=0 THEN 1 ELSE 0 END) AS [Online],
	SUM(CASE WHEN D.state=1 THEN 1 ELSE 0 END) AS Restoring,
	SUM(CASE WHEN D.state=2 THEN 1 ELSE 0 END) AS Recovering,
	SUM(CASE WHEN D.state=3 THEN 1 ELSE 0 END) AS [Recovery Pending],
	SUM(CASE WHEN D.state=4 THEN 1 ELSE 0 END) AS Suspect,
	SUM(CASE WHEN D.state=5 THEN 1 ELSE 0 END) AS Emergency,
	SUM(CASE WHEN D.state IN(6,10) THEN 1 ELSE 0 END) AS Offline,
	SUM(CASE WHEN D.state =7  THEN 1 ELSE 0 END) AS Copying,
	SUM(CASE WHEN D.is_in_standby =1 THEN 1 ELSE 0 END) AS [Standby],
	SUM(CASE WHEN D.database_id >4 THEN 1 ELSE 0 END) AS [User Database Count],
	MAX(D.VLFCount) AS [Max VLF Count],
	SUM(CASE WHEN D.target_recovery_time_in_seconds IS NULL THEN NULL WHEN D.target_recovery_time_in_seconds = 0 AND D.database_id > 4 THEN 1 ELSE 0 END) AS [Not Using Indirect Checkpoints],
	SUM(CASE WHEN D.target_recovery_time_in_seconds IS NULL OR D.target_recovery_time_in_seconds IN(0,60) THEN 0 ELSE 1 END) AS [None-Default Target Recovery Time],
	MAX(I.MaxSupportedCompatibilityLevel) AS [Max Supported Compatibility Level],
	SUM(CASE WHEN D.database_id >4  AND D.is_read_committed_snapshot_on = 1 THEN 1 ELSE 0 END) AS [RCSI Count]
FROM dbo.InstanceInfo I 
JOIN dbo.Databases D ON I.InstanceID = D.InstanceID
WHERE I.IsActive=1
AND D.IsActive=1
AND (
	EXISTS	(
			SELECT 1 
			FROM STRING_SPLIT(@InstanceIDs,',') ss 
			WHERE ss.value = I.InstanceID
			)
		OR @InstanceIDs IS NULL
	)
AND (I.ShowInSummary=1 OR @ShowHidden=1)
GROUP BY I.InstanceGroupName