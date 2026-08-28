CREATE PROC XE.XETraceTemplate_Del(
	@UserID INT,
	@Name NVARCHAR(128)
)
AS
DELETE XE.XETraceTemplate
WHERE UserID = @UserID
AND Name = @Name
