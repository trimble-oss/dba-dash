CREATE PROC dbo.CollectionDatesThresholds_Get(
	@InstanceID INT,
	@Reference VARCHAR(30)=NULL
)
AS
SELECT InstanceID,
        Reference,
        WarningThreshold,
        CriticalThreshold,
		CAST(0 AS BIT) AS Inherited
FROM dbo.CollectionDatesThresholds
WHERE InstanceID=@InstanceID
AND (Reference = @Reference OR @Reference IS NULL)
UNION ALL
SELECT InstanceID,
        Reference,
        WarningThreshold,
        CriticalThreshold,
		CAST(1 AS BIT) AS Inherited
FROM dbo.CollectionDatesThresholds rootT
WHERE InstanceID=-1
AND (Reference = @Reference OR @Reference IS NULL)
AND NOT EXISTS(SELECT 1 
			FROM dbo.CollectionDatesThresholds instanceT
			WHERE instanceT.InstanceID = @InstanceID
			AND instanceT.Reference = rootT.Reference)
ORDER BY Reference