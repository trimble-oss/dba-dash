CREATE PROC Alert.ElasticPoolSpaceAlert_Upd
AS
/*
	Get Azure elastic pools that fail the ElasticPoolSpace alert rule & update the active alerts.
	Elastic pool stats are collected from the master database so the alert is raised against the master DB instance.

	UseCriticalStatus uses the critical elastic pool storage threshold already configured (dbo.AzureDBElasticPoolStorageThresholds) which is based on allocated space.
	Threshold is a percentage (e.g. 90 = 90%) of the pool storage limit, applied to used or allocated space depending on SpaceMeasure.
	If both are specified, both need to be exceeded for the alert to trigger (same as DriveSpace).
*/
SET NOCOUNT ON
DECLARE @Type VARCHAR(50)='ElasticPoolSpace';

/* Check if we have any rules to process */
IF NOT EXISTS(
	SELECT 1
	FROM Alert.Rules
	WHERE Type = @Type
)
BEGIN
	PRINT CONCAT('No rules of type ',@Type,' to process')
	RETURN;
END
PRINT CONCAT('Processing alerts of type ',@Type)

CREATE TABLE #EffectiveThresholds(
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	Threshold DECIMAL(28,9) NULL,
	UseCriticalStatus BIT NOT NULL,
	UseAllocatedSpace BIT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
)

INSERT INTO #EffectiveThresholds
(
	InstanceID,
	Priority,
	Threshold,
	UseCriticalStatus,
	UseAllocatedSpace,
	AlertKey,
	RuleID,
	GroupID
)
SELECT I.InstanceID,
	R.Priority,
	R.Threshold,
	ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.UseCriticalStatus') AS BIT),1) AS UseCriticalStatus,
	CASE WHEN JSON_VALUE(R.Details,'$.SpaceMeasure') = 'Used' THEN 0 ELSE 1 END AS UseAllocatedSpace,
	R.AlertKey,
	R.RuleID,
	R.GroupID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
WHERE R.Type = @Type
AND R.IsActive=1;

DECLARE @AlertDetails Alert.AlertDetails;

CREATE TABLE #ExceededThreshold (
	InstanceID INT NOT NULL,
	Priority INT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	Message NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
);
/* De dupe so we have a single row per pool/instance */
WITH DeDupe AS (
	SELECT T.InstanceID,
		T.Priority,
		T.AlertKey,
		CONCAT('Elastic pool ',RS.elastic_pool_name,' is low on space. ',
				FORMAT(RS.avg_allocated_storage_percent/100.0,'P1'),' allocated, ',
				FORMAT(RS.avg_storage_percent/100.0,'P1'),' used of ',FORMAT(RS.elastic_pool_storage_limit_mb/1024.0,'N1'),'GB storage limit') AS Message,
		T.RuleID,
		T.GroupID,
		ROW_NUMBER() OVER (PARTITION BY T.InstanceID,T.GroupID,RS.PoolID ORDER BY T.Priority,T.RuleID) AS rnum
	FROM #EffectiveThresholds T
	JOIN dbo.AzureDBElasticPoolResourceStats_Current RS ON T.InstanceID = RS.InstanceID
	JOIN dbo.AzureDBElasticPoolStorageStatus SS ON RS.PoolID = SS.PoolID
	JOIN dbo.AzureDBElasticPool P ON RS.PoolID = P.PoolID
	CROSS APPLY(SELECT CASE WHEN T.UseAllocatedSpace=1 THEN RS.avg_allocated_storage_percent ELSE RS.avg_storage_percent END AS StoragePercent) Calc
	WHERE P.ValidTo IS NULL /* Exclude pools that no longer exist */
	AND NOT (T.UseCriticalStatus=0 AND T.Threshold IS NULL) /* Rule has nothing to evaluate */
	AND (SS.ElasticPoolStorageStatus = 1 OR T.UseCriticalStatus=0)
	AND (Calc.StoragePercent >= T.Threshold OR T.Threshold IS NULL)
)
INSERT INTO #ExceededThreshold(
	InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID,
	GroupID
)
SELECT InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID,
	GroupID
FROM DeDupe
WHERE rnum = 1;

/* Single row per instance.  Priority & RuleID come from the same (highest priority) row so rule notes & closure match the alert */
WITH Ranked AS (
	SELECT ET.InstanceID,
		ET.Priority,
		ET.AlertKey,
		ET.RuleID,
		ET.GroupID,
		ROW_NUMBER() OVER (PARTITION BY ET.InstanceID,ET.AlertKey,ET.GroupID ORDER BY ET.Priority,ET.RuleID) AS rnum
	FROM #ExceededThreshold ET
)
INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID,
	GroupID
)
SELECT	R.InstanceID,
		R.Priority,
		STUFF((SELECT '
' + ETcsv.Message FROM #ExceededThreshold ETcsv
		WHERE ETcsv.InstanceID = R.InstanceID
		AND ETcsv.AlertKey = R.AlertKey
		AND ETcsv.GroupID = R.GroupID
		ORDER BY ETcsv.Priority
		FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,''),
		R.AlertKey,
		R.RuleID,
		R.GroupID
FROM Ranked R
WHERE R.rnum = 1

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type
