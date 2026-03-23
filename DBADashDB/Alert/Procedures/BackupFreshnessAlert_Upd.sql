CREATE PROC Alert.BackupFreshnessAlert_Upd
AS
SET NOCOUNT ON
DECLARE @Type VARCHAR(50) = 'BackupFreshness'

/* Check if we have any rules to process */
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

CREATE TABLE #BackupFreshnessApplicable(
    InstanceID INT NOT NULL,
    Priority TINYINT NOT NULL,
    BackupType CHAR(1) NOT NULL,
    DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
    ExcludedDatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
    MinimumDatabaseAgeMins INT NULL,
    ThresholdMins INT NOT NULL,
    AlertKeyTemplate NVARCHAR(128) COLLATE DATABASE_DEFAULT NOT NULL,
    RuleID INT NOT NULL
)

/* Get rules and the instances they apply to */
INSERT INTO #BackupFreshnessApplicable(
    InstanceID,
    Priority,
    BackupType,
    DatabaseName,
    ExcludedDatabaseName,
    MinimumDatabaseAgeMins,
    ThresholdMins,
    AlertKeyTemplate,
    RuleID
)
SELECT I.InstanceID,
       R.Priority,
       CASE UPPER(JSON_VALUE(R.Details, '$.BackupType'))
            WHEN 'FULL' THEN 'D'
            WHEN 'DIFF' THEN 'I'
            WHEN 'LOG' THEN 'L'
       END AS BackupType,
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
AND UPPER(JSON_VALUE(R.Details, '$.BackupType')) IN ('FULL', 'DIFF', 'LOG')

DECLARE @AlertDetails Alert.AlertDetails

;WITH BackupData AS (
    SELECT BS.InstanceID,
           BS.DatabaseID,
           BS.name,
           BS.create_date_utc,
           CASE A.BackupType
                WHEN 'D' THEN BS.LastFull
                WHEN 'I' THEN BS.LastDiff
                WHEN 'L' THEN BS.LastLog
           END AS LastBackupDateUTC,
           CASE A.BackupType
                WHEN 'D' THEN 'Full'
                WHEN 'I' THEN 'Diff'
                WHEN 'L' THEN 'Log'
           END AS BackupTypeName,
           A.Priority,
           A.MinimumDatabaseAgeMins,
           A.ThresholdMins,
           A.AlertKeyTemplate,
           A.RuleID
    FROM #BackupFreshnessApplicable A
    JOIN dbo.BackupStatus BS ON BS.InstanceID = A.InstanceID
    WHERE (BS.name LIKE A.DatabaseName OR A.DatabaseName IS NULL)
    AND NOT (BS.name LIKE A.ExcludedDatabaseName AND A.ExcludedDatabaseName IS NOT NULL)
    AND (A.MinimumDatabaseAgeMins IS NULL OR BS.create_date_utc <= DATEADD(MINUTE, -A.MinimumDatabaseAgeMins, GETUTCDATE()))
    AND (
            (A.BackupType = 'D' AND BS.FullBackupExcludedReason IS NULL)
            OR (A.BackupType = 'I' AND BS.DiffBackupExcludedReason IS NULL)
            OR (A.BackupType = 'L' AND BS.LogBackupExcludedReason IS NULL)
        )
), ExceededThreshold AS (
    SELECT BD.InstanceID,
           BD.DatabaseID,
           BD.name,
           BD.Priority,
           BD.ThresholdMins,
           BD.LastBackupDateUTC,
           BD.BackupTypeName,
           BD.AlertKeyTemplate,
           BD.RuleID,
           DATEDIFF(MINUTE, BD.LastBackupDateUTC, GETUTCDATE()) AS BackupAgeMins
    FROM BackupData BD
    WHERE BD.LastBackupDateUTC IS NULL
       OR DATEDIFF(MINUTE, BD.LastBackupDateUTC, GETUTCDATE()) >= BD.ThresholdMins
)
INSERT INTO @AlertDetails(
    InstanceID,
    Priority,
    Message,
    AlertKey,
    RuleID
)
SELECT ET.InstanceID,
       ET.Priority,
       CASE
           WHEN ET.LastBackupDateUTC IS NULL THEN CONCAT('No ', LOWER(ET.BackupTypeName), ' backup found for ', ET.name, '. Threshold: ', FORMAT(ET.ThresholdMins, 'N0'), ' minutes.')
           ELSE CONCAT(
                'Last ', LOWER(ET.BackupTypeName), ' backup for ', ET.name,
                ' was ', FORMAT(ET.BackupAgeMins, 'N0'), ' minutes ago at ',
                CONVERT(VARCHAR(19), ET.LastBackupDateUTC, 120),
                ' UTC. Threshold: ', FORMAT(ET.ThresholdMins, 'N0'), ' minutes.'
           )
       END,
       LEFT(REPLACE(REPLACE(ET.AlertKeyTemplate, '{DatabaseName}', ET.name), '{BackupType}', ET.BackupTypeName), 128),
       ET.RuleID
FROM ExceededThreshold ET

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type
