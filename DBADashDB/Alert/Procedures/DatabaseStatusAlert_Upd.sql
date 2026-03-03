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
	ExcludedStates NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NULL,
	RuleID INT NOT NULL
)

/* Get a list of rules and the instances they apply to */
INSERT INTO #DatabaseStatusApplicable(
	InstanceID,
	Priority,
	DatabaseName,
	ExcludedStates,
	RuleID
)
SELECT I.InstanceID,
		R.Priority,
		NULLIF(JSON_VALUE(R.Details,'$.DatabaseName'),'') AS DatabaseName,
		CASE
			WHEN JSON_VALUE(R.Details,'$.ExcludedStates[0]') IS NULL THEN '[0,1,7,10]'
			ELSE JSON_QUERY(R.Details,'$.ExcludedStates')
		END AS ExcludedStates,
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
WHERE D.IsActive = 1
AND NOT EXISTS(SELECT 1 FROM OPENJSON(DSA.ExcludedStates) WHERE CAST(value AS INT) = D.state)

EXEC Alert.ActiveAlerts_Upd @AlertDetails = @AlertDetails, @AlertType = @Type, @ResolveAlertsOfType = 0

/* Resolve alerts when the database is no longer in the triggered set:
   - Database returned to an excluded state (e.g. back ONLINE)
   - Database was dropped (IsActive = 0) or renamed
   - Rule was updated (e.g. ExcludedStates now includes the current state) */
UPDATE AA
		SET ResolvedDate  = SYSUTCDATETIME(),
		IsResolved    = 1,
		LastMessage   = 'Issue resolved',
		UpdateCount  += 1,
		ResolvedCount += 1,
		UpdatedDate   = SYSUTCDATETIME()
FROM Alert.ActiveAlerts AA
WHERE AA.AlertType = @Type
AND NOT EXISTS(
			SELECT 1
			FROM @AlertDetails AD
			WHERE AD.InstanceID = AA.InstanceID
			AND AD.AlertKey = AA.AlertKey
)
AND AA.IsResolved = 0
