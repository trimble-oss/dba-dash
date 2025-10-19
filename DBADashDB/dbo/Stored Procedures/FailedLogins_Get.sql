CREATE PROC dbo.FailedLogins_Get(
	@InstanceIDs IDs READONLY,
	@InstanceID INT=NULL,
	@Days INT=2
)
AS
IF NOT EXISTS(	SELECT 1 
				FROM dbo.CollectionDates
				WHERE Reference = 'FailedLogins')
BEGIN
	SELECT 'Enable the FailedLogins collection in the service config tool to see data here.' AS Message, 'https://dbadash.com/docs/help/failed-logins/' AS Url
	RETURN
END

SELECT	I.InstanceID,
		I.InstanceDisplayName,
		COUNT(FL.InstanceID) AS FailedLogins,
		MIN(LogDate) AS FirstFailedLogin,
		MAX(LogDate) AS LastFailedLogin,
		CD.HumanSnapshotAge AS SnapshotAge,
		CD.SnapshotDate,
		CD.Status AS SnapshotStatus
FROM dbo.Instances I
LEFT JOIN dbo.FailedLogins FL ON FL.InstanceID = I.InstanceID
								AND FL.LogDate >= DATEADD(d,-@Days,SYSUTCDATETIME())
INNER JOIN dbo.CollectionDatesStatus CD ON CD.InstanceID = I.InstanceID AND CD.Reference = 'FailedLogins'
WHERE I.IsActive=1
AND EXISTS(
			SELECT 1 
			FROM @InstanceIDs T 
			WHERE T.ID = I.InstanceID
			AND (T.ID = @InstanceID OR @InstanceID IS NULL)
			)
GROUP BY	I.InstanceID,
			I.InstanceDisplayName,
			CD.HumanSnapshotAge,
			CD.SnapshotDate,
			CD.Status 
ORDER BY FailedLogins DESC

IF @InstanceID IS NOT NULL
BEGIN
	SELECT	SUBSTRING(Text,Pos1+1,Pos2-Pos1-1) AS Login,
			COUNT(*) [Count]
	FROM dbo.FailedLogins FL
	OUTER APPLY(SELECT NULLIF(CHARINDEX('''',Text),0) AS Pos1) AS Calc1
	OUTER APPLY(SELECT NULLIF(CHARINDEX('''',Text,Pos1+1),0) AS Pos2) AS Calc2
	WHERE FL.LogDate >= DATEADD(d,-@Days,SYSUTCDATETIME())
	AND FL.InstanceID = @InstanceID
	GROUP BY SUBSTRING(Text,Pos1+1,Pos2-Pos1-1)

	SELECT	RTRIM(REPLACE(REPLACE(SUBSTRING(Text,CHARINDEX('[CLIENT:',Text),LEN(Text)),CHAR(13),''),CHAR(10),'')) AS Client,
			COUNT(*) [Count]
	FROM dbo.FailedLogins FL
	WHERE FL.LogDate >= DATEADD(d,-@Days,SYSUTCDATETIME())
	AND FL.InstanceID = @InstanceID
	GROUP BY RTRIM(REPLACE(REPLACE(SUBSTRING(Text,CHARINDEX('[CLIENT:',Text),LEN(Text)),CHAR(13),''),CHAR(10),''))

	SELECT	SUBSTRING(Text,Pos1+1,Pos2-Pos1-1) AS Message,
			COUNT(*) [Count]
	FROM dbo.FailedLogins FL
	OUTER APPLY(SELECT NULLIF(CHARINDEX('.',Text),0) AS Pos1) AS Calc1
	OUTER APPLY(SELECT NULLIF(CHARINDEX('.',Text,Pos1+1),0) AS Pos2) AS Calc2
	WHERE FL.LogDate >= DATEADD(d,-@Days,SYSUTCDATETIME())
	AND FL.InstanceID = @InstanceID
	GROUP BY SUBSTRING(Text,Pos1+1,Pos2-Pos1-1)

	SELECT	LogDate, 
			Text
	FROM dbo.FailedLogins FL
	WHERE FL.LogDate >= DATEADD(d,-@Days,SYSUTCDATETIME())
	AND FL.InstanceID = @InstanceID
	ORDER BY LogDate DESC

END