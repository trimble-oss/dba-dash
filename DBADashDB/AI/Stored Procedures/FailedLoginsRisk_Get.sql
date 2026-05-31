CREATE PROC AI.FailedLoginsRisk_Get(
    @MaxRows INT = 200,
    @InstanceFilter NVARCHAR(256) = NULL,
    @HoursBack INT = 24
)
AS
SET NOCOUNT ON
/* Result set 1: Failed login counts per instance over the window. */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    COUNT(fl.InstanceID) AS FailedLogins,
    MIN(fl.LogDate) AS FirstFailedLogin,
    MAX(fl.LogDate) AS LastFailedLogin
FROM dbo.Instances i
INNER JOIN dbo.FailedLogins fl ON fl.InstanceID = i.InstanceID
    AND fl.LogDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
WHERE i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY i.InstanceDisplayName
ORDER BY FailedLogins DESC

/* Result set 2: Top failing logins and client hosts so the model can identify the offending account/source. */
SELECT TOP (@MaxRows)
    i.InstanceDisplayName,
    SUBSTRING(fl.Text, Calc1.Pos1 + 1, Calc2.Pos2 - Calc1.Pos1 - 1) AS Login,
    RTRIM(REPLACE(REPLACE(SUBSTRING(fl.Text, NULLIF(CHARINDEX('[CLIENT:', fl.Text), 0), 64), CHAR(13), ''), CHAR(10), '')) AS Client,
    COUNT(*) AS FailedLogins,
    MAX(fl.LogDate) AS LastFailedLogin
FROM dbo.FailedLogins fl
INNER JOIN dbo.Instances i ON i.InstanceID = fl.InstanceID
OUTER APPLY (SELECT NULLIF(CHARINDEX('''', fl.Text), 0) AS Pos1) AS Calc1
OUTER APPLY (SELECT NULLIF(CHARINDEX('''', fl.Text, Calc1.Pos1 + 1), 0) AS Pos2) AS Calc2
WHERE fl.LogDate >= DATEADD(HOUR, -@HoursBack, SYSUTCDATETIME())
  AND i.IsActive = 1
  AND (@InstanceFilter IS NULL OR i.InstanceDisplayName LIKE @InstanceFilter + '%')
GROUP BY
    i.InstanceDisplayName,
    SUBSTRING(fl.Text, Calc1.Pos1 + 1, Calc2.Pos2 - Calc1.Pos1 - 1),
    RTRIM(REPLACE(REPLACE(SUBSTRING(fl.Text, NULLIF(CHARINDEX('[CLIENT:', fl.Text), 0), 64), CHAR(13), ''), CHAR(10), ''))
ORDER BY COUNT(*) DESC
