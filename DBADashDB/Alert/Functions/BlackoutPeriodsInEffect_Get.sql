CREATE FUNCTION Alert.BlackoutPeriodsInEffect_Get()
RETURNS TABLE
AS
/*
	Blackout periods in effect right now: within the start/end dates, scheduled for the current day of the week and time of day in the blackout's time zone.
	The single definition of "in effect" for Alert.IsBlackoutPeriod, Alert.ApplicableInstances_Upd and Alert.Alerts_Upd.
	Day of week is independent of SET DATEFIRST (Monday = 1).
*/
RETURN
SELECT BP.BlackoutPeriodID,
	BP.ApplyToInstanceID,
	BP.ApplyToTagID,
	BP.ApplyToTagIDs,
	BP.AlertKey
FROM Alert.BlackoutPeriod BP
OUTER APPLY(SELECT SYSDATETIMEOFFSET() AT TIME ZONE BP.TimeZone AS CurrentTime) AS TZ
OUTER APPLY(SELECT CAST(TZ.CurrentTime AS TIME) AS CurrentTimeAsTime,
				(DATEPART(dw,TZ.CurrentTime) + @@DATEFIRST - 2) % 7 + 1 AS MondayBasedDay
			) AS T
WHERE (BP.StartDate < SYSUTCDATETIME() OR BP.StartDate IS NULL)
AND (BP.EndDate > SYSUTCDATETIME() OR BP.EndDate IS NULL)
AND CHOOSE(T.MondayBasedDay,BP.Monday,BP.Tuesday,BP.Wednesday,BP.Thursday,BP.Friday,BP.Saturday,BP.Sunday) = CAST(1 AS BIT)
AND (
		(T.CurrentTimeAsTime >= BP.TimeFrom OR BP.TimeFrom IS NULL)
		AND (T.CurrentTimeAsTime <= BP.TimeTo OR BP.TimeTo IS NULL)
		OR (
			/* Configured TimeTo is less than TimeFrom, crossing midnight boundary. e.g. 22:00 to 03:00.
			   Either >= TimeFrom or <= TimeTo is OK when midnight boundary is crossed */
			BP.TimeTo < BP.TimeFrom
			AND (T.CurrentTimeAsTime >= BP.TimeFrom OR T.CurrentTimeAsTime <= BP.TimeTo)
		)
	)
