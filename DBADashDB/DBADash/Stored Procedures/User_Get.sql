CREATE PROC DBADash.User_Get(
	@UserName NVARCHAR(128),
	@UserID INT OUT,
	@ManageGlobalViews BIT OUT
)
AS
SELECT @ManageGlobalViews = IS_ROLEMEMBER('db_owner') | IS_ROLEMEMBER('ManageGlobalViews')

SELECT @UserID = UserID
FROM DBADash.Users 
WHERE UserName = @UserName

IF @@ROWCOUNT=0
BEGIN
	INSERT INTO DBADash.Users(UserName)
	VALUES(@UserName)

	SELECT @UserID = SCOPE_IDENTITY()
END