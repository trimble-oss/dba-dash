CREATE FUNCTION Alert.TagScopeMatchesInstance(
	@ApplyToTagID INT,
	@ApplyToTagIDs VARCHAR(MAX),
	@InstanceID INT
)
RETURNS TABLE
AS
/*
	Does a single instance match a tag scope (alert rule or blackout period)?  Used where matching happens outside the per alert run tables: SQL Agent alerts as they're imported, Alert.IsBlackoutPeriod and the tag picker preview.
	Same rules as Alert.ApplicableInstances_Upd: tags with different names must all match (AND), tags with the same name are alternatives (OR).
	A tag that no longer exists can't be matched.
*/
RETURN
WITH Req AS (
	SELECT @ApplyToTagID AS TagID
	WHERE @ApplyToTagID > 0
	UNION
	SELECT TRY_CAST(SS.value AS INT)
	FROM STRING_SPLIT(@ApplyToTagIDs,',') SS
	WHERE @ApplyToTagID = -2
	AND TRY_CAST(SS.value AS INT) > 0
),
G AS (
	SELECT CASE WHEN T.TagID IS NULL THEN CONCAT(N'#',Req.TagID) ELSE CONCAT(N'=',UPPER(T.TagName)) END AS TagGroup,
		CASE WHEN EXISTS(
					SELECT 1
					FROM dbo.InstanceIDsTags IT
					WHERE IT.InstanceID = @InstanceID
					AND IT.TagID = Req.TagID
					)
			OR EXISTS(
					SELECT 1
					FROM dbo.InstanceTags IT
					JOIN dbo.Instances I ON I.Instance = IT.Instance
					WHERE I.InstanceID = @InstanceID
					AND IT.TagID = Req.TagID
					)
			THEN 1 ELSE 0 END AS HasTag
	FROM Req
	LEFT JOIN dbo.Tags T ON T.TagID = Req.TagID
)
SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM G)
				AND NOT EXISTS(
						SELECT 1
						FROM G
						GROUP BY G.TagGroup
						HAVING MAX(G.HasTag) = 0
						)
			THEN 1 ELSE 0 END AS BIT) AS IsMatch
