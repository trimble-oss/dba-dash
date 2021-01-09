CREATE PROC InstanceTags_Add(@Instance SYSNAME,@TagName VARCHAR(50),@TagValue VARCHAR(50),@TagID SMALLINT OUT)
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
BEGIN
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