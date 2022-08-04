CREATE PROC DBADash.SavedViews_Upd(
	@UserID INT,
	@Name NVARCHAR(50),
	@ViewType TINYINT,
	@SavedObject NVARCHAR(MAX)
)
AS
IF EXISTS(	SELECT 1 
			FROM DBADash.SavedViews
			WHERE UserID = @UserID 
			AND Name = @Name 
			AND ViewType = @ViewType
		)
BEGIN
	UPDATE DBADash.SavedViews
		SET SavedObject = @SavedObject,
		ModifiedDate = GETUTCDATE()
	WHERE UserID = @UserID
	AND ViewType = @ViewType
	AND Name = @Name
END
ELSE
BEGIN
	INSERT INTO DBADash.SavedViews(
			UserID,
			Name,
			ViewType,
			SavedObject
	)
	VALUES(	@UserID,
			@Name,
			@ViewType,
			@SavedObject
			)
END