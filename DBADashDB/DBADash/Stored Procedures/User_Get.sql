CREATE PROC DBADash.User_Get(
	@UserName NVARCHAR(128),
	@UserID INT OUT,
	@ManageGlobalViews BIT OUT,
	@TimeZone VARCHAR(50) OUT,
	@Theme VARCHAR(50)=NULL OUT,
	@AllowMessaging BIT =NULL OUT,
	@AllowPlanForcing BIT =NULL OUT,
	@IsAdmin BIT=NULL OUT,
	@AllowJobExecution BIT=NULL OUT
)
AS
SELECT @ManageGlobalViews = IS_ROLEMEMBER('db_owner') | IS_ROLEMEMBER('ManageGlobalViews')
SELECT @AllowMessaging = ISNULL(HAS_PERMS_BY_NAME('Messaging','SCHEMA','EXECUTE'),0)
SELECT @AllowPlanForcing = IS_ROLEMEMBER('db_owner') | IS_ROLEMEMBER('AllowPlanForcing')
SELECT @AllowJobExecution = IS_ROLEMEMBER('db_owner') | IS_ROLEMEMBER('AllowJobExecution')
SELECT @IsAdmin = IS_ROLEMEMBER('db_owner')

SELECT	@UserID = UserID,
		@TimeZone = TimeZone,
		@Theme = Theme
FROM DBADash.Users 
WHERE UserName = @UserName

IF @@ROWCOUNT=0
BEGIN
	INSERT INTO DBADash.Users(UserName)
	VALUES(@UserName)

	SELECT @UserID = SCOPE_IDENTITY()
END

SELECT name
FROM sys.database_principals
WHERE type = 'R'
AND IS_ROLEMEMBER(name)=1