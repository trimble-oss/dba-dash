CREATE PROC [Report].[LastGoodCheckDB_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
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

SELECT D.InstanceID,
       D.Instance,
       D.Name,
       D.LastGoodCheckDbTime,
       D.HrsSinceLastGoodCheckDB,
       D.DaysSinceLastGoodCheckDB,
       D.Status,
       D.StatusDescription,
       D.ConfiguredLevel,
       D.WarningThresholdHrs,
       D.CriticalThresholdHrs,
	   D.ExcludedFromCheck,
	   D.state_desc,
	   D.is_in_standby
FROM dbo.LastGoodCheckDB D
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = D.InstanceID)
AND D.Status<=@FilterLevel