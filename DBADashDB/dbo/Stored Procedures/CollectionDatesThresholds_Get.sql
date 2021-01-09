CREATE PROC dbo.CollectionDatesThresholds_Get(@InstanceID INT,@Reference VARCHAR(30))
AS
SELECT TOP(1) InstanceID,
	   WarningThreshold,
       CriticalThreshold 
FROM dbo.CollectionDatesThresholds
WHERE (InstanceID=@InstanceID OR InstanceID=-1)
AND Reference = @Reference
ORDER BY InstanceID DESC