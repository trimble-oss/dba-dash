CREATE PROC dbo.InstanceTags_Del(
	@Instance SYSNAME,
	@InstanceID INT,
	@TagName VARCHAR(50),
	@TagValue VARCHAR(50)
)
AS
DECLARE @TagID INT 

SELECT @TagID = TagID
FROM dbo.Tags
WHERE TagName = @TagName
AND TagValue = @TagValue

IF @TagID IS NOT NULL
BEGIN 
	DELETE dbo.InstanceTags 
	WHERE Instance =@Instance 
	AND TagID = @TagID

	DELETE dbo.InstanceIDsTags
	WHERE InstanceID = @InstanceID
	AND TagID = @TagID
	
	IF NOT EXISTS(SELECT 1 
					FROM dbo.InstanceTags 
					WHERE TagID	= @TagID
					UNION ALL 
					SELECT 1 
					FROM dbo.InstanceIDsTags
					WHERE @TagID = @TagID
					)
	BEGIN
		DELETE dbo.Tags
		WHERE TagID = @TagID
	END
END