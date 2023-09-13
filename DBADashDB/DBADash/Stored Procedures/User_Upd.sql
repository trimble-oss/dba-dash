CREATE PROC DBADash.User_Upd(
	@UserID INT,
	@TimeZone VARCHAR(50),
	@Theme VARCHAR(50)
)
AS
UPDATE DBADash.Users
	SET TimeZone = @TimeZone,
	Theme = @Theme
WHERE UserID = @UserID