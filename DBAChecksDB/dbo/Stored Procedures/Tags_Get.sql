CREATE PROC Tags_Get
AS
SELECT TagID,TagName,TagValue 
FROM dbo.Tags
ORDER BY TagName,TagValue