CREATE PROC dbo.InstanceTags_Get(
	@InstanceID INT,
	@Instance SYSNAME
)
AS
WITH T AS (
	-- For AzureDB
	SELECT	T.TagID,
			T.TagName,
			T.TagValue,
			CAST(1 AS BIT) AS Checked,
			CASE WHEN @InstanceID IS NULL THEN 0 ELSE 1 END AS IsInherited
	FROM dbo.Tags T
	JOIN dbo.InstanceTags IT ON IT.TagID = T.TagID 
	JOIN dbo.Instances I ON IT.Instance = I.Instance 
	WHERE I.EngineEdition=5 -- AzureDB
	AND I.Instance=@Instance
	UNION ALL
	-- For other instance types
	SELECT	T.TagID,
			T.TagName,
			T.TagValue,
			1 AS Checked,
			0 AS IsInherited
	FROM dbo.Tags T
	JOIN dbo.InstanceIDsTags IT ON IT.TagID = T.TagID
	WHERE IT.InstanceID=@InstanceID
	UNION ALL 
	SELECT T.TagID,
			T.TagName,
			T.TagValue,
			0 AS Checked,
			0 AS IsInherited
	FROM dbo.Tags T
	WHERE TagName NOT LIKE '{%'
)
SELECT TagID,
	   TagName,
	   TagValue,
	   CAST(MAX(Checked) AS BIT) AS Checked,
	   CAST(MAX(IsInherited) AS BIT) AS IsInherited
FROM T
GROUP BY TagID,
		 TagName,
		 TagValue
ORDER BY TagName,
		 TagValue
