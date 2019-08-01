CREATE PROC [Report].[Drivers_Get](@InstanceIDs VARCHAR(MAX)=NULL,@DriverSearch NVARCHAR(200))
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
	   CASE WHEN I.ProductVersion LIKE '9.%' THEN 'SQL 2005' 
			WHEN I.ProductVersion LIKE '10.0%' THEN 'SQL 2008' 
			WHEN I.ProductVersion LIKE '10.5%' THEN 'SQL 2008 R2'
			WHEN I.ProductVersion LIKE '11.%' THEN 'SQL 2012'
			WHEN I.ProductVersion LIKE '12.%' THEN 'SQL 2014'
			WHEN I.ProductVersion LIKE '13.%' THEN 'SQL 2016'
			WHEN I.ProductVersion LIKE '14.%' THEN 'SQL 2017'
			WHEN I.ProductVersion LIKE '15.%' THEN 'SQL 2019'
			ELSE I.ProductVersion END + ' ' + ISNULL(I.Edition + ' ','') + 
						ISNULL(I.ProductLevel + ' ','') + ISNULL(I.ProductUpdateLevel,'') AS SQLVersion,
	   I.ProductVersion,
	   I.ProductUpdateReference,
	   I.WindowsCaption + '(' + I.WindowsRelease + ', SKU: ' + I.WindowsSKU + ')' AS Windows,
       D.DeviceName,
       D.DriverProviderName,
       D.DriverVersion,
	   MAX(D.ValidFrom) AS ValidFrom,
	   CASE WHEN EXISTS(SELECT 1  FROM dbo.DriversHistory H WHERE I.InstanceID = H.InstanceID) THEN 1 ELSE 0 END AS HasDriverUpdates,
	   CD.SnapshotDate
FROM dbo.Instances I
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
		 I.WindowsCaption;