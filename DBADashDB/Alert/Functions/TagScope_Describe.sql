CREATE FUNCTION Alert.TagScope_Describe(
	@ApplyToTagIDs VARCHAR(MAX)
)
RETURNS TABLE
AS
/*
	Description of a multiple tag scope (alert rule or blackout period) for display.  e.g. (Role:Production OR Role:UAT) AND Region:APAC
	Groups are ordered by tag name, values within a group by text.  Matches DBADashTagSet.ToString in the GUI.
	STUFF/FOR XML rather than STRING_AGG: the repository database supports SQL 2016.
*/
RETURN
WITH RT AS (
	SELECT CASE WHEN T.TagID IS NULL THEN CONCAT(N'#',SS.value) ELSE UPPER(T.TagName) END COLLATE Latin1_General_100_BIN2 AS TagGroup, /* Tag names group case-insensitively (accent-sensitive), as in alert evaluation */
		CASE WHEN T.TagID IS NULL THEN CONCAT(N'{Deleted tag ',SS.value,N'}') ELSE CONCAT(T.TagName,N':',T.TagValue) END AS TagText
	FROM STRING_SPLIT(@ApplyToTagIDs,',') SS
	LEFT JOIN dbo.Tags T ON T.TagID = TRY_CAST(SS.value AS INT)
),
TG AS (
	SELECT G.TagGroup,
		CASE WHEN G.Cnt > 1 THEN CONCAT(N'(',V.ValueList,N')') ELSE V.ValueList END AS GroupText
	FROM (	SELECT RT.TagGroup,COUNT(*) AS Cnt
			FROM RT
			GROUP BY RT.TagGroup
		) G
	CROSS APPLY(SELECT STUFF((	SELECT N' OR ' + RT.TagText
								FROM RT
								WHERE RT.TagGroup = G.TagGroup
								ORDER BY RT.TagText
								FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,4,N'') AS ValueList
				) V
)
SELECT STUFF((	SELECT N' AND ' + TG.GroupText
				FROM TG
				ORDER BY TG.TagGroup
				FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,5,N'') AS TagText
