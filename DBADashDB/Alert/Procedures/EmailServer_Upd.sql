CREATE PROC Alert.EmailServer_Upd(
	@EmailServerID INT,
	@EmailServerName NVARCHAR(100),
	@ServerDetails NVARCHAR(MAX)
)
AS
UPDATE Alert.EmailServer
SET EmailServerName = @EmailServerName,
	ServerDetails = @ServerDetails
WHERE EmailServerID = @EmailServerID
