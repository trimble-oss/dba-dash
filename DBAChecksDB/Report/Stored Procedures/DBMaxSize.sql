CREATE PROC Report.DBMaxSize(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ThresholdPct INT = 20
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT Item
	FROM dbo.SplitStrings(@InstanceIDs,',')
END
SELECT i.Instance,
		d.name AS DB,
		f.filegroup_name,
		SUM(f.size)/128 AS SizeMB,
		SUM(f.space_used)/128.0 AS UsedMB,
		SUM(f.size-f.space_used)/128.0 AS FreeMB,
		1.0-SUM(f.space_used*1.0)/SUM(f.size) AS FreePct,
		SUM(f.max_size) /128 AS MaxSizeMB,
		(SUM(f.max_size)-SUM(f.size))/128.0 AS AvailableGrowthMB,
		1.0-(SUM(f.size)*1.0/NULLIF(SUM(f.max_size),0)) AS AvailableGrowthPct,
		(SUM(f.max_size)-SUM(f.space_used))/128.0 AS AvailableMB,
		1.0-(SUM(f.space_used)*1.0/NULLIF(SUM(f.max_size),0)) AS AvailablePct,
		COUNT(*) Files
FROM dbo.DBFiles f
JOIN dbo.Databases d ON d.DatabaseID = f.DatabaseID
JOIN dbo.Instances i ON i.InstanceID = d.InstanceID
WHERE f.is_read_only=0
AND d.is_read_only=0
AND i.IsActive=1
AND d.IsActive=1
AND f.type=0
GROUP BY i.Instance,
		d.name,
		f.FILEGROUP_NAME
HAVING MIN(f.max_size)>0
AND (1.0-(SUM(f.space_used)*1.0/NULLIF(SUM(f.max_size),0))) *100 < @ThresholdPct
ORDER BY AvailableGrowthPct