CREATE PROC [dbo].[SystemTags_Upd](@InstanceID INT)
AS
DECLARE @Tags TABLE(
	TagName NVARCHAR(50),
	TagValue NVARCHAR(50)
);
DECLARE @Instance SYSNAME
SELECT @Instance = Instance 
FROM dbo.Instances 
WHERE InstanceID = @InstanceID;

WITH T AS (
	SELECT CAST(v.SQLVersionName as NVARCHAR(50)) as [Version], 
			CAST(I.Edition as NVARCHAR(50)) as Edition,
			CAST(v.SQLVersionName + ' ' + ProductLevel + ISNULL(' ' + I.ProductUpdateLevel,'') as NVARCHAR(50)) as PatchLevel,
			CAST(I.Collation as NVARCHAR(50)) Collation,
			CAST(I.SystemManufacturer as NVARCHAR(50)) as SystemManufacturer,
			CAST(I.SystemProductName as NVARCHAR(50)) as SystemProductName,
			CAST(RIGHT(REPLICATE(' ',5) +  CAST(I.cpu_count as NVARCHAR(50)),5) as NVARCHAR(50)) as CPUCount,
			CAST(AgentHostName + ' {' + AgentVersion + '}' as NVARCHAR(50)) DBAChecksAgent
	FROM dbo.Instances I
	CROSS APPLY SQLVersionName(EditionID,ProductVersion) v
	WHERE I.InstanceID=@InstanceID
)
INSERT INTO @Tags
(
    TagName,
    TagValue
)
SELECT 	'{' + TagName + '}' as TagName,
		TagValue 
FROM T
UNPIVOT(TagValue FOR TagName IN(PatchLevel, [Version], Edition, Collation, SystemManufacturer, SystemProductName, CPUCount, DBAChecksAgent)) upvt


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


DELETE T 
FROM dbo.Tags T
WHERE NOT EXISTS(SELECT 1 FROM dbo.InstanceTags IT WHERE IT.TagID=T.TagID)