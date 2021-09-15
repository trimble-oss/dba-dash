CREATE PROC [Report].[Logshipping_Get](@InstanceIDs VARCHAR(MAX)=NULL,@FilterLevel TINYINT=2)
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

SELECT InstanceID,
	   DatabaseID,
       Instance,
       name,
       restore_date,
       backup_start_date,
       TimeSinceLast,
       LatencyOfLast,
       TotalTimeBehind,
       SnapshotAge,
       Status,
       StatusDescription,
	   f.FileName AS last_file
FROM dbo.LogShippingStatus LSS
CROSS APPLY dbo.ParseFileName(LSS.last_file) f
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = LSS.InstanceID)
AND (Status<=@FilterLevel)
ORDER BY Status,TotalTimeBehind DESC
OPTION(RECOMPILE)