CREATE PROC DBADash.AI_DrivesRisk_Get(
	@MaxRows INT = 200,
	@InstanceFilter NVARCHAR(256) = NULL,
	@HoursBack INT = NULL
)
AS
SELECT TOP (@MaxRows)
	ds.InstanceDisplayName,
	ds.Name AS DriveName,
	ds.Label,
	ds.TotalGB,
	ds.FreeGB,
	ds.PctFreeSpace,
	ds.Status,
	ds.SnapshotAgeMins
FROM dbo.DriveStatus ds
WHERE ds.Status IN (1,2)
  AND (@InstanceFilter IS NULL OR ds.InstanceDisplayName LIKE @InstanceFilter + '%')
ORDER BY ds.Status ASC, ds.PctFreeSpace ASC
