CREATE FUNCTION Alert.AlertDelayStatus_Get(
	@IsResolved BIT,
	@ActiveSince DATETIME2, /* ISNULL(ActiveSince,TriggerDate) from Alert.ActiveAlerts */
	@ResolvingSince DATETIME2,
	@RuleID INT
)
RETURNS TABLE
AS
/*
	Is an active alert held back by its rule's delays?  Nothing is sent for it while it is.
	Pending: the issue hasn't persisted for the rule's Notify After delay yet.
	Resolving: the issue has cleared, but not for the rule's Resolve After delay yet.  See Alert.ActiveAlerts_Resolve.
	Used wherever active alerts are reported, so the GUI, notifications and reports agree.
*/
RETURN
SELECT CAST(CASE WHEN @IsResolved = 0
					AND @ResolvingSince IS NULL
					AND @ActiveSince > DATEADD(mi,-N.NotifyAfterMins,SYSUTCDATETIME())
				THEN 1 ELSE 0 END AS BIT) AS IsPending,
	CAST(CASE WHEN @IsResolved = 0 AND @ResolvingSince IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsResolving,
	CAST(CASE WHEN @IsResolved = 0
					AND (@ResolvingSince IS NOT NULL OR @ActiveSince > DATEADD(mi,-N.NotifyAfterMins,SYSUTCDATETIME()))
				THEN 1 ELSE 0 END AS BIT) AS IsHeld,
	N.NotifyAfterMins
FROM (SELECT ISNULL((SELECT R.NotifyAfterMins FROM Alert.Rules R WHERE R.RuleID = @RuleID),0) AS NotifyAfterMins) N
