CREATE FUNCTION dbo.InstancesMatchingTags(@TagIDs VARCHAR(MAX))
RETURNS TABLE
AS
RETURN
WITH T AS (
SELECT * FROM dbo.Tags T
JOIN STRING_SPLIT(@TagIDs,',') ss ON ss.value = T.TagID
)
SELECT * 
FROM dbo.Instances I
WHERE EXISTS(SELECT 1
			FROM T
			JOIN dbo.InstanceTags IT ON IT.TagID = T.TagID
			WHERE IT.Instance = I.Instance
			GROUP BY IT.Instance
			HAVING COUNT(DISTINCT T.TagName) = (SELECT COUNT(DISTINCT TagName) FROM T)
			)
AND @TagIDs IS NOT NULL AND @TagIDs <> '' AND @TagIDs<>'-1'
UNION ALL
SELECT * FROM dbo.Instances
WHERE @TagIDs IS NULL OR @TagIDs='' OR @TagIDs='-1'