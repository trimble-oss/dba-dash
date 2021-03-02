CREATE PROC [dbo].[Tags_Get](
	@TagFilters NVARCHAR(MAX)=NULL
)
AS

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT TagID,TagName,TagValue 
FROM dbo.Tags T 
WHERE EXISTS(SELECT 1 
			FROM dbo.InstanceTags IT 
			JOIN dbo.Instances I ON IT.Instance = I.Instance
			WHERE IT.TagID = T.TagID
			AND I.IsActive=1
			)
' + CASE WHEN @TagFilters IS NULL THEN '' ELSE 'AND EXISTS(SELECT TagName,TagValue 
		INTERSECT
		SELECT SUBSTRING(ss.Value,0,CHARINDEX('':'',ss.value)),
				 SUBSTRING(ss.Value,CHARINDEX('':'',ss.value)+1,LEN(ss.value))
			FROM STRING_SPLIT(@TagFilters,'','') ss
		)' END + '
ORDER BY TagName,TagValue'

EXEC sp_executesql @SQL,N'@TagFilters NVARCHAR(MAX)',@TagFilters
