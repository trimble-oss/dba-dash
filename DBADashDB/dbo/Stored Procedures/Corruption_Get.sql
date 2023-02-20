CREATE PROC dbo.Corruption_Get(
	@InstanceIDs dbo.IDs READONLY
)
AS
SELECT I.InstanceGroupName,	
	D.name,
	C.UpdateDate,
	CASE WHEN C.SourceTable = 1 THEN 'msdb.dbo.suspect_pages'
		WHEN C.SourceTable=2 THEN 'sys.dm_db_mirroring_auto_page_repair'
		WHEN C.SourceTable=3 THEN 'sys.dm_hadr_auto_page_repair'
		ELSE NULL END as SourceTable,
	D.LastGoodCheckDbTime,
	DATEADD(mi,I.UTCOffset,C.UpdateDate) AS UpdateDateUtc
FROM dbo.Corruption C 
JOIN dbo.Databases D ON C.DatabaseID = D.DatabaseID
JOIN dbo.Instances I ON D.InstanceID = I.InstanceID
LEFT JOIN dbo.LastGoodCheckDB LG ON LG.DatabaseID = D.DatabaseID
WHERE EXISTS(	
				SELECT 1 
				FROM @InstanceIDs T 
				WHERE T.ID = I.InstanceID
			)
ORDER BY C.UpdateDate DESC