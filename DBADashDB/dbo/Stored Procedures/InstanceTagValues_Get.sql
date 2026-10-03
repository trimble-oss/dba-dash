CREATE PROC dbo.InstanceTagValues_Get
AS
/*
	Tags for every active instance, used to evaluate custom report visibility rules client side.
	Tags by instance name (Azure DB logical server) and by InstanceID are combined, as they are shown in the Tags tab.
	Returned as name/value rather than TagID as report metadata can be scripted to other repositories.
*/
SELECT	I.InstanceID,
		T.TagName,
		T.TagValue
FROM dbo.Instances I
JOIN dbo.InstanceTags IT ON IT.Instance = I.Instance
JOIN dbo.Tags T ON T.TagID = IT.TagID
WHERE I.IsActive=1
UNION
SELECT	I.InstanceID,
		T.TagName,
		T.TagValue
FROM dbo.Instances I
JOIN dbo.InstanceIDsTags IT ON IT.InstanceID = I.InstanceID
JOIN dbo.Tags T ON T.TagID = IT.TagID
WHERE I.IsActive=1
