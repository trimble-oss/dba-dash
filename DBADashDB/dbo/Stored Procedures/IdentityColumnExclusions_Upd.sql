CREATE PROC dbo.IdentityColumnExclusions_Upd(
	@InstanceID INT, /* -1 for root */
	@DatabaseID INT, /* -1 for root or instance level */
	@Exclusions dbo.IdentityColumnExclusions READONLY
)
AS
/*
	Replaces the identity column table exclusions for the root, instance or database
*/
SET NOCOUNT ON
SET XACT_ABORT ON

BEGIN TRAN

DELETE X
FROM dbo.IdentityColumnExclusions X
WHERE X.InstanceID = @InstanceID
AND X.DatabaseID = @DatabaseID
AND NOT EXISTS(SELECT 1 FROM @Exclusions E WHERE E.ObjectName = X.ObjectName)

INSERT INTO dbo.IdentityColumnExclusions(InstanceID,DatabaseID,ObjectName)
SELECT @InstanceID,@DatabaseID,E.ObjectName
FROM @Exclusions E
WHERE E.ObjectName <> ''
AND NOT EXISTS(
		SELECT 1
		FROM dbo.IdentityColumnExclusions X
		WHERE X.InstanceID = @InstanceID
		AND X.DatabaseID = @DatabaseID
		AND X.ObjectName = E.ObjectName
		)

COMMIT
