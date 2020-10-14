CREATE PROC [dbo].[Drives_Get](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@IncludeCritical BIT=1,
	@IncludeWarning BIT=1,
	@IncludeNA BIT=0,
	@IncludeOK BIT=0,
	@IncludeMetrics BIT=0
)
AS
DECLARE @StatusSQL NVARCHAR(MAX)

SELECT @StatusSQL = CASE WHEN @IncludeCritical=1 THEN ',1' ELSE '' END
	+ CASE WHEN @IncludeWarning=1 THEN ',2' ELSE '' END
	+ CASE WHEN @IncludeNA=1 THEN ',3' ELSE '' END
	+ CASE WHEN @IncludeOK=1 THEN ',4' ELSE '' END

SELECT @StatusSQL = CASE WHEN @StatusSQL='' THEN 'AND 1=2'
		ELSE 'AND D.Status IN(' + STUFF(@StatusSQL,1,1,'') + ')' END

DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT D.DriveID,
		D.Name,
		D.InstanceID,
		D.Label,
		D.TotalGB,
		D.FreeGB,
		D.DriveCheckType,
		D.Status,
		D.DriveWarningThreshold,
		D.DriveCriticalThreshold,
		D.IsInheritedThreshold,
		D.Instance,
		D.PctFreeSpace,
		D.DriveCheckType,
		D.DriveCheckConfiguredLevel,
		D.SnapshotDate,
		D.SnapshotAgeMins,
		D.SnapshotStatus' + CASE WHEN  @IncludeMetrics=1 THEN ',
		(D.UsedGB-PreviousDay.UsedGB) AS ChangeUsedGB24Hrs,
		(D.TotalGB-PreviousDay.TotalGB) AS ChangeDriveSize24Hrs,
		(D.UsedGB-SevenDay.UsedGB) AS ChangeUsedGB7Days,
		(D.TotalGB-SevenDay.TotalGB) AS ChangeDriveSize7Days,
		(D.UsedGB-ThirtyDay.UsedGB) ChangeUsedGB30Days,
		(D.TotalGB-ThirtyDay.TotalGB) AS ChangeDriveSize30Days,
		(D.UsedGB-NinetyDay.UsedGB) AS ChangeUsedGB90Days,
		(D.TotalGB-NinetyDay.TotalGB) AS ChangeDriveSize90Days' ELSE '' END + '
FROM dbo.DriveStatus D ' + CASE WHEN @IncludeMetrics=1 THEN '
OUTER APPLY (SELECT TOP(1) DSS.Capacity / POWER(1024.0,3) as TotalGB,
                    DSS.UsedSpace / POWER(1024.0,3)as UsedGB
		FROM dbo.DriveSnapshot DSS 
		WHERE DSS.SnapshotDate < DATEADD(HH,-24,GETUTCDATE())
		AND DSS.DriveID=D.DriveID
		ORDER BY DSS.SnapshotDate DESC
		) PreviousDay
OUTER APPLY (SELECT TOP(1) DSS.Capacity / POWER(1024.0,3) as TotalGB,
                    DSS.UsedSpace / POWER(1024.0,3) as UsedGB
		FROM dbo.DriveSnapshot DSS 
		WHERE DSS.SnapshotDate >= DATEADD(d,-7,GETUTCDATE())
		AND DSS.DriveID=D.DriveID
		ORDER BY DSS.SnapshotDate
		) SevenDay
OUTER APPLY (SELECT TOP(1) DSS.Capacity / POWER(1024.0,3) as TotalGB,
                    DSS.UsedSpace / POWER(1024.0,3)as UsedGB
		FROM dbo.DriveSnapshot DSS 
		WHERE DSS.SnapshotDate >= DATEADD(d,-30,GETUTCDATE())
		AND DSS.DriveID=D.DriveID
		ORDER BY DSS.SnapshotDate
		) ThirtyDay
OUTER APPLY (SELECT TOP(1) DSS.Capacity / POWER(1024.0,3) as TotalGB,
                    DSS.UsedSpace / POWER(1024.0,3)as UsedGB
		FROM dbo.DriveSnapshot DSS 
		WHERE DSS.SnapshotDate >= DATEADD(d,-90,GETUTCDATE())
		AND DSS.DriveID=D.DriveID
		ORDER BY DSS.SnapshotDate
		) NinetyDay' ELSE '' END + '
WHERE ' + CASE WHEN @InstanceIDs IS NULL OR @InstanceIDs = '' 
			THEN '1=1' 
			ELSE 'EXISTS(SELECT 1 
						FROM STRING_SPLIT(@InstanceIDs,'','') ss
						WHERE ss.value = D.InstanceID)' END + '
' + @StatusSQL + '
ORDER BY Status DESC, PctFreeSpace DESC;'

EXEC sp_executesql @SQL,N'@InstanceIDs VARCHAR(MAX)',@InstanceIDs
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Drives_Get] TO [Reports]
    AS [dbo];

