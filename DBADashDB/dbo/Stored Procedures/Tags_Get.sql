CREATE PROC dbo.Tags_Get
AS
SELECT TagID,TagName,TagValue 
FROM dbo.Tags T 
WHERE EXISTS(SELECT 1 
			FROM dbo.InstanceTags IT 
			JOIN dbo.Instances I ON IT.Instance = I.Instance
			WHERE IT.TagID = T.TagID
			AND I.IsActive=1
			)
ORDER BY TagName,TagValue
