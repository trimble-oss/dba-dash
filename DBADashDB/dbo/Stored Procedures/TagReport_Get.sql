CREATE PROC dbo.TagReport_Get(
	@InstanceIDs IDs READONLY,
	@ShowHidden BIT=1
)
AS
/*
	Report to return all the tags for the specified instances - pivoted with columns for each tag.
*/
DECLARE @SQL NVARCHAR(MAX)

DECLARE @ColList NVARCHAR(MAX) 

SET @ColList= STUFF((SELECT ', ' + QUOTENAME(TagName) 
FROM Tags
GROUP BY TagName
ORDER BY CASE WHEN TagName LIKE '{%' THEN 2 ELSE 1 END,TagName
FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'');

SET @SQL = N'
WITH T AS (
	SELECT DISTINCT I.InstanceGroupName AS Instance,
					CASE WHEN I.EngineEdition=5 THEN -1 ELSE I.InstanceID END AS InstanceID,
					T.TagName,
					T.TagValue
	FROM dbo.Tags T 
	JOIN dbo.InstanceIDsTags IT ON T.TagID = IT.TagID
	JOIN dbo.Instances I ON I.InstanceID = IT.InstanceID
	WHERE I.IsActive=1
	' + CASE WHEN @ShowHidden=1 THEN '' ELSE 'AND I.ShowInSummary=1' END + '
	' + CASE WHEN EXISTS(SELECT 1 FROM @InstanceIDs) THEN 'AND EXISTS(SELECT 1 
				FROM @InstanceIDs T 
				WHERE T.ID = I.InstanceID
				)' ELSE '' END + '
	
)
/* 
	We can have multiple tag values for a tag.  Get a distinct list of tags for each instance with a comma separated list of values
	Could be done with STRING_AGG but want to support SQL 2016 for repository DB
*/
, V AS (
	SELECT Instance,
			InstanceID,
			TagName,
			STUFF((SELECT '', '' + RTRIM(LTRIM(T2.TagValue))
					FROM T T2 
					WHERE T1.Instance = T2.Instance 
					AND T1.TagName = T2.TagName 
					ORDER BY T2.TagValue
					FOR XML PATH(''''),TYPE).value(''.'',''NVARCHAR(MAX)''),1,2,'''') AS TagValues
	FROM T T1
	GROUP BY Instance,
			 TagName,
			 InstanceID
)
SELECT *
FROM V 
PIVOT( MAX(TagValues) FOR TagName IN(' + @ColList + ')) AS pvt'

EXEC sp_executesql @SQL,N'@InstanceIDs IDs READONLY',@InstanceIDs
