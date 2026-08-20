CREATE PROC DBADash.XETraceTemplate_Del(
	@UserID INT,
	@Name NVARCHAR(128)
)
AS
DELETE DBADash.XETraceTemplate
WHERE UserID = @UserID
AND Name = @Name
