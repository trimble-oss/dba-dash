CREATE PROC DBADash.SavedViews_Del(
	@UserID INT,
	@ViewType TINYINT,
	@Name NVARCHAR(50)
)
AS
DELETE DBADash.SavedViews
WHERE UserID = @UserID
AND Name = @Name
AND ViewType = @ViewType