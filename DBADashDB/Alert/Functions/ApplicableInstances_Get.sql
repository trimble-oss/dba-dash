CREATE  FUNCTION Alert.ApplicableInstances_Get(
	@RuleID INT,
	@TagID INT,
	@InstanceID INT,
	@AlertKey NVARCHAR(256)='%',
	@ApplyToHidden BIT=0
)
RETURNS TABLE
AS
/*
	Instances a rule applies to, excluding instances in a blackout period.
	Tag-scoped rules (single tag or multiple tags) and blackouts are resolved once per alert run by Alert.ApplicableInstances_Upd.
	Only one branch runs per rule: all instances / a specific instance scan dbo.Instances, tag rules seek Alert.RuleInstanceMatch.
*/
RETURN
SELECT I.InstanceID, ISNULL(I.scheduler_count,1) AS scheduler_count
FROM (
	/* All instances or a specific instance */
	SELECT I.InstanceID
	FROM dbo.Instances I
	WHERE @TagID = -1
	AND (@InstanceID IS NULL OR I.InstanceID = @InstanceID)
	UNION ALL
	/* Tag rules */
	SELECT M.InstanceID
	FROM Alert.RuleInstanceMatch M
	WHERE M.RuleID = @RuleID
	AND @TagID <> -1
	AND @InstanceID IS NULL
) X
JOIN dbo.Instances I ON I.InstanceID = X.InstanceID
WHERE I.IsActive=1
AND (I.ShowInSummary=1 OR @ApplyToHidden=1)
AND NOT EXISTS(
			SELECT 1
			FROM Alert.ActiveBlackout AB
			WHERE AB.InstanceID = I.InstanceID
			AND @AlertKey LIKE AB.AlertKeyPattern
			)
