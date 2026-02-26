CREATE PROC Alert.DatabaseStatusAlert_Upd
AS
SET NOCOUNT ON
DECLARE @Type VARCHAR(50) = 'DatabaseStatus'
DECLARE @AlertKeyPrefix NVARCHAR(128) = 'DB:'

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

CREATE TABLE #DatabaseStatusApplicable(
	InstanceID INT NOT NULL,
	Priority TINYINT NOT NULL,
	DatabaseName NVARCHAR(128) COLLATE DATABASE_DEFAULT NULL,
	RuleID INT NOT NULL
)

/* Get a list of rules and the instances they apply to */
INSERT INTO #DatabaseStatusApplicable(
	InstanceID,
	Priority,
	DatabaseName,
	RuleID
)
SELECT I.InstanceID,
		R.Priority,
		NULLIF(JSON_VALUE(R.Details,'$.DatabaseName'),'') AS DatabaseName,
		R.RuleID
FROM Alert.Rules R
CROSS APPLY Alert.ApplicableInstances_Get(R.ApplyToTagID, R.ApplyToInstanceID, R.AlertKey, R.ApplyToHidden) I
WHERE R.Type = @Type
AND R.IsActive = 1

DECLARE @AlertDetails Alert.AlertDetails

INSERT INTO @AlertDetails(
	InstanceID,
	Priority,
	Message,
	AlertKey,
	RuleID
)
SELECT D.InstanceID,
		DSA.Priority,
		CONCAT(D.name, ' is ', D.state_desc),
		CONCAT(@AlertKeyPrefix, D.name) AS AlertKey,
		DSA.RuleID
FROM dbo.Databases D
JOIN #DatabaseStatusApplicable DSA ON D.InstanceID = DSA.InstanceID
		AND (D.name LIKE DSA.DatabaseName OR DSA.DatabaseName IS NULL)
WHERE D.state <> 0	/* not ONLINE */
AND D.IsActive = 1

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type, @ResolveAlertsOfType = 0

/* Resolve alerts for databases that are now ONLINE or have been removed */
UPDATE AA
		SET ResolvedDate  = SYSUTCDATETIME(),
		IsResolved    = 1,
		LastMessage   = 'Issue resolved',
		UpdateCount  += 1,
		ResolvedCount += 1,
		UpdatedDate   = SYSUTCDATETIME()
FROM Alert.ActiveAlerts AA
WHERE AA.AlertType = @Type
AND EXISTS(
	SELECT 1
	FROM dbo.Databases D
	WHERE D.InstanceID = AA.InstanceID
	AND D.name = STUFF(AA.AlertKey, 1, LEN(@AlertKeyPrefix), '')
	AND (D.state = 0 OR D.IsActive = 0)	/* back online or removed */
)
AND AA.IsResolved = 0
