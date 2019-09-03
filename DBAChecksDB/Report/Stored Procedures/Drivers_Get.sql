CREATE PROC [Report].[Drivers_Get](@InstanceIDs VARCHAR(MAX)=NULL,@DriverSearch NVARCHAR(200)=NULL)
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
END;

SELECT I.InstanceID,
       I.Instance,
	   I.SystemManufacturer,
	   I.SystemProductName,
	   I.SQLVersion,
	   I.ProductVersion,
	   I.ProductUpdateReference,
	   I.WindowsCaption + '(' + I.WindowsRelease + ', SKU: ' + I.WindowsSKU + ')' AS Windows,
       D.DeviceName,
       D.DriverProviderName,
       D.DriverVersion,
	   MAX(D.ValidFrom) AS ValidFrom,
	   CASE WHEN EXISTS(SELECT 1  FROM dbo.DriversHistory H WHERE I.InstanceID = H.InstanceID) THEN 1 ELSE 0 END AS HasDriverUpdates,
	   CD.SnapshotDate
FROM dbo.InstanceInfo I
    LEFT JOIN dbo.Drivers D ON D.InstanceID = I.InstanceID
	LEFT JOIN dbo.CollectionDates CD ON I.InstanceID = CD.InstanceID AND CD.Reference='Drivers'
WHERE D.DeviceName IS NOT NULL
AND I.IsActive=1
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND (D.DeviceName LIKE '%' + @DriverSearch + '%' OR D.DriverProviderName LIKE '%' + @DriverSearch + '%'  OR D.DriverVersion LIKE '%' + @DriverSearch + '%'  OR @DriverSearch IS NULL)
GROUP BY I.InstanceID,
         I.Instance,
         D.DeviceName,
         D.DriverProviderName,
         D.DriverVersion,
		 CD.SnapshotDate,
		 I.SystemManufacturer,
	     I.SystemProductName,
		 I.ProductUpdateReference,
		 I.ProductVersion,
		 I.ProductLevel,
		 I.ProductUpdateLevel,
		 I.Edition,
		 I.WindowsSKU,
		 I.WindowsRelease,
		 I.WindowsCaption,
		 I.SQLVersion;