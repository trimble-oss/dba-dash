CREATE PROC dbo.CollectionDatesThresholds_Get(
	@InstanceID INT,
	@Reference VARCHAR(30)=NULL
)
AS
-- Driven from dbo.CollectionDates (every reference actually collected in this scope) rather than
-- dbo.CollectionDatesThresholds, so references with no threshold row yet still appear in the list
-- (unconfigured, using the schedule-based default) and can be selected/saved.
SELECT @InstanceID AS InstanceID,
       R.Reference,
       T.WarningThreshold,
       T.CriticalThreshold,
       T.WarningMultiplier,
       T.CriticalMultiplier,
       T.WarningBufferMinutes,
       T.CriticalBufferMinutes,
       ISNULL(T.Disabled,0) AS Disabled,
       -- "Inherited" (shown as the Inherit/Default radio) whenever there's no local override at this
       -- exact level: either nothing configured anywhere (T.InstanceID IS NULL) or the resolved value
       -- came from a different level (root, or the built-in system default).
       CAST(CASE WHEN T.InstanceID IS NULL OR T.InstanceID <> @InstanceID THEN 1 ELSE 0 END AS BIT) AS Inherited
FROM (
	SELECT DISTINCT Reference
	FROM dbo.CollectionDates
	WHERE (@InstanceID = -1 OR InstanceID = @InstanceID)
) R
OUTER APPLY (
	SELECT TOP(1) CDT.WarningThreshold,
		CDT.CriticalThreshold,
		CDT.WarningMultiplier,
		CDT.CriticalMultiplier,
		CDT.WarningBufferMinutes,
		CDT.CriticalBufferMinutes,
		CDT.Disabled,
		CDT.InstanceID
	FROM dbo.CollectionDatesThresholds CDT
	WHERE (CDT.InstanceID = @InstanceID OR CDT.InstanceID = -1)
	AND CDT.Reference = R.Reference
	ORDER BY CDT.InstanceID DESC
) T
WHERE (R.Reference = @Reference OR @Reference IS NULL)
ORDER BY R.Reference
