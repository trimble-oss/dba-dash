CREATE PROC DBADash.XETraceTemplate_Get(
	@UserID INT
)
AS
SELECT	Name,
		Definition
FROM DBADash.XETraceTemplate
WHERE UserID = @UserID
ORDER BY Name
