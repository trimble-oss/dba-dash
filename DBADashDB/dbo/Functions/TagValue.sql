/* 
	Returns tag value(s) associated with a specified tag for an instance.
	Note: Can return zero rows or more than 1 row.  
	e.g. An instance can be tagged Product:ABC and Product:XYZ
*/
CREATE FUNCTION dbo.TagValue(
	@InstanceID INT,
	@TagName NVARCHAR(50),
	@EngineEdition INT
)
RETURNS TABLE 
AS
RETURN
/* Tags associated with the InstanceID.  System tags and user tags for regular instances */
SELECT  T.TagValue
FROM dbo.InstanceIDsTags IT 
JOIN dbo.Tags T ON IT.TagID = T.TagID
WHERE InstanceID = @InstanceID
AND T.TagName = @TagName
UNION /* There could be duplicate tags to remove for Azure DB, so use UNION */
/* 
Tags associated with the Instance name.  These are user defined tags for AzureDB.
System tags for AzureDB are associated with InstanceID. (Some tags like CPU count can vary by database)
*/
SELECT T.TagValue
FROM dbo.Instances I
JOIN dbo.InstanceTags IT ON IT.Instance = I.Instance
JOIN dbo.Tags T ON IT.TagID = T.TagID 
WHERE I.InstanceID = @InstanceID
AND I.EngineEdition = 5 /* AzureDB */
AND T.TagName= @TagName
AND T.TagName NOT LIKE '{%' /* User tags only */
GO
