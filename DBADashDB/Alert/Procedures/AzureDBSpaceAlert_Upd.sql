CREATE PROC Alert.AzureDBSpaceAlert_Upd
AS
/*
	Get Azure SQL databases that fail the AzureDBSpace alert rule & update the active alerts.
	Space is measured as a percentage of the database max size (data files only).

	UseCriticalStatus uses the critical "% Max Size" threshold already configured (dbo.DBFileThresholds) which is based on allocated space.
	Threshold is a percentage (e.g. 90 = 90%) applied to used or allocated space depending on SpaceMeasure.
	If both are specified, both need to be exceeded for the alert to trigger (same as DriveSpace).
*/
SET NOCOUNT ON
DECLARE @Type VARCHAR(50)='AzureDBSpace';

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
	CASE WHEN JSON_VALUE(R.Details,'$.SpaceMeasure') = 'Allocated' THEN 1 ELSE 0 END AS UseAllocatedSpace,
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
/* De dupe so we have a single row per database/instance */
WITH DeDupe AS (
	SELECT T.InstanceID,
		T.Priority,
		T.AlertKey,
		CONCAT('Database ',DB.DB,' is low on space. ',
				FORMAT(DB.UsedSpaceMB/1024.0,'N1'),'GB used of ',FORMAT(DB.MaxStorageSizeMB/1024.0,'N1'),'GB max size (',FORMAT(DB.UsedPctOfMaxSize,'P1'),'). ',
				FORMAT(DB.AllocatedSpaceMB/1024.0,'N1'),'GB allocated (',FORMAT(DB.AllocatedPctOfMaxSize,'P1'),')') AS Message,
		T.RuleID,
		T.GroupID,
		ROW_NUMBER() OVER (PARTITION BY T.InstanceID,T.GroupID,DB.DatabaseID ORDER BY T.Priority,T.RuleID) AS rnum
	FROM #EffectiveThresholds T
	JOIN dbo.AzureDBInfo DB ON T.InstanceID = DB.InstanceID
	CROSS APPLY(SELECT CASE WHEN T.UseAllocatedSpace=1 THEN DB.AllocatedPctOfMaxSize ELSE DB.UsedPctOfMaxSize END AS PctOfMaxSize) Calc
	WHERE DB.MaxStorageSizeMB IS NOT NULL
	AND DB.IsReadOnlyReplica=0
	AND DB.DB <> 'master'
	AND NOT (T.UseCriticalStatus=0 AND T.Threshold IS NULL) /* Rule has nothing to evaluate */
	AND (DB.PctMaxSizeStatus = 1 OR T.UseCriticalStatus=0)
	AND (Calc.PctOfMaxSize >= T.Threshold/100.0 OR T.Threshold IS NULL)
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
