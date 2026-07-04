CREATE PROC dbo.LastGoodCheckDBReport_Get(
	@InstanceIDs IDs READONLY,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@ShowHidden BIT=1
)
AS
;WITH Statuses AS(
	SELECT 1 AS Status WHERE @IncludeCritical=1
	UNION ALL
	SELECT 2 WHERE @IncludeWarning=1
	UNION ALL
	SELECT 3 WHERE @IncludeNA=1
	UNION ALL
	SELECT 4 WHERE @IncludeOK=1
)
SELECT LG.InstanceID,
       LG.DatabaseID,
       LG.Instance,
       LG.InstanceDisplayName,
       LG.name,
       LG.state,
       LG.state_desc,
       LG.is_in_standby,
       LG.LastGoodCheckDbTimeUTC,
       LG.HrsSinceLastGoodCheckDB,
       LG.DaysSinceLastGoodCheckDB,
       LG.Status,
       LG.StatusDescription,
       LG.ConfiguredLevel,
       LG.WarningThresholdHrs,
       LG.CriticalThresholdHrs,
       LG.LastGoodCheckDBExcludedReason,
       LG.create_date_utc
FROM dbo.LastGoodCheckDB LG
WHERE EXISTS(SELECT 1 FROM @InstanceIDs ids WHERE ids.ID = LG.InstanceID)
AND EXISTS(SELECT 1 FROM Statuses s WHERE s.Status = LG.Status)
AND (LG.ShowInSummary = 1 OR @ShowHidden = 1)
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[LastGoodCheckDBReport_Get] TO [Reports]
    AS [dbo];
