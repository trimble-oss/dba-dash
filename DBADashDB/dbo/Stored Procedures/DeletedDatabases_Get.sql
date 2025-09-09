CREATE PROC dbo.DeletedDatabases_Get(
	@InstanceIDs IDs READONLY,
	@Days INT=7
)
AS
SELECT	I.InstanceGroupName,
		D.name AS DB,
		DATEADD(mi,I.UTCOffset,D.create_date) AS CreatedDate,
		D.UpdatedDate AS DeletedDate,
		(SELECT TOP(1) SUM(SS.Size)/128.0
		FROM dbo.DBFiles F 
		/* Get latest snapshot for each file */
		OUTER APPLY(SELECT TOP(1)	SS.Size,
									SS.SnapshotDate
					FROM dbo.DBFileSnapshot SS
					WHERE SS.FileID = F.FileID
					ORDER BY SS.SnapshotDate DESC
					)	SS		
		WHERE F.DatabaseID = D.DatabaseID
		GROUP BY SS.SnapshotDate /* Group and SUM by SnapshotDate */
		ORDER BY SS.SnapshotDate DESC /* Only return sum of files included in the last snapshot */
		) SizeMB
FROM dbo.Databases D 
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
WHERE D.IsActive = 0
AND EXISTS(SELECT 1
			FROM @InstanceIDs T
			WHERE T.ID = I.InstanceID
			)
AND D.UpdatedDate >= DATEADD(d,-@Days,SYSUTCDATETIME())
ORDER BY DeletedDate DESC
