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
	AlertKey NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	RuleID INT NOT NULL
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
	RuleID
)
SELECT I.InstanceID,
	R.Priority,	
	R.Threshold,
	ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.UseCriticalStatus') AS BIT),1) AS UseCriticalStatus,
	ISNULL(TRY_CAST(JSON_VALUE(R.Details,'$.IsThresholdPercentage') AS BIT),1) AS IsThresholdPercentage,
	TRY_CAST(JSON_VALUE(R.Details,'$.DriveLetter') AS CHAR(1)) AS DriveLetter,
	R.AlertKey,
	R.RuleID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID,R.ApplyToInstanceID,R.AlertKey,R.ApplyToHidden) I
WHERE R.Type = @Type
AND R.IsActive=1;

DECLARE @AlertDetails Alert.AlertDetails;

CREATE TABLE #ExceededThreshold (
	InstanceID INT NOT NULL,
	Priority INT NOT NULL,
	AlertKey NVARCHAR(128) NOT NULL,
	Message NVARCHAR(MAX) NOT NULL,
	RuleID INT NOT NULL
);
/* De dupe so we have a single row per drive/instance */
WITH DeDupe AS (
	SELECT T.InstanceID,
		T.Priority,
		T.AlertKey,
		CONCAT('Drive ',DS.Name,' is low on disk space. ',FORMAT(DS.FreeGB,'N1'),'GB free of ',FORMAT(DS.TotalGB,'N1'),' (',FORMAT(DS.PctFreeSpace,'P1'),')') AS Message,
		T.RuleID,
		ROW_NUMBER() OVER (PARTITION BY T.InstanceID,DS.DriveID ORDER BY T.Priority,T.RuleID) AS rnum
	FROM #EffectiveThresholds T
	JOIN dbo.DriveStatus DS ON T.InstanceID = DS.InstanceID
	WHERE (DS.Status = 1 OR T.UseCriticalStatus=0)
	AND (DS.PctFreeSpace <= (T.Threshold/100.0) OR T.Threshold IS NULL OR T.IsThresholdPercentage=0)
	AND (DS.FreeGB >= T.Threshold*1024.0 OR T.IsThresholdPercentage=1 OR T.Threshold IS NULL)
)
INSERT INTO #ExceededThreshold(
	InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID
)
SELECT InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID 
FROM DeDupe 
WHERE rnum = 1

/* Aggregate so we have a single row per instance */
INSERT INTO @AlertDetails(
    InstanceID,
    Priority,
    Message,
	AlertKey,
	RuleID
)
SELECT	ET.InstanceID,
		MIN(ET.Priority) AS Priority,
		STUFF((SELECT '
' + ETcsv.Message FROM #ExceededThreshold ETcsv
		WHERE ETcsv.InstanceID = ET.InstanceID
		AND ETcsv.AlertKey = ET.AlertKey
		ORDER BY ETcsv.Priority
		FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,''),
		ET.AlertKey,
		MIN(RuleID) 
FROM #ExceededThreshold ET
GROUP BY ET.InstanceID,ET.AlertKey

EXEC Alert.ActiveAlerts_Upd @AlertDetails=@AlertDetails,@AlertType=@Type