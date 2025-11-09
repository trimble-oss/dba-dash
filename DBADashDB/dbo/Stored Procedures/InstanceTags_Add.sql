CREATE PROC InstanceTags_Add(
	@Instance SYSNAME,
	@InstanceID INT,
	@TagName NVARCHAR(50),
	@TagValue NVARCHAR(128),
	@TagID INT OUT
)
AS
SELECT @TagID = TagID
FROM dbo.Tags
WHERE TagName = @TagName
AND TagValue =@TagValue

IF @TagID IS NULL 
BEGIN
	INSERT INTO dbo.Tags
	(
		TagName,
		TagValue
	)
	VALUES(@TagName,@TagValue)
	SET @TagID = SCOPE_IDENTITY()
END
IF -- tag doesn't exist
	NOT EXISTS(	SELECT 1 
				FROM dbo.InstanceTags 
				WHERE TagID=@TagID 
				AND Instance=@Instance
				)
	-- dbo.InstanceTags apply to Azure DB (server level tags)
	AND EXISTS(SELECT 1 
				FROM dbo.Instances 
				WHERE EngineEdition=5 
				AND Instance=@Instance 
				AND @InstanceID IS NULL
				)
BEGIN
	-- Azure DB server level tags
	INSERT INTO dbo.InstanceTags
	(
		Instance,
		TagID
	)
	VALUES(@Instance,@TagID)
END
IF -- dbo.InstanceIDsTags apply to SQL Server instances and individual Azure DBs
	EXISTS(	SELECT 1 
			FROM dbo.Instances 
			WHERE InstanceID = @InstanceID
			)
	AND NOT EXISTS(
					SELECT 1 
					FROM dbo.InstanceIDsTags 
					WHERE TagID=@TagID 
					AND InstanceID=@InstanceID
					)
BEGIN
	-- InstanceID associated tags - regular instances and individual Azure DBs
	INSERT INTO dbo.InstanceIDsTags(
			InstanceID,
			TagID
			)
	VALUES(@InstanceID,@TagID)
END