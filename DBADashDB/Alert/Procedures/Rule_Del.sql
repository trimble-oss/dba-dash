CREATE PROC Alert.Rule_Del(
	@RuleID INT
)
AS
SET NOCOUNT ON 
SET XACT_ABORT ON

DECLARE @AlertIDs BigIDs

BEGIN TRAN

INSERT INTO @AlertIDs(ID)
SELECT AlertID 
FROM Alert.ActiveAlerts
WHERE RuleID = @RuleID

IF @@ROWCOUNT>0 /* Close any existing alerts that reference the rule */
BEGIN
	EXEC Alert.ClosedAlerts_Add @AlertIDs=@AlertIDs
END

DELETE Alert.Rules 
WHERE RuleID = @RuleID

COMMIT