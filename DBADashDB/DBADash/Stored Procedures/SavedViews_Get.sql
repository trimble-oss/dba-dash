CREATE PROC DBADash.SavedViews_Get(
	@UserID INT,
	@ViewType TINYINT
)
AS
SELECT	Name,
		SavedObject 
FROM DBADash.SavedViews
WHERE UserID = @UserID
AND ViewType=@ViewType 
ORDER BY Name