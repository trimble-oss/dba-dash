CREATE PROC Alert.BackupChainRiskAlert_Upd
AS
SET NOCOUNT ON
DECLARE @Type VARCHAR(50) = 'BackupChainRisk'

IF NOT EXISTS(
    SELECT 1
    FROM Alert.Rules
    WHERE Type = @Type
)
BEGIN
    PRINT CONCAT('No rules of type ', @Type, ' to process')
    RETURN;
END
PRINT CONCAT('Processing alerts of type ', @Type)

CREATE TABLE #BackupChainApplicable(
    InstanceID INT NOT NULL,
    Priority TINYINT NOT NULL,
    DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
    ExcludedDatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
    MinimumDatabaseAgeMins INT NULL,
    ThresholdMins INT NOT NULL,
    AlertKeyTemplate NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
    RuleID INT NOT NULL
)

INSERT INTO #BackupChainApplicable(
    InstanceID,
    Priority,
    DatabaseName,
    ExcludedDatabaseName,
    MinimumDatabaseAgeMins,
    ThresholdMins,
    AlertKeyTemplate,
    RuleID
)
SELECT I.InstanceID,
       R.Priority,
       NULLIF(JSON_VALUE(R.Details, '$.DatabaseName'), '') AS DatabaseName,
       NULLIF(JSON_VALUE(R.Details, '$.ExcludedDatabaseName'), '') AS ExcludedDatabaseName,
       TRY_CAST(JSON_VALUE(R.Details, '$.MinimumDatabaseAgeMins') AS INT) AS MinimumDatabaseAgeMins,
       TRY_CAST(R.Threshold AS INT) AS ThresholdMins,
       R.AlertKey,
       R.RuleID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID, R.ApplyToInstanceID, R.AlertKey, R.ApplyToHidden) I
WHERE R.Type = @Type
AND R.IsActive = 1
AND TRY_CAST(R.Threshold AS INT) IS NOT NULL

DECLARE @AlertDetails Alert.AlertDetails

;WITH BackupData AS (
    SELECT BS.InstanceID,
           BS.DatabaseID,
           BS.name,
           BS.recovery_model_desc,
           BS.create_date_utc,
           BS.LastFull,
           BS.LastLog,
           A.Priority,
           A.ThresholdMins,
           A.AlertKeyTemplate,
           A.RuleID
    FROM #BackupChainApplicable A
    JOIN dbo.BackupStatus BS ON BS.InstanceID = A.InstanceID
    WHERE (BS.name LIKE A.DatabaseName OR A.DatabaseName IS NULL)
    AND NOT (BS.name LIKE A.ExcludedDatabaseName AND A.ExcludedDatabaseName IS NOT NULL)
    AND (A.MinimumDatabaseAgeMins IS NULL OR BS.create_date_utc <= DATEADD(MINUTE, -A.MinimumDatabaseAgeMins, GETUTCDATE()))
    AND BS.recovery_model IN (1,2)
    AND BS.FullBackupExcludedReason IS NULL
    AND BS.LogBackupExcludedReason IS NULL
), RiskData AS (
    SELECT BD.InstanceID,
           BD.DatabaseID,
           BD.name,
           BD.recovery_model_desc,
           BD.Priority,
           BD.ThresholdMins,
           BD.LastFull,
           BD.LastLog,
           BD.AlertKeyTemplate,
           BD.RuleID,
           DATEDIFF(MINUTE, BD.LastLog, GETUTCDATE()) AS LogBackupAgeMins,
           CASE
               WHEN BD.LastFull IS NULL THEN 'NoFullBackup'
               WHEN BD.LastLog IS NULL THEN 'NoLogBackup'
               WHEN BD.LastLog < BD.LastFull THEN 'LogChainNotStartedAfterFull'
               WHEN DATEDIFF(MINUTE, BD.LastLog, GETUTCDATE()) >= BD.ThresholdMins THEN 'LogBackupTooOld'
               ELSE NULL
           END AS RiskType
    FROM BackupData BD
)
INSERT INTO @AlertDetails(
    InstanceID,
    Priority,
    Message,
    AlertKey,
    RuleID
)
SELECT RD.InstanceID,
       RD.Priority,
       CASE RD.RiskType
           WHEN 'NoFullBackup' THEN CONCAT(RD.name, ' is in ', RD.recovery_model_desc, ' recovery but has no full backup, so a valid log backup chain cannot exist.')
           WHEN 'NoLogBackup' THEN CONCAT(RD.name, ' is in ', RD.recovery_model_desc, ' recovery and has a full backup, but no log backup was found.')
           WHEN 'LogChainNotStartedAfterFull' THEN CONCAT(RD.name, ' has a newer full backup than log backup. Last full backup: ', CONVERT(VARCHAR(19), RD.LastFull, 120), ' UTC. Last log backup: ', CONVERT(VARCHAR(19), RD.LastLog, 120), ' UTC.')
           WHEN 'LogBackupTooOld' THEN CONCAT(RD.name, ' is in ', RD.recovery_model_desc, ' recovery and the last log backup was ', FORMAT(RD.LogBackupAgeMins, 'N0'), ' minutes ago. Threshold: ', FORMAT(RD.ThresholdMins, 'N0'), ' minutes.')
       END,
       LEFT(REPLACE(RD.AlertKeyTemplate, '{DatabaseName}', RD.name), 128),
       RD.RuleID
FROM RiskData RD
WHERE RD.RiskType IS NOT NULL

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type
