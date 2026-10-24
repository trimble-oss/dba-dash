CREATE PROC Alert.DriveSpaceAlert_Upd
AS
/* 
	Get instances that fail the DriveSpace alert rule & update the active alerts
*/
DECLARE @Type VARCHAR(50)='DriveSpace';

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
	IsThresholdPercentage BIT NOT NULL,
	DriveLetter CHAR(1) COLLATE DATABASE_DEFAULT NULL,
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
	IsThresholdPercentage,
	DriveLetter,
	AlertKey,
	RuleID,
	GroupID
)
SELECT I.InstanceID,
	R.Priority,	
	R.Threshold,
	ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.UseCriticalStatus') AS BIT),1) AS UseCriticalStatus,
	ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.IsThresholdPercentage') AS BIT),1) AS IsThresholdPercentage,
	TRY_CAST(JSON_VALUE(R.Details,'$.DriveLetter') AS CHAR(1)) AS DriveLetter,
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
/* De dupe so we have a single row per drive/instance */
WITH DeDupe AS (
	SELECT T.InstanceID,
		T.Priority,
		T.AlertKey,
		CONCAT('Drive ',DS.Name,' is low on disk space. ',FORMAT(DS.FreeGB,'N1'),'GB free of ',FORMAT(DS.TotalGB,'N1'),' (',FORMAT(DS.PctFreeSpace,'P1'),')') AS Message,
		T.RuleID,
		T.GroupID,
		ROW_NUMBER() OVER (PARTITION BY T.InstanceID,T.GroupID,DS.DriveID ORDER BY T.Priority,T.RuleID) AS rnum
	FROM #EffectiveThresholds T
	JOIN dbo.DriveStatus DS ON T.InstanceID = DS.InstanceID
	WHERE (DS.Status = 1 OR T.UseCriticalStatus=0)
	AND (DS.PctFreeSpace <= (T.Threshold/100.0) OR T.Threshold IS NULL OR T.IsThresholdPercentage=0)
	AND (DS.FreeGB <= T.Threshold/1024.0 OR T.IsThresholdPercentage=1 OR T.Threshold IS NULL)
	AND (LEFT(DS.Name, 1) = T.DriveLetter or T.DriveLetter IS NULL)
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
