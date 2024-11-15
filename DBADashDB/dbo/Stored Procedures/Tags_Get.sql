CREATE PROC dbo.Tags_Get(
	@TagFilters NVARCHAR(MAX)=NULL,
	@TagID INT=NULL
)
AS

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT	TagID,
		TagName,
		TagValue 
FROM dbo.Tags T 
WHERE ' + CASE WHEN @TagID IS NOT NULL THEN 'T.TagID = @TagID'
ELSE 'EXISTS(SELECT 1 
			FROM dbo.InstanceTags IT 
			JOIN dbo.Instances I ON IT.Instance = I.Instance
			WHERE IT.TagID = T.TagID
			AND I.IsActive=1
			UNION ALL
			SELECT 1 
			FROM dbo.InstanceIDsTags IT 
			JOIN dbo.Instances I ON IT.InstanceID = I.InstanceID
			WHERE IT.TagID = T.TagID
			AND I.IsActive=1
			)' END  + '
' + CASE WHEN @TagFilters IS NULL THEN '' ELSE 'AND EXISTS(SELECT TagName,TagValue 
		INTERSECT
		SELECT SUBSTRING(ss.Value,0,CHARINDEX('':'',ss.value)),
				 SUBSTRING(ss.Value,CHARINDEX('':'',ss.value)+1,LEN(ss.value))
			FROM STRING_SPLIT(@TagFilters,'','') ss
		)' END + '
ORDER BY TagName,TagValue'

EXEC sp_executesql @SQL,N'@TagFilters NVARCHAR(MAX),@TagID INT',@TagFilters,@TagID
