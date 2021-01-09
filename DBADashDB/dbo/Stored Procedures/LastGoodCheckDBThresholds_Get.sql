CREATE PROC dbo.LastGoodCheckDBThresholds_Get(
	@InstanceID INT,
	@DatabaseID INT
)
AS
SELECT  WarningThresholdHrs,
       CriticalThresholdHrs 
FROM  dbo.LastGoodCheckDBThresholds
WHERE InstanceID = @InstanceID
AND DatabaseID = @DatabaseID