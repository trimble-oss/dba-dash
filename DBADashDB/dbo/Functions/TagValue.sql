/* 
	Returns tag value(s) associated with a specified tag for an instance.
	Note: Can ruturn zero rows or more than 1 row
*/
CREATE FUNCTION dbo.TagValue(
	@InstanceID INT,
	@TagName NVARCHAR(50),
	@EngineEdition INT
)
RETURNS TABLE 
AS
RETURN
SELECT  T.TagValue
FROM dbo.InstanceIDsTags IT 
JOIN dbo.Tags T ON IT.TagID = T.TagID
WHERE InstanceID = @InstanceID
AND T.TagName = @TagName
AND @EngineEdition <> 5 -- AzureDB
UNION ALL
SELECT T.TagValue
FROM dbo.Instances I
JOIN dbo.InstanceTags IT ON IT.Instance = I.Instance
JOIN dbo.Tags T ON IT.TagID = T.TagID 
WHERE I.InstanceID = @InstanceID
AND I.EngineEdition = 5 -- AzureDB
AND T.TagName= @TagName

