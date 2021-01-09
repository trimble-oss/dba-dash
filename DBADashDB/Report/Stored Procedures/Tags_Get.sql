CREATE PROC [Report].[Tags_Get]
AS
SELECT -1 AS TagID,'{All Instances}' AS Tag,0 AS Sort
UNION ALL
SELECT TagID,TagName + ' | ' + TagValue AS Tag,CASE WHEN LEFT(TagName,1)='{' THEN 2 ELSE 1 END as Sort
FROM dbo.Tags
WHERE TagID>0
ORDER BY Sort, Tag