CREATE PROC XE.XETraceTemplate_Get(
	@UserID INT
)
AS
SELECT	Name,
		Definition
FROM XE.XETraceTemplate
WHERE UserID = @UserID
ORDER BY Name
