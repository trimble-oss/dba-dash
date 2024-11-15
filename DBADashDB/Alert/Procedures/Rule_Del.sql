CREATE PROC Alert.Rule_Del(
	@RuleID INT
)
AS
SET NOCOUNT ON 

DELETE Alert.Rules 
WHERE RuleID = @RuleID