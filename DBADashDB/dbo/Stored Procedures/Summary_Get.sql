CREATE PROC dbo.Summary_Get(
	/* Comma-separated list of InstanceIDs to include */
	@InstanceIDs VARCHAR(MAX)=NULL,
	/* Option to show 'hidden' instances */
	@ShowHidden BIT=1,
	/* Set to 1 to force a refresh of summary data. Otherwise summary data might be returned from cache depending on SummaryCacheDurationSec setting */
	@ForceRefresh BIT = 0,
	/* This option will force a refresh if cached summary is <= specified date. */
	@ForceRefreshDate DATETIME=NULL,
	/* Output param to indicate if cached summary data was updated */
	@WasRefreshed BIT=NULL OUT
)
AS
SET NOCOUNT ON
SET XACT_ABORT ON

/*******************************
* Summary Refresh             
* Summary data is cached to improve performance.  This can be useful on installations with a large number of instances/databases.
*******************************/
DECLARE @SummaryCacheDurationSec INT
IF @SummaryCacheDurationSec IS NULL
BEGIN
	SELECT @SummaryCacheDurationSec = CAST(SettingValue AS INT)
	FROM dbo.Settings
	WHERE SettingName = 'SummaryCacheDurationSec'

	SELECT @SummaryCacheDurationSec = ISNULL(@SummaryCacheDurationSec,0)
END
SELECT @ForceRefreshDate = CASE WHEN DATEADD(s,-@SummaryCacheDurationSec,GETUTCDATE()) > @ForceRefreshDate OR @ForceRefreshDate IS NULL THEN DATEADD(s,-@SummaryCacheDurationSec,GETUTCDATE()) ELSE @ForceRefreshDate END

IF ISNULL((SELECT TOP(1) RefreshDate 
	FROM dbo.Summary
	),'19000101')  <= @ForceRefreshDate OR @ForceRefresh=1
BEGIN
	EXEC dbo.Summary_Upd
	SET @WasRefreshed = 1
END
ELSE
BEGIN
	PRINT 'Skipping summary refresh'
	SET @WasRefreshed = 0
END

/*******************************/

CREATE TABLE #Instances(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO #Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO #Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

SELECT 	InstanceID,
		Instance,
		InstanceGroupName,
		LogShippingStatus,
		FullBackupStatus,
		LogBackupStatus,
		DiffBackupStatus,
		DriveStatus,
		FileFreeSpaceStatus,
		LogFreeSpaceStatus,
		JobStatus,
		AGStatus,
		DetectedCorruptionDateUtc,
		CorruptionStatus,
		CollectionErrorStatus,
		CollectionErrorCount,
		SnapshotAgeMin,
		SnapshotAgeMax,
		SnapshotAgeStatus,
		sqlserver_start_time_utc,
		UTCOffset,
		sqlserver_uptime,
		UptimeStatus,
		UptimeWarningThreshold,
		UptimeCriticalThreshold,
		UptimeConfiguredLevel,
		AdditionalUptime,
		host_uptime,
		host_start_time_utc,
		MemoryDumpCount,
		LastMemoryDump,
		LastMemoryDumpUTC,
		MemoryDumpStatus,
		LastGoodCheckDBStatus,
		LastGoodCheckDBCriticalCount,
		LastGoodCheckDBWarningCount,
		LastGoodCheckDBHealthyCount,
		LastGoodCheckDBNACount,
		OldestLastGoodCheckDBTime,
		DaysSinceLastGoodCheckDB,
		LastAlert,
		LastCritical,
		TotalAlerts,
		AlertStatus,
		AlertSnapshotDate,
		IsAgentRunning,
		IsAgentRunningStatus,
		CustomCheckStatus,
		MirroringStatus,
		ElasticPoolStorageStatus,
		PctMaxSizeStatus,
		QueryStoreStatus,
		DBMailStatus,
		DBMailStatusDescription,
		IdentityStatus,
		MaxIdentityPctUsed,
		MinIdentityEstimatedDays,
		ShowInSummary,
		IsHidden,
		DatabaseStateStatus,
		RefreshDate,
		CAST(0 AS BIT) AS IsAzure
FROM dbo.Summary S
WHERE EXISTS	(	
				SELECT 1 
				FROM #Instances T 
				WHERE T.InstanceID = S.InstanceID
				)
AND IsAzure=0
AND (S.ShowInSummary=1 OR @ShowHidden=1)
UNION ALL
SELECT 	NULL AS InstanceID,
		Instance,
		InstanceGroupName,
		3 AS LogShippingStatus,
		3 AS FullBackupStatus,
		3 AS LogBackupStatus,
		3 AS DiffBackupStatus,
		3 AS DriveStatus,
		ISNULL(MIN(NULLIF(S.FileFreeSpaceStatus,3)),3) AS FileFreeSpaceStatus,
		ISNULL(MIN(NULLIF(S.LogFreeSpaceStatus,3)),3) AS LogFreeSpaceStatus,
		3 AS JobStatus,
		3 AS AGStatus,
		NULL AS DetectedCorruptionDateUtc,
		3 AS CorruptionStatus,
		ISNULL(MIN(NULLIF(S.CollectionErrorStatus,3)),3) AS CollectionErrorStatus,
		ISNULL(SUM(S.CollectionErrorCount),0) AS CollectionErrorCount,
		MIN(S.SnapshotAgeMin) AS SnapshotAgeMin,
		MAX(S.SnapshotAgeMax) AS SnapshotAgeMax,
		ISNULL(MIN(NULLIF(S.SnapshotAgeStatus,3)),3) AS SnapshotAgeStatus,
		NULL AS sqlserver_start_time_utc,
		0 AS UTCOffset,
		NULL AS sqlserver_uptime,
		3 AS UptimeStatus,
		NULL AS UptimeWarningThreshold,
		NULL AS UptimeCriticalThreshold,
		NULL AS UptimeConfiguredLevel,
		NULL AS AdditionalUptime,
		NULL AS host_uptime,
		NULL AS	host_start_time_utc,
		NULL AS MemoryDumpCount,
		NULL AS LastMemoryDump,
		NULL AS LastMemoryDumpUTC,
		3 AS MemoryDumpStatus,
		3 AS  LastGoodCheckDBStatus,
		NULL AS LastGoodCheckDBCriticalCount,
		NULL AS LastGoodCheckDBWarningCount,
		NULL AS LastGoodCheckDBHealthyCount,
		NULL AS LastGoodCheckDBNACount,
		NULL AS OldestLastGoodCheckDBTime,
		NULL AS DaysSinceLastGoodCheckDB,
		NULL AS LastAlert,
		NULL AS LastCritical,
		NULL AS TotalAlerts,
		3 AlertStatus,
		NULL AS AlertSnapshotDate,
		NULL AS IsAgentRunning,
		3 AS IsAgentRunningStatus,
		ISNULL(MIN(NULLIF(S.CustomCheckStatus,3)),3) AS CustomCheckStatus,
		3 AS MirroringStatus,
		ISNULL(MIN(NULLIF(S.ElasticPoolStorageStatus,3)),3)  AS ElasticPoolStorageStatus,
		ISNULL(MIN(NULLIF(S.PctMaxSizeStatus,3)),3) AS PctMaxSizeStatus,
		ISNULL(MIN(NULLIF(S.QueryStoreStatus,3)),3) AS QueryStoreStatus,
		3 AS DBMailStatus,
		NULL AS DBMailStatusDescription,
		ISNULL(MIN(NULLIF(S.IdentityStatus,3)),3) IdentityStatus,
		MAX(S.MaxIdentityPctUsed) AS MaxIdentityPctUsed,
		MIN(S.MinIdentityEstimatedDays) AS MinIdentityEstimatedDays,
		CAST(MAX(CAST(S.ShowInSummary AS TINYINT)) AS BIT) AS ShowInSummary,
		~CAST(MAX(CAST(S.ShowInSummary AS TINYINT)) AS BIT) AS IsHidden,
		ISNULL(MIN(S.DatabaseStateStatus),4) AS DatabaseStateStatus,
		MIN(S.RefreshDate) AS RefreshDate,
		CAST(1 AS BIT) AS IsAzure
FROM dbo.Summary S
WHERE EXISTS	(	
				SELECT 1 
				FROM #Instances T 
				WHERE T.InstanceID = S.InstanceID
				)
AND IsAzure=1
AND (S.ShowInSummary=1 OR @ShowHidden=1)
GROUP BY Instance,InstanceGroupName

