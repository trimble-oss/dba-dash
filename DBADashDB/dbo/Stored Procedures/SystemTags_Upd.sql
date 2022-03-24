CREATE PROC dbo.SystemTags_Upd(
	@InstanceID INT
)
AS
DECLARE @Tags TABLE(
	TagID INT NULL,
	TagName NVARCHAR(50) NOT NULL,
	TagValue NVARCHAR(128) NOT NULL
);

WITH T AS (
	SELECT CAST(v.SQLVersionName AS NVARCHAR(128)) AS [Version], 
			CAST(I.Edition AS NVARCHAR(128)) AS Edition,
			CAST(v.SQLVersionName + ' ' + I.ProductLevel + ISNULL(' ' + I.ProductUpdateLevel,'') AS NVARCHAR(128)) AS PatchLevel,
			CAST(I.Collation AS NVARCHAR(128)) Collation,
			CAST(I.SystemManufacturer AS NVARCHAR(128)) AS SystemManufacturer,
			CAST(I.SystemProductName AS NVARCHAR(128)) AS SystemProductName,
			CAST(RIGHT(REPLICATE(' ',5) +  CAST(I.cpu_count as NVARCHAR(50)),5) AS NVARCHAR(128)) AS CPUCount,
			CAST(Cagt.AgentHostName AS NVARCHAR(128)) CollectAgent,
			CAST(Cagt.AgentServiceName AS NVARCHAR(128)) CollectAgentServiceName,
			CAST(Cagt.AgentVersion AS NVARCHAR(128)) AS CollectAgentVersion,
			CAST(Cagt.AgentPath AS NVARCHAR(128)) AS CollectAgentPath,
			CAST(Iagt.AgentHostName AS NVARCHAR(128)) ImportAgent,
			CAST(Iagt.AgentServiceName AS NVARCHAR(128)) ImportAgentServiceName,
			CAST(Iagt.AgentVersion AS NVARCHAR(128)) AS ImportAgentVersion,
			CAST(Iagt.AgentPath AS NVARCHAR(128)) AS ImportAgentPath
	FROM dbo.Instances I
	JOIN dbo.DBADashAgent Cagt ON I.CollectAgentID = Cagt.DBADashAgentID
	JOIN dbo.DBADashAgent Iagt ON I.ImportAgentID = Iagt.DBADashAgentID
	CROSS APPLY dbo.SQLVersionName(I.EditionID,I.ProductVersion) v
	WHERE I.InstanceID=@InstanceID
)
INSERT INTO @Tags
(
    TagName,
    TagValue
)
SELECT 	'{' + TagName + '}' as TagName,
		ISNULL(TagValue,'')
FROM T
UNPIVOT(TagValue FOR TagName IN(PatchLevel, 
								[Version],
								Edition, 
								Collation, 
								SystemManufacturer, 
								SystemProductName, 
								CPUCount, 
								ImportAgent,
								ImportAgentVersion,
								ImportAgentServiceName,
								ImportAgentPath,
								CollectAgent,
								CollectAgentVersion,
								CollectAgentServiceName,
								CollectAgentPath
								)
		) upvt


IF EXISTS(SELECT 1 
			FROM @Tags T
			WHERE NOT EXISTS(SELECT 1 
						FROM dbo.Tags TG
						WHERE TG.TagName = t.TagName 
						AND TG.TagValue = t.TagValue
						)
			)
BEGIN
	INSERT INTO dbo.Tags
	(
		TagName,
		TagValue
	)
	SELECT T.TagName,T.TagValue
	FROM @Tags T
	WHERE NOT EXISTS(SELECT 1 
				FROM dbo.Tags TG WITH(UPDLOCK,HOLDLOCK)
				WHERE TG.TagName = t.TagName 
				AND TG.TagValue = t.TagValue
				)
END

UPDATE TMP
	SET TMP.TagID = TG.TagID
FROM @Tags TMP
JOIN dbo.Tags TG ON TMP.TagName = TG.TagName AND TMP.TagValue = TG.TagValue

DELETE IT 
FROM dbo.InstanceIDsTags IT
JOIN dbo.Tags T ON IT.TagID = T.TagID 
WHERE IT.InstanceID = @InstanceID
AND T.TagName LIKE '{%'
AND NOT EXISTS(SELECT 1 
			FROM @Tags tmp
			WHERE T.TagID = tmp.TagID
			)

IF EXISTS(SELECT 1
		FROM @Tags T 
		WHERE NOT EXISTS(SELECT 1 
				FROM dbo.InstanceIDsTags IT 
				WHERE IT.InstanceID = @InstanceID 
				AND IT.TagID = T.TagID)
		)
BEGIN
	INSERT INTO dbo.InstanceIDsTags
	(
		InstanceID,
		TagID
	)
	SELECT	@InstanceID,
			T.TagID
	FROM @Tags T 
	WHERE NOT EXISTS(SELECT 1 
				FROM dbo.InstanceIDsTags IT WITH(UPDLOCK,HOLDLOCK)
				WHERE IT.InstanceID = @InstanceID 
				AND IT.TagID = T.TagID)
END

IF EXISTS(SELECT 1	
			FROM dbo.Tags T
			WHERE NOT EXISTS(SELECT 1 
							FROM dbo.InstanceIDsTags IT 
							WHERE IT.TagID=T.TagID
							UNION ALL 
							SELECT 1 
							FROM dbo.InstanceTags IT
							WHERE IT.TagID = T.TagID
							)
			)
BEGIN
	DELETE T 
	FROM dbo.Tags T
	WHERE NOT EXISTS(SELECT 1 
					FROM dbo.InstanceIDsTags IT 
					WHERE IT.TagID=T.TagID
					UNION ALL 
					SELECT 1 
					FROM dbo.InstanceTags IT
					WHERE IT.TagID = T.TagID
					)
END