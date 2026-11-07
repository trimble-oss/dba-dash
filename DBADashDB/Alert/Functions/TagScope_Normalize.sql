CREATE FUNCTION Alert.TagScope_Normalize(
	@ApplyToTagID INT,
	@ApplyToTagIDs VARCHAR(MAX)
)
RETURNS TABLE
AS
/*
	Canonical tag scope for a rule.  @ApplyToTagIDs is a comma-separated list of TagIDs:
	- Empty: @ApplyToTagID as passed (-1 for all instances or a single tag)
	- 1 tag: stored as ApplyToTagID
	- 2+ tags: ApplyToTagID = -2 with the distinct TagIDs sorted, so the same tags in a different order give the same RuleHash
*/
RETURN
WITH T AS (
	SELECT DISTINCT TRY_CAST(SS.value AS INT) AS TagID
	FROM STRING_SPLIT(@ApplyToTagIDs,',') SS
	WHERE TRY_CAST(SS.value AS INT) > 0
)
SELECT CASE WHEN C.Cnt = 0 THEN @ApplyToTagID
			WHEN C.Cnt = 1 THEN C.MinTagID
			ELSE -2 END AS ApplyToTagID,
	/* STUFF/FOR XML rather than STRING_AGG: the repository database supports SQL 2016 */
	CASE WHEN C.Cnt > 1 THEN STUFF((SELECT ',' + CAST(T.TagID AS VARCHAR(11)) FROM T ORDER BY T.TagID FOR XML PATH(''),TYPE).value('.','VARCHAR(MAX)'),1,1,'') END AS ApplyToTagIDs
FROM (	SELECT COUNT(*) AS Cnt,
			MIN(TagID) AS MinTagID
		FROM T
	) C
