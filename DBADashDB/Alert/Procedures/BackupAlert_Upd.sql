CREATE PROC Alert.BackupAlert_Upd
AS
/*
	Get instances that fail the Backup alert rule & update the active alerts.
*/
SET NOCOUNT ON;

DECLARE @Type VARCHAR(50) = 'BackupAlert';

IF NOT EXISTS(
	SELECT 1
	FROM Alert.Rules
	WHERE Type = @Type
)
BEGIN
	PRINT CONCAT('No rules of type ', @Type, ' to process');
	RETURN;
END

PRINT CONCAT('Processing alerts of type ', @Type);

DECLARE @AlertDetails Alert.AlertDetails;

CREATE TABLE #Rules(
	InstanceID INT NOT NULL,
	DatabaseID INT NOT NULL,
	DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	ExcludeDatabaseName NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	Priority TINYINT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	BackupType VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	AlertMode VARCHAR(30) COLLATE DATABASE_DEFAULT NOT NULL,
	MinimumAlertStatus VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	ThresholdMins INT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
);

INSERT INTO #Rules(
	InstanceID,
	DatabaseID,
	DatabaseName,
	ExcludeDatabaseName,
	Priority,
	AlertKey,
	BackupType,
	AlertMode,
	MinimumAlertStatus,
	ThresholdMins,
	RuleID,
	GroupID
)
SELECT I.InstanceID,
	D.DatabaseID,
	Calc.DatabaseName,
	Calc.ExcludeDatabaseName,
	R.Priority,
	R.AlertKey,
	Calc.BackupType,
	Calc.AlertMode,
	Calc.MinimumAlertStatus,
	TRY_CAST(R.Threshold AS INT) AS ThresholdMins,
	R.RuleID,
	R.GroupID
FROM Alert.Rules R
OUTER APPLY(
	SELECT NULLIF(JSON_VALUE(R.Details, '$.DatabaseName'), '') AS DatabaseName,
		NULLIF(JSON_VALUE(R.Details, '$.ExcludeDatabaseName'), '') AS ExcludeDatabaseName,
		UPPER(NULLIF(JSON_VALUE(R.Details, '$.BackupType'), '')) AS BackupType,
		UPPER(ISNULL(NULLIF(JSON_VALUE(R.Details, '$.AlertMode'), ''), 'AgeSinceLastBackup')) AS AlertMode,
		UPPER(ISNULL(NULLIF(JSON_VALUE(R.Details, '$.MinimumAlertStatus'), ''), 'Critical')) AS MinimumAlertStatus
) Calc
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID, R.ApplyToInstanceID, R.AlertKey, R.ApplyToHidden) I
JOIN dbo.Databases D ON D.InstanceID = I.InstanceID
WHERE R.Type = @Type
	AND R.IsActive = 1
	AND D.IsActive = 1
	AND Calc.BackupType IN ('FULL','DIFF','LOG')
	AND NOT (Calc.BackupType = 'LOG' AND D.recovery_model = 3) /* Exclude SIMPLE recovery model for log backup alerts */
	AND D.name NOT IN('tempdb') /* Exclude tempdb */
	AND D.state NOT IN(1,6) /* Exclude RESTORING, OFFLINE */
	AND D.is_in_standby = 0 /* Exclude databases in standby */
	AND (
		Calc.DatabaseName IS NULL
		OR EXISTS(
			SELECT 1
			FROM STRING_SPLIT(REPLACE(REPLACE(Calc.DatabaseName, CHAR(13) + CHAR(10), CHAR(10)), CHAR(13), CHAR(10)), CHAR(10)) SS
			OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) T
			WHERE T.value <> ''
			AND (
				D.name LIKE T.value
				OR D.name = T.value
			)
		)
	)
	AND NOT EXISTS(
		SELECT 1
		FROM STRING_SPLIT(REPLACE(REPLACE(Calc.ExcludeDatabaseName, CHAR(13) + CHAR(10), CHAR(10)), CHAR(13), CHAR(10)), CHAR(10)) SS
		OUTER APPLY (SELECT LTRIM(RTRIM(SS.value)) AS value) T
		WHERE T.value <> ''
		AND (
			D.name LIKE T.value
			OR D.name = T.value
		)
	);

CREATE TABLE #Triggered(
	InstanceID INT NOT NULL,
	DatabaseID INT NOT NULL,
	DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
	BackupType VARCHAR(10) COLLATE DATABASE_DEFAULT NOT NULL,
	Priority INT NOT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	Message NVARCHAR(MAX) NOT NULL,
	RuleID INT NOT NULL,
	GroupID INT NOT NULL DEFAULT(0)
);

;WITH BackupStatusCalc AS (
	SELECT T.InstanceID,
		T.Priority,
		T.AlertKey,
		T.BackupType,
		T.AlertMode,
		T.MinimumAlertStatus,
		T.ThresholdMins,
		T.RuleID,
		T.GroupID,
		D.name,
		D.recovery_model,
		B.LastFull,
		B.LastDiff,
		B.LastLog,
		B.FullBackupStatus,
		B.DiffBackupStatus,
		B.LogBackupStatus,
		CASE T.BackupType WHEN 'FULL' THEN B.FullBackupStatus WHEN 'DIFF' THEN B.DiffBackupStatus WHEN 'LOG' THEN B.LogBackupStatus END AS CurrentStatus,
		CASE T.BackupType WHEN 'FULL' THEN DATEDIFF(mi, B.LastFull, GETUTCDATE()) WHEN 'DIFF' THEN DATEDIFF(mi, B.LastDiff, GETUTCDATE()) WHEN 'LOG' THEN DATEDIFF(mi, B.LastLog, GETUTCDATE()) END AS AgeMins,
		CASE T.BackupType WHEN 'FULL' THEN B.LastFull WHEN 'DIFF' THEN B.LastDiff WHEN 'LOG' THEN B.LastLog END AS LastBackupDate,
		D.DatabaseID,
		B.create_date_utc
	FROM #Rules T
	JOIN dbo.Databases D ON D.DatabaseID = T.DatabaseID
	JOIN dbo.BackupStatus B ON B.DatabaseID = D.DatabaseID
)
INSERT INTO #Triggered(
	InstanceID,
	DatabaseID,
	DatabaseName,
	BackupType,
	Priority,
	AlertKey,
	Message,
	RuleID,
	GroupID
)
SELECT BSC.InstanceID,
	BSC.DatabaseID,
	BSC.name,
	BSC.BackupType,
	BSC.Priority,
	BSC.AlertKey,
	CASE 
		WHEN BSC.AlertMode = 'STATUS' THEN
			CONCAT(
				CASE BSC.BackupType WHEN 'FULL' THEN 'Full' WHEN 'DIFF' THEN 'Diff' WHEN 'LOG' THEN 'Log' END,
				' backup for ', BSC.name, ' has status ',
				CASE BSC.CurrentStatus WHEN 1 THEN 'Critical' WHEN 2 THEN 'Warning' ELSE 'Unknown' END
			)
		ELSE
			CONCAT(
				CASE WHEN BSC.AgeMins IS NULL THEN 'No ' ELSE '' END,
				CASE BSC.BackupType WHEN 'FULL' THEN 'Full' WHEN 'DIFF' THEN 'Diff' WHEN 'LOG' THEN 'Log' END,
				' backup for ', 
				BSC.name,
				CASE WHEN BSC.AgeMins IS NULL THEN '' ELSE ' is ' + ISNULL(CONVERT(VARCHAR(20), BSC.AgeMins),'?') + ' minutes old' END
			)
	END,
	BSC.RuleID,
	BSC.GroupID
FROM BackupStatusCalc BSC
WHERE (
		BSC.AlertMode = 'AGESINCELASTBACKUP'
		AND (	
				BSC.AgeMins >= BSC.ThresholdMins 
				OR 
				(BSC.LastBackupDate IS NULL AND BSC.create_date_utc < DATEADD(mi,-BSC.ThresholdMins,GETUTCDATE()))
			)
	)
	OR (
		BSC.AlertMode = 'STATUS'
		AND (
			BSC.MinimumAlertStatus = 'CRITICAL' AND BSC.CurrentStatus = 1
			OR BSC.MinimumAlertStatus = 'WARNING' AND BSC.CurrentStatus IN (1,2)
		)
	);

INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	AlertKey,
	Message,
	RuleID,
	GroupID
)
SELECT Msg.InstanceID,
	Msg.Priority,
	Msg.AlertKey,
	CASE WHEN LEN(Msg.FullMessage) > 1000 THEN LEFT(Msg.FullMessage, 1000) + '...' ELSE Msg.FullMessage END AS Message,
	Msg.RuleID,
	Msg.GroupID
FROM (
	SELECT T.InstanceID,
		MIN(T.Priority) AS Priority,
		MIN(T.AlertKey) AS AlertKey,
		CONCAT(
			CASE T.BackupType
				WHEN 'FULL' THEN 'Full'
				WHEN 'DIFF' THEN 'Diff'
				WHEN 'LOG' THEN 'Log'
			END,
			' backup issue',
			CASE WHEN COUNT(DISTINCT T.DatabaseID) = 1 THEN '' ELSE 's' END,
			' for ',
			COUNT(DISTINCT T.DatabaseID),
			' database',
			CASE WHEN COUNT(DISTINCT T.DatabaseID) = 1 THEN '' ELSE 's' END,
			':',
			CHAR(10),
			STUFF((
				SELECT CHAR(10) + CONCAT('- ', X.Message)
				FROM (
					SELECT DISTINCT DT.Priority, DT.DatabaseName, DT.Message
					FROM #Triggered DT
					WHERE DT.InstanceID = T.InstanceID
						AND DT.GroupID = T.GroupID
						AND DT.BackupType = T.BackupType
				) X
				ORDER BY X.Priority, X.DatabaseName
				FOR XML PATH(''), TYPE
			).value('.', 'nvarchar(max)'), 1, 1, ''),
			''
		) AS FullMessage,
		MIN(T.RuleID) AS RuleID,
		T.GroupID
	FROM #Triggered T
	GROUP BY T.InstanceID, T.GroupID, T.BackupType
) Msg;

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type;
