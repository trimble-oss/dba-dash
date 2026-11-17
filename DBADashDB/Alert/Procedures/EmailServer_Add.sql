CREATE PROC Alert.EmailServer_Add(
	@EmailServerName NVARCHAR(100),
	@ServerDetails NVARCHAR(MAX),
	@EmailServerID INT OUT
)
AS
INSERT INTO Alert.EmailServer(EmailServerName, ServerDetails)
VALUES(@EmailServerName, @ServerDetails)

SET @EmailServerID = SCOPE_IDENTITY()
