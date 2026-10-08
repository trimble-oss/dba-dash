CREATE PROC dbo.SQLLoginPasswordAge_Get(
	@InstanceIDs IDs READONLY,
	@Days INT=0, /* Return SQL logins with a password older than this number of days.  0 = ALL */
	@IncludeDisabled BIT=0,
	@Login NVARCHAR(128)=NULL,
	@InstanceDisplayName NVARCHAR(128)=NULL,
	@Top INT=1000 /* Limit detail to the logins with the oldest passwords */
)
AS
/* Base query into temp table */
SELECT	I.InstanceID,
		I.InstanceDisplayName AS Instance,
		SP.name AS Login,
		SP.is_disabled AS [Is Disabled],
		/* Convert from instance local time to UTC.  The GUI converts to the app time zone */
		DATEADD(mi,I.UTCOffset,SP.create_date) AS [Created Date],
		DATEADD(mi,I.UTCOffset,SP.modify_date) AS [Modified Date],
		DATEADD(mi,I.UTCOffset,SP.password_last_set_time) AS [Password Last Set],
		DATEDIFF(d,DATEADD(mi,I.UTCOffset,SP.password_last_set_time),SYSUTCDATETIME()) AS [Password Age (Days)]
INTO #T
FROM dbo.ServerPrincipals SP
JOIN dbo.Instances I ON SP.InstanceID = I.InstanceID
WHERE SP.type = 'S' /* SQL_LOGIN */
AND I.IsActive = 1
AND EXISTS(SELECT 1
		FROM @InstanceIDs T
		WHERE T.ID = I.InstanceID
		)
AND (@Days = 0 OR DATEADD(mi,I.UTCOffset,SP.password_last_set_time) < DATEADD(d,-@Days,SYSUTCDATETIME()))
AND (SP.is_disabled = 0 OR @IncludeDisabled = 1)
AND (SP.name = @Login OR @Login IS NULL)
AND (I.InstanceDisplayName = @InstanceDisplayName OR @InstanceDisplayName IS NULL)
OPTION(RECOMPILE)

/* Summary by instance with count of logins in each password age bucket */
SELECT	InstanceID,
		Instance,
		COUNT(*) AS [SQL Logins],
		MAX([Password Age (Days)]) AS [Max Password Age (Days)],
		SUM(CASE WHEN [Password Age (Days)] <= 1 THEN 1 ELSE 0 END) AS [0-1 Days],
		SUM(CASE WHEN [Password Age (Days)] BETWEEN 2 AND 7 THEN 1 ELSE 0 END) AS [2-7 Days],
		SUM(CASE WHEN [Password Age (Days)] BETWEEN 8 AND 14 THEN 1 ELSE 0 END) AS [8-14 Days],
		SUM(CASE WHEN [Password Age (Days)] BETWEEN 15 AND 30 THEN 1 ELSE 0 END) AS [15-30 Days],
		SUM(CASE WHEN [Password Age (Days)] BETWEEN 31 AND 90 THEN 1 ELSE 0 END) AS [31-90 Days],
		SUM(CASE WHEN [Password Age (Days)] BETWEEN 91 AND 180 THEN 1 ELSE 0 END) AS [91-180 Days],
		SUM(CASE WHEN [Password Age (Days)] BETWEEN 181 AND 365 THEN 1 ELSE 0 END) AS [181-365 Days],
		SUM(CASE WHEN [Password Age (Days)] > 365 THEN 1 ELSE 0 END) AS [>1 Year],
		SUM(CASE WHEN [Password Age (Days)] IS NULL THEN 1 ELSE 0 END) AS [Unknown]
FROM #T
GROUP BY InstanceID, Instance
ORDER BY [Max Password Age (Days)] DESC, Instance

/* Detail - logins with the oldest passwords */
SELECT TOP(@Top) InstanceID,
		Instance,
		Login,
		[Is Disabled],
		[Created Date],
		[Modified Date],
		[Password Last Set],
		[Password Age (Days)]
FROM #T
ORDER BY [Password Age (Days)] DESC, Instance, Login
