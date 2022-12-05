CREATE PROC DBADash.User_TimeZone_Upd(
	@UserID INT,
	@TimeZone VARCHAR(50)
)
AS
UPDATE DBADash.Users
	SET TimeZone = @TimeZone 
WHERE UserID = @UserID