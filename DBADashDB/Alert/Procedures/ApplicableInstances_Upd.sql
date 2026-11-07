CREATE PROC Alert.ApplicableInstances_Upd
AS
/*
	Resolves which instances the alert rules apply to, once per alert run:
	- Alert.RuleInstanceMatch: instances matching each tag-scoped rule (single tag or multiple tags)
	- Alert.ActiveBlackout: blackout periods in effect for each instance right now
	Alert.ApplicableInstances_Get reads these instead of evaluating tags & blackout periods for every rule x instance.

	Alert.Alerts_Upd runs this before the alert procs.  An alert proc run on its own runs this first (see Alert.ApplicableInstances_Ensure).

	Tag matching here is set-based for every rule x instance.  Alert.TagScopeMatchesInstance is the single instance equivalent - keep them in step
	(tests check they agree).  Tag names are grouped case-insensitively (but accent-sensitively) in both, matching the GUI's description of the scope.
*/
SET NOCOUNT ON
SET XACT_ABORT ON

CREATE TABLE #Match(
	RuleID INT NOT NULL,
	InstanceID INT NOT NULL,
	PRIMARY KEY(RuleID,InstanceID)
);
CREATE TABLE #Blackout(
	InstanceID INT NOT NULL,
	AlertKeyPattern NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL,
	PRIMARY KEY(InstanceID,AlertKeyPattern)
);
CREATE TABLE #BP(
	ApplyToInstanceID INT NULL,
	ApplyToTagID INT NOT NULL,
	ApplyToTagIDs VARCHAR(800) COLLATE DATABASE_DEFAULT NULL,
	AlertKey NVARCHAR(256) COLLATE DATABASE_DEFAULT NOT NULL
);

/*	The applock serializes concurrent alert runs (e.g. another service taking over alert processing while a slow run is still going).
	Taken before resolving rather than just around the writes, so a slower run can't overwrite a newer result with an older one. */
BEGIN TRAN

DECLARE @LockResult INT
EXEC @LockResult = sp_getapplock @Resource = 'Alert.ApplicableInstances_Upd', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 60000;
IF @LockResult < 0
BEGIN
	THROW 50000, 'Alert.ApplicableInstances_Upd: unable to acquire the applock - another alert run is still updating the rule instance match.', 1;
END;

/*	Tags each rule requires.  Tags with the same name form a group - an instance needs at least 1 tag from every group.
	A tag that no longer exists is a group of its own that can't be matched, so deleting a tag never widens a rule. */
WITH RuleTags AS (
	SELECT R.RuleID, R.ApplyToTagID AS TagID
	FROM Alert.Rules R
	WHERE R.ApplyToTagID > 0
	AND R.ApplyToInstanceID IS NULL
	AND R.IsActive = 1
	UNION
	SELECT R.RuleID, TRY_CAST(SS.value AS INT) AS TagID
	FROM Alert.Rules R
	CROSS APPLY STRING_SPLIT(R.ApplyToTagIDs,',') SS
	WHERE R.ApplyToTagID = -2
	AND R.ApplyToInstanceID IS NULL
	AND R.IsActive = 1
	AND TRY_CAST(SS.value AS INT) > 0
),
Req AS (
	SELECT RT.RuleID,
		RT.TagID,
		CASE WHEN T.TagID IS NULL THEN CONCAT(N'#',RT.TagID) ELSE CONCAT(N'=',UPPER(T.TagName)) END COLLATE Latin1_General_100_BIN2 AS TagGroup, /* Binary after UPPER: case-insensitive but accent-sensitive whatever the repository collation, like the GUI */
		IIF(T.TagID IS NULL,0,1) AS TagExists
	FROM RuleTags RT
	LEFT JOIN dbo.Tags T ON T.TagID = RT.TagID
),
ReqGroups AS (
	SELECT RuleID, COUNT(DISTINCT TagGroup) AS Groups
	FROM Req
	GROUP BY RuleID
),
/* Instance tags plus Azure server level tags, so a rule can combine both */
InstTags AS (
	SELECT IT.InstanceID, IT.TagID
	FROM dbo.InstanceIDsTags IT
	UNION
	SELECT I.InstanceID, IT.TagID
	FROM dbo.InstanceTags IT
	JOIN dbo.Instances I ON I.Instance = IT.Instance
),
Hit AS (
	SELECT Req.RuleID, IT.InstanceID, COUNT(DISTINCT Req.TagGroup) AS Groups
	FROM Req
	JOIN InstTags IT ON IT.TagID = Req.TagID
	WHERE Req.TagExists = 1 /* A deleted tag can't match, even if assignments to it were left behind */
	GROUP BY Req.RuleID, IT.InstanceID
)
INSERT INTO #Match(RuleID,InstanceID)
SELECT H.RuleID, H.InstanceID
FROM Hit H
JOIN ReqGroups RG ON RG.RuleID = H.RuleID AND RG.Groups = H.Groups
WHERE EXISTS(SELECT 1 FROM dbo.Instances I WHERE I.InstanceID = H.InstanceID AND I.IsActive = 1);

/* Blackout periods in effect - evaluated once (time zone conversion etc) rather than for every instance */
INSERT INTO #BP(ApplyToInstanceID,ApplyToTagID,ApplyToTagIDs,AlertKey)
SELECT ApplyToInstanceID, ApplyToTagID, ApplyToTagIDs, AlertKey
FROM Alert.BlackoutPeriodsInEffect_Get();

/* Same rules as Alert.IsBlackoutPeriod, evaluated once per instance rather than per rule x instance */
INSERT INTO #Blackout(InstanceID,AlertKeyPattern)
SELECT DISTINCT I.InstanceID, X.AlertKey
FROM dbo.Instances I
CROSS APPLY(
	/* Collections paused from the GUI - alerts are muted until they resume */
	SELECT N'%' AS AlertKey
	WHERE I.CollectionsPausedUntil > SYSUTCDATETIME()
	UNION ALL
	/* All instances */
	SELECT BP.AlertKey
	FROM #BP BP
	WHERE BP.ApplyToTagID = -1
	AND BP.ApplyToInstanceID = -1
	UNION ALL
	/* Specific instance */
	SELECT BP.AlertKey
	FROM #BP BP
	WHERE BP.ApplyToInstanceID = I.InstanceID
	AND BP.ApplyToTagID = -1
	UNION ALL
	/* Tag */
	SELECT BP.AlertKey
	FROM #BP BP
	WHERE BP.ApplyToInstanceID = -1
	AND EXISTS(SELECT 1 FROM dbo.InstanceIDsTags IT WHERE IT.InstanceID = I.InstanceID AND IT.TagID = BP.ApplyToTagID)
	UNION ALL
	/* Tag (Azure server level) */
	SELECT BP.AlertKey
	FROM #BP BP
	WHERE BP.ApplyToInstanceID = -1
	AND EXISTS(SELECT 1 FROM dbo.InstanceTags IT WHERE IT.Instance = I.Instance AND IT.TagID = BP.ApplyToTagID)
	UNION ALL
	/* Multiple tags.  Blackout periods are few, so checking each instance against them is cheap */
	SELECT BP.AlertKey
	FROM #BP BP
	CROSS APPLY Alert.TagScopeMatchesInstance(BP.ApplyToTagID,BP.ApplyToTagIDs,I.InstanceID) TM
	WHERE BP.ApplyToInstanceID = -1
	AND BP.ApplyToTagID = -2
	AND TM.IsMatch = 1
) X
WHERE I.IsActive = 1;

/* Only write the differences */

DELETE M
FROM Alert.RuleInstanceMatch M
WHERE NOT EXISTS(SELECT 1 FROM #Match N WHERE N.RuleID = M.RuleID AND N.InstanceID = M.InstanceID);

INSERT INTO Alert.RuleInstanceMatch(RuleID,InstanceID)
SELECT N.RuleID, N.InstanceID
FROM #Match N
WHERE NOT EXISTS(SELECT 1 FROM Alert.RuleInstanceMatch M WHERE M.RuleID = N.RuleID AND M.InstanceID = N.InstanceID);

DELETE AB
FROM Alert.ActiveBlackout AB
WHERE NOT EXISTS(SELECT 1 FROM #Blackout N WHERE N.InstanceID = AB.InstanceID AND N.AlertKeyPattern = AB.AlertKeyPattern);

INSERT INTO Alert.ActiveBlackout(InstanceID,AlertKeyPattern)
SELECT N.InstanceID, N.AlertKeyPattern
FROM #Blackout N
WHERE NOT EXISTS(SELECT 1 FROM Alert.ActiveBlackout AB WHERE AB.InstanceID = N.InstanceID AND AB.AlertKeyPattern = N.AlertKeyPattern);

COMMIT
