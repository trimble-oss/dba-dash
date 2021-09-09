CREATE PROC dbo.TagReport_Get(
	@InstanceIDs VARCHAR(MAX)=NULL
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
	SELECT IT.Instance,
		T.TagName,
		T.TagValue
	FROM dbo.Tags T 
	JOIN dbo.InstanceTags IT ON T.TagID = IT.TagID
	WHERE EXISTS(SELECT 1 
				FROM dbo.Instances I 
				' + CASE WHEN @InstanceIDs IS NULL THEN '' ELSE 'JOIN STRING_SPLIT(@InstanceIDs,'','') ss ON ss.value = I.InstanceID' END + '
				WHERE I.Instance = IT.Instance
				AND I.IsActive=1
				)
)
/* 
	We can have multiple tag values for a tag.  Get a distinct list of tags for each instance with a comma separated list of values
	Could be done with STRING_AGG but want to support SQL 2016 for repository DB
*/
, V AS (
	SELECT Instance,
			TagName,
			STUFF((SELECT '', '' + T2.TagValue 
					FROM T T2 
					WHERE T1.Instance = T2.Instance 
					AND T1.TagName = T2.TagName 
					ORDER BY T2.TagValue
					FOR XML PATH(''''),TYPE).value(''.'',''NVARCHAR(MAX)''),1,2,'''') AS TagValues
	FROM T T1
	GROUP BY Instance,TagName
)
SELECT *
FROM V 
PIVOT( MAX(TagValues) FOR TagName IN(' + @ColList + ')) AS pvt'

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX)',@InstanceIDs


