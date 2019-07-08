CREATE PROC LogshippingReport_Get(@InstanceID INT=NULL,@FilterLevel TINYINT=2)
AS
SELECT * 
FROM dbo.LogShippingStatus
WHERE (Status<=@FilterLevel)
AND (InstanceID = @InstanceID OR @InstanceID IS NULL)
ORDER BY Status,TotalTimeBehind DESC
OPTION(RECOMPILE)






