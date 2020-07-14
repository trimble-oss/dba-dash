CREATE PROC dbo.SystemTags_Upd(@InstanceID INT)
AS
DECLARE @Version SYSNAME
DECLARE @Edition SYSNAME
DECLARE @Instance SYSNAME
DECLARE @PatchLevel SYSNAME

SELECT @Version= v.SQLVersionName, @Edition=I.Edition,@Instance=I.Instance,@PatchLevel = ProductLevel + ISNULL(' ' + I.ProductUpdateLevel,'')
FROM dbo.Instances I
CROSS APPLY SQLVersionName(EditionID,ProductVersion) v
WHERE I.InstanceID=@InstanceID


DECLARE @Tags TABLE(TagName NVARCHAR(50),TagValue NVARCHAR(50))
INSERT INTO @Tags
(
    TagName,
    TagValue
)
SELECT '{Edition}',@Edition 
UNION ALL
SELECT '{Version}',@Version
UNION ALL
SELECT '{PatchLevel}',@PatchLevel

INSERT INTO dbo.Tags
(
    TagName,
    TagValue
)
SELECT TagName,TagValue
FROM @Tags t
WHERE NOT EXISTS(SELECT 1 
			FROM dbo.Tags tg
			WHERE tg.TagName = t.TagName 
			AND tg.TagValue = t.TagValue
			)

DELETE IT 
FROM dbo.InstanceTags IT
WHERE IT.Instance = @Instance
AND  EXISTS(SELECT 1 
			FROM @Tags t
			JOIN dbo.Tags tg ON t.TagName = tg.TagName AND t.TagValue <> tg.TagValue
			WHERE tg.TagID = IT.TagID
			)


INSERT INTO dbo.InstanceTags
(
    Instance,
    TagID
)
SELECT @Instance,tg.TagID
FROM @Tags T 
JOIN dbo.Tags tg ON tg.TagName = T.TagName AND tg.TagValue = T.TagValue
WHERE NOT EXISTS(SELECT 1 
			FROM dbo.InstanceTags IT 
			WHERE IT.Instance = @Instance 
			AND IT.TagID = tg.TagID)