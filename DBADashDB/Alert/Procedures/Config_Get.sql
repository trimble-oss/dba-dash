CREATE PROC Alert.Config_Get
AS
SET NOCOUNT ON
SET DATEFIRST 1
EXEC Alert.Rules_Get

SELECT BP.BlackoutPeriodID,
		BP.ApplyToInstanceID,
		CASE WHEN BP.ApplyToInstanceID = -1 THEN '{All}' ELSE I.InstanceDisplayName END AS ApplyToInstance,
		BP.ApplyToTagID,
		T.TagName + ':' + T.TagValue AS ApplyToTag,
		I.ConnectionID,
		BP.AlertKey,
		BP.StartDate,
		BP.EndDate,
		BP.Monday,
		BP.Tuesday,
		BP.Wednesday,
		BP.Thursday,
		BP.Friday,
		BP.Saturday,
		BP.Sunday,
		BP.TimeZone,
		BP.TimeFrom,
		BP.TimeTo,
		CASE WHEN BP.StartDate<=SYSUTCDATETIME() AND BP.EndDate> SYSUTCDATETIME() THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS InEffect,
		CASE	WHEN BP.StartDate>SYSUTCDATETIME() THEN 'Starts in ' + HDStartsIn.HumanDuration
				WHEN BP.EndDate < SYSUTCDATETIME() THEN 'Ended'
				WHEN CHOOSE(DATEPART(dw,TZ.CurrentTime),BP.Monday,BP.Tuesday,BP.Wednesday,BP.Thursday,BP.Friday,BP.Saturday,BP.Sunday) = CAST(0 AS BIT) 
					THEN 'Not scheduled for ' + DATENAME(WEEKDAY,TZ.CurrentTime) + ' (' + BP.TimeZone + ')'
				WHEN (CAST(TZ.CurrentTime AS TIME) < BP.TimeFrom OR CAST(TZ.CurrentTime AS TIME) > BP.TimeTo ) 
					THEN 'Not scheduled for ' + FORMAT(TZ.CurrentTime,'HH:mm') + ' (' + BP.TimeZone + ')'
		WHEN BP.TimeFrom IS NOT NULL OR BP.TimeTo IS NOT NULL 
			THEN CONCAT(
				'Active between ',
				CONVERT(CHAR(5),ISNULL(BP.TimeFrom,'00:00'),114),
				' and ',CONVERT(CHAR(5),ISNULL(BP.TimeTo,'00:00'),114),' (' + BP.TimeZone + ')',
				IIF(DATEDIFF(d,GETUTCDATE(),BP.EndDate)>365,'',', (Ends in ' + HDEndsIn.HumanDuration + ')')
			)
		ELSE 'Active' + IIF(DATEDIFF(d,GETUTCDATE(),BP.EndDate)>365 OR BP.EndDate IS NULL,'',' (Ends in ' + HDEndsIn.HumanDuration + ')')  END AS CurrentStatus,
		HDStartsIn.HumanDuration AS StartsIn,
		HDEndsIn.HumanDuration AS EndsIn,
		BP.Notes
FROM Alert.BlackoutPeriod BP
OUTER APPLY(SELECT SYSDATETIMEOFFSET() AT TIME ZONE TimeZone AS CurrentTime) AS TZ
LEFT JOIN dbo.Instances I ON I.InstanceID = BP.ApplyToInstanceID
LEFT JOIN dbo.Tags T ON T.TagID = BP.ApplyToTagID
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF_BIG(s,GETUTCDATE(),BP.EndDate)) HDEndsIn
OUTER APPLY dbo.SecondsToHumanDuration(DATEDIFF_BIG(s,GETUTCDATE(),BP.StartDate)) HDStartsIn
ORDER BY BP.EndDate DESC

EXEC Alert.NotificationChannel_Get