CREATE PROC [dbo].[InstanceTags_Get](@Instance SYSNAME)
AS
SELECT T.TagID,T.TagName,T.TagValue,CASE WHEN IT.TagID IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS Checked
FROM dbo.Tags T
LEFT JOIN dbo.InstanceTags IT ON IT.TagID = T.TagID AND IT.Instance=@Instance
ORDER BY TagName,TagValue