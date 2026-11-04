CREATE PROC dbo.BusyPeriods_Get(
	@InstanceID INT,
	@FromDate DATETIME2,
	@ToDate DATETIME2
)
AS
SET NOCOUNT ON
/* Busy periods that apply to an instance, expanded to UTC intervals that overlap the date range.
   The most specific level that has any periods is used: the instance's own periods, otherwise the periods for its tags, otherwise the periods for all instances.
   Levels aren't merged (unlike Alert.IsBlackoutPeriod), so one instance can be the exception to its tag.
   Each day is expanded in the period's own time zone, so daylight saving changes move the UTC times correctly. */
DECLARE @InstanceTags TABLE(TagID INT NOT NULL PRIMARY KEY);

INSERT INTO @InstanceTags(TagID)
SELECT IT.TagID
FROM dbo.InstanceIDsTags IT
WHERE IT.InstanceID = @InstanceID
UNION
/* Azure instances are tagged by name rather than by InstanceID */
SELECT IT.TagID
FROM dbo.InstanceTags IT
INNER JOIN dbo.Instances I ON I.Instance = IT.Instance
WHERE I.InstanceID = @InstanceID;

DECLARE @Level TINYINT = CASE WHEN EXISTS(SELECT 1 FROM dbo.BusyPeriod BP WHERE BP.ApplyToInstanceID = @InstanceID) THEN 1
							WHEN EXISTS(SELECT 1 FROM dbo.BusyPeriod BP INNER JOIN @InstanceTags T ON T.TagID = BP.ApplyToTagID) THEN 2
							ELSE 3
						END;

;WITH Applicable AS(
	SELECT BP.BusyPeriodID, BP.Monday, BP.Tuesday, BP.Wednesday, BP.Thursday, BP.Friday, BP.Saturday, BP.Sunday, BP.TimeFrom, BP.TimeTo, BP.TimeZone, BP.Description, CAST(1 AS TINYINT) AS SourceLevel
	FROM dbo.BusyPeriod BP
	WHERE @Level = 1
	AND BP.ApplyToInstanceID = @InstanceID
	UNION ALL
	SELECT BP.BusyPeriodID, BP.Monday, BP.Tuesday, BP.Wednesday, BP.Thursday, BP.Friday, BP.Saturday, BP.Sunday, BP.TimeFrom, BP.TimeTo, BP.TimeZone, BP.Description, CAST(2 AS TINYINT)
	FROM dbo.BusyPeriod BP
	WHERE @Level = 2
	AND BP.ApplyToTagID IN (SELECT TagID FROM @InstanceTags)
	UNION ALL
	SELECT BP.BusyPeriodID, BP.Monday, BP.Tuesday, BP.Wednesday, BP.Thursday, BP.Friday, BP.Saturday, BP.Sunday, BP.TimeFrom, BP.TimeTo, BP.TimeZone, BP.Description, CAST(3 AS TINYINT)
	FROM dbo.BusyPeriod BP
	WHERE @Level = 3
	AND BP.ApplyToInstanceID = -1
	AND BP.ApplyToTagID = -1
),
/* One row per calendar date, from the day before the range to the day after it, so periods that start or end across the range edges are kept */
Days AS(
	SELECT CAST(DATEADD(DAY, -1, CAST(@FromDate AS DATE)) AS DATE) AS BusyDate
	UNION ALL
	SELECT DATEADD(DAY, 1, BusyDate)
	FROM Days
	WHERE BusyDate < DATEADD(DAY, 1, CAST(@ToDate AS DATE))
),
/* Local start and end of each period on each day. The day is the weekday the period starts on, so a period crossing midnight belongs to its start day. */
Expanded AS(
	SELECT A.BusyPeriodID, A.SourceLevel, A.Description, A.TimeZone,
		DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00' AS TIME), A.TimeFrom), CAST(D.BusyDate AS DATETIME2)) AS StartLocal,
		CASE WHEN A.TimeTo > A.TimeFrom
			THEN DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00' AS TIME), A.TimeTo), CAST(D.BusyDate AS DATETIME2))
			ELSE DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00' AS TIME), A.TimeTo), CAST(DATEADD(DAY, 1, D.BusyDate) AS DATETIME2))
		END AS EndLocal
	FROM Applicable A
	CROSS JOIN Days D
	/* Weekday number 1 (Monday) to 7 (Sunday), whatever @@DATEFIRST is */
	WHERE CHOOSE(((DATEPART(WEEKDAY, D.BusyDate) + @@DATEFIRST - 2) % 7) + 1, A.Monday, A.Tuesday, A.Wednesday, A.Thursday, A.Friday, A.Saturday, A.Sunday) = CAST(1 AS BIT)
),
Converted AS(
	SELECT E.BusyPeriodID, E.SourceLevel, E.Description, E.TimeZone,
		CAST(CAST(E.StartLocal AS DATETIME2) AT TIME ZONE E.TimeZone AT TIME ZONE 'UTC' AS DATETIME2) AS StartUtc,
		CAST(CAST(E.EndLocal AS DATETIME2) AT TIME ZONE E.TimeZone AT TIME ZONE 'UTC' AS DATETIME2) AS EndUtc
	FROM Expanded E
)
SELECT C.BusyPeriodID,
	CHOOSE(C.SourceLevel, 'Instance', 'Tag', 'All') AS Source,
	C.Description,
	C.TimeZone,
	C.StartUtc,
	C.EndUtc
FROM Converted C
WHERE C.StartUtc < @ToDate
AND C.EndUtc > @FromDate
ORDER BY C.StartUtc
OPTION (MAXRECURSION 0);

/* Every configured period, for the editor.  Relevant: it's the instance's own, for one of its tags or for all instances (Source says which).
   Applies: it's at the level the instance uses - periods at a less specific level are overridden.  Periods for other instances or tags are
   listed so they can be seen and edited from here too. */
SELECT BP.BusyPeriodID,
	BP.ApplyToInstanceID,
	BP.ApplyToTagID,
	T.TagName,
	T.TagValue,
	I.InstanceDisplayName,
	CHOOSE(L.SourceLevel, 'Instance', 'Tag', 'All') AS Source,
	CAST(CASE WHEN L.SourceLevel = @Level THEN 1 ELSE 0 END AS BIT) AS Applies,
	CAST(CASE WHEN L.SourceLevel IS NULL THEN 0 ELSE 1 END AS BIT) AS Relevant,
	BP.Monday, BP.Tuesday, BP.Wednesday, BP.Thursday, BP.Friday, BP.Saturday, BP.Sunday,
	BP.TimeFrom,
	BP.TimeTo,
	BP.TimeZone,
	BP.Description
FROM dbo.BusyPeriod BP
CROSS APPLY(SELECT CASE WHEN BP.ApplyToInstanceID = @InstanceID THEN 1
						WHEN BP.ApplyToTagID IN (SELECT TagID FROM @InstanceTags) THEN 2
						WHEN BP.ApplyToInstanceID = -1 AND BP.ApplyToTagID = -1 THEN 3
					END AS SourceLevel) L
LEFT JOIN dbo.Tags T ON T.TagID = BP.ApplyToTagID
LEFT JOIN dbo.Instances I ON I.InstanceID = BP.ApplyToInstanceID
ORDER BY CASE WHEN L.SourceLevel IS NULL THEN 4 ELSE L.SourceLevel END, BP.TimeFrom, BP.BusyPeriodID;
