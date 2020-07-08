CREATE PROC [Report].[Tags_Get]
AS
SELECT -1 AS TagID,'{All Instances}' AS Tag
UNION ALL
SELECT TagID,TagName + ' | ' + TagValue AS Tag
FROM dbo.Tags
WHERE TagID>0