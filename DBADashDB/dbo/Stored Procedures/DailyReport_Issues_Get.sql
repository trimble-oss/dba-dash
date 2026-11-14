CREATE PROC dbo.DailyReport_Issues_Get(
	@InstanceIDs VARCHAR(MAX)
)
AS
/*
	Critical (1) and warning (2) problems for the instances in a daily report, one row per database, drive or job.
	Uses the same views as the summary page, so the email agrees with it.  CheckGroup is the section the report puts the row in.
	Instances not shown in the summary are left out, as on the summary page.
*/
SET NOCOUNT ON
SET DATEFIRST 1

;WITH Scope AS (
	SELECT TRY_CAST(value AS INT) AS InstanceID
	FROM STRING_SPLIT(@InstanceIDs,',')
)
SELECT 'Backups - Full' AS CheckGroup,
	B.FullBackupStatus AS Status,
	B.InstanceDisplayName AS Instance,
	B.name AS ObjectName,
	CASE WHEN B.LastFull IS NULL THEN 'no full backup recorded'
		ELSE 'last full backup ' + (SELECT AgeText FROM dbo.DailyReport_AgeText(B.LastFull,GETUTCDATE())) + ' ago' END AS Detail
FROM dbo.BackupStatus B
JOIN Scope S ON S.InstanceID = B.InstanceID
WHERE B.FullBackupStatus IN (1,2)
AND B.ShowInSummary = 1
UNION ALL
SELECT 'Backups - Diff',
	B.DiffBackupStatus,
	B.InstanceDisplayName,
	B.name,
	CASE WHEN B.LastDiff IS NULL THEN 'no differential backup recorded'
		ELSE 'last differential backup ' + (SELECT AgeText FROM dbo.DailyReport_AgeText(B.LastDiff,GETUTCDATE())) + ' ago' END
FROM dbo.BackupStatus B
JOIN Scope S ON S.InstanceID = B.InstanceID
WHERE B.DiffBackupStatus IN (1,2)
AND B.ShowInSummary = 1
UNION ALL
SELECT 'Backups - Log',
	B.LogBackupStatus,
	B.InstanceDisplayName,
	B.name,
	CASE WHEN B.LastLog IS NULL THEN 'no log backup recorded'
		ELSE 'last log backup ' + (SELECT AgeText FROM dbo.DailyReport_AgeText(B.LastLog,GETUTCDATE())) + ' ago' END
FROM dbo.BackupStatus B
JOIN Scope S ON S.InstanceID = B.InstanceID
WHERE B.LogBackupStatus IN (1,2)
AND B.ShowInSummary = 1
UNION ALL
SELECT 'Drive space',
	D.Status,
	D.InstanceDisplayName,
	D.Name,
	CAST(CAST(D.FreeGB AS DECIMAL(10,1)) AS VARCHAR(20)) + ' GB free of ' + CAST(CAST(D.TotalGB AS DECIMAL(10,1)) AS VARCHAR(20)) + ' GB ('
		+ CAST(CAST(D.PctFreeSpace*100 AS DECIMAL(5,1)) AS VARCHAR(10)) + '% free)'
FROM dbo.DriveStatus D
JOIN Scope S ON S.InstanceID = D.InstanceID
WHERE D.Status IN (1,2)
AND D.ShowInSummary = 1
UNION ALL
SELECT 'Agent jobs',
	J.JobStatus,
	J.InstanceDisplayName,
	J.name,
	CASE WHEN J.LastFailed IS NULL THEN 'failing'
		ELSE 'last failed ' + CONVERT(VARCHAR(16), J.LastFailed, 120) + ' UTC' END
FROM dbo.AgentJobStatus J
JOIN Scope S ON S.InstanceID = J.InstanceID
JOIN dbo.Instances I ON I.InstanceID = J.InstanceID
WHERE J.JobStatus IN (1,2)
AND J.enabled = 1
AND I.ShowInSummary = 1
