CREATE PROC dbo.LogShippingExclusions_Upd(
	@InstanceID INT, /* -1 for root */
	@Exclusions dbo.LogShippingExclusions READONLY
)
AS
/*
	Replaces the log shipping database exclusions for the instance (or root)
*/
SET NOCOUNT ON
SET XACT_ABORT ON

BEGIN TRAN

DELETE X
FROM dbo.LogShippingExclusions X
WHERE X.InstanceID = @InstanceID
AND NOT EXISTS(SELECT 1 FROM @Exclusions E WHERE E.DatabaseName = X.DatabaseName)

INSERT INTO dbo.LogShippingExclusions(InstanceID,DatabaseName)
SELECT @InstanceID,E.DatabaseName
FROM @Exclusions E
WHERE E.DatabaseName <> ''
AND NOT EXISTS(
		SELECT 1
		FROM dbo.LogShippingExclusions X
		WHERE X.InstanceID = @InstanceID
		AND X.DatabaseName = E.DatabaseName
		)

COMMIT
