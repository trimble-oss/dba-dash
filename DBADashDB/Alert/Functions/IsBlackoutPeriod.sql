CREATE FUNCTION Alert.IsBlackoutPeriod(
	@InstanceID INT=-1,
	@AlertKey NVARCHAR(128)='%'
)
RETURNS TABLE
AS
RETURN
SELECT CASE WHEN EXISTS(
				SELECT 1 
				FROM Alert.BlackoutPeriod BP 
				OUTER APPLY(SELECT SYSDATETIMEOFFSET() AT TIME ZONE TimeZone AS CurrentTime) AS TZ
				OUTER APPLY(SELECT CAST(TZ.CurrentTime AS TIME) AS CurrentTimeAsTime) AS T
				WHERE BP.ApplyToInstanceID = -1
				AND  (BP.StartDate < SYSUTCDATETIME() OR BP.StartDate IS NULL)
				AND (BP.EndDate > SYSUTCDATETIME() OR BP.EndDate IS NULL)
				AND @AlertKey LIKE BP.AlertKey
				AND CHOOSE(DATEPART(dw,TZ.CurrentTime),BP.Monday,BP.Tuesday,BP.Wednesday,BP.Thursday,BP.Friday,BP.Saturday,BP.Sunday) = CAST(1 AS BIT)
				AND (
						(T.CurrentTimeAsTime >= BP.TimeFrom OR BP.TimeFrom IS NULL) 
						AND (T.CurrentTimeAsTime <= BP.TimeTo OR BP.TimeTo IS NULL)
						OR (
							/* Configured TimeTo is less than TimeFrom, crossing midnight boundary. e.g. 22:00 to 03:00. */
							BP.TimeTo<BP.TimeFrom
							/* Either >= TimeFrom or <= TimeTo is OK when midnight boundary is crossed*/
							AND (T.CurrentTimeAsTime >= BP.TimeFrom OR T.CurrentTimeAsTime <= BP.TimeTo )
						) 
					)		
				AND EXISTS(
							/* Apply to all tags/instances */
							SELECT 1
							WHERE BP.ApplyToTagID = -1
							AND BP.ApplyToInstanceID = -1
							UNION ALL
							/* Apply to specific instance */
							SELECT 1 
							WHERE BP.ApplyToInstanceID = @InstanceID
							AND BP.ApplyToTagID =-1
							UNION ALL
							/* Apply to tag */
							SELECT 1 
							FROM dbo.InstanceIDsTags IT
							WHERE IT.InstanceID = @InstanceID
							AND IT.TagID = BP.ApplyToTagID 
							AND BP.ApplyToInstanceID = -1
							UNION ALL
							/* Apply to tag (Azure instance) */
							SELECT 1 
							FROM dbo.InstanceTags IT
							WHERE IT.Instance = I.Instance
							AND IT.TagID = BP.ApplyToTagID
							AND BP.ApplyToInstanceID = -1
					)
				UNION ALL 
				SELECT 1 
				FROM Alert.BlackoutPeriod BP 
				OUTER APPLY(SELECT SYSDATETIMEOFFSET() AT TIME ZONE TimeZone AS CurrentTime) AS TZ
				OUTER APPLY(SELECT CAST(TZ.CurrentTime AS TIME) AS CurrentTimeAsTime) AS T
				WHERE BP.ApplyToInstanceID = @InstanceID
				AND BP.ApplyToTagID = -1
				AND  (BP.StartDate < SYSUTCDATETIME() OR BP.StartDate IS NULL)
				AND (BP.EndDate > SYSUTCDATETIME() OR BP.EndDate IS NULL)
				AND @AlertKey LIKE BP.AlertKey
				AND CHOOSE(DATEPART(dw,TZ.CurrentTime),BP.Monday,BP.Tuesday,BP.Wednesday,BP.Thursday,BP.Friday,BP.Saturday,BP.Sunday) = CAST(1 AS BIT)
				AND (
						(T.CurrentTimeAsTime >= BP.TimeFrom OR BP.TimeFrom IS NULL) 
						AND (T.CurrentTimeAsTime <= BP.TimeTo OR BP.TimeTo IS NULL)
						OR (
							/* Configured TimeTo is less than TimeFrom, crossing midnight boundary. e.g. 22:00 to 03:00. */
							BP.TimeTo<BP.TimeFrom
							/* Either >= TimeFrom or <= TimeTo is OK when midnight boundary is crossed*/
							AND (T.CurrentTimeAsTime >= BP.TimeFrom OR T.CurrentTimeAsTime <= BP.TimeTo )
						) 
					)
			) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsBlackout
FROM dbo.Instances I
WHERE InstanceID = @InstanceID
