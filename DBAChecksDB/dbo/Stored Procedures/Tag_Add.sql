CREATE PROC Tag_Add(@InstanceID INT,@Tag NVARCHAR(50))
AS
DECLARE @TagID SMALLINT 
SELECT @TagID = TagID
FROM dbo.Tags
WHERE Tag = @Tag
IF @TagID IS NULL
BEGIN
	INSERT INTO dbo.Tags
	(
	    Tag
	)
	VALUES
	(@Tag
	    )
	SET @TagID = SCOPE_IDENTITY()
END
IF NOT EXISTS(SELECT 1 FROM dbo.InstanceTag WHERE TagID=@TagID AND InstanceID=@InstanceID)
BEGIN
	INSERT INTO dbo.InstanceTag
	(
	    InstanceID,
	    TagID
	)
	VALUES
	(   @InstanceID,
	    @TagID
	    )
END