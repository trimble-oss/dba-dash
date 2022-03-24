CREATE PROC InstanceTags_Add(
	@Instance SYSNAME,
	@InstanceID INT,
	@TagName VARCHAR(50),
	@TagValue VARCHAR(128),
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
	VALUES
	(@TagName,
	@TagValue
		)
	SET @TagID = SCOPE_IDENTITY()
END
IF NOT EXISTS(SELECT 1 FROM dbo.InstanceTags WHERE TagID=@TagID AND Instance=@Instance)
	AND EXISTS(SELECT 1 FROM dbo.Instances WHERE EngineEdition=5 AND Instance=@Instance)
BEGIN
	-- Azure DB tag
	INSERT INTO dbo.InstanceTags
	(
		Instance,
		TagID
	)
	VALUES
	(   @Instance,
		@TagID
		)
END
IF EXISTS(SELECT 1 FROM dbo.Instances WHERE InstanceID = @InstanceID AND EngineEdition<>5)
BEGIN
	-- Tags for other instances associated with InstanceID
	INSERT INTO dbo.InstanceIDsTags(
			InstanceID,
			TagID
			)
	VALUES(@InstanceID,@TagID)
END