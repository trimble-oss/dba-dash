CREATE PROC dbo.Drivers_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@DriverSearch NVARCHAR(200)=NULL,
	@Provider NVARCHAR(200)=NULL
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
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;

SELECT I.InstanceID,
       I.Instance,
	   I.ConnectionID,
	   I.InstanceDisplayName,
       D.DeviceName,
       D.DriverProviderName,
       D.DriverVersion,
	   MAX(D.ValidFrom) AS ValidFrom,
	   CD.SnapshotDate
FROM dbo.InstanceInfo I
    LEFT JOIN dbo.Drivers D ON D.InstanceID = I.InstanceID
	LEFT JOIN dbo.CollectionDates CD ON I.InstanceID = CD.InstanceID AND CD.Reference='Drivers'
WHERE D.DeviceName IS NOT NULL
AND I.IsActive=1
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND (D.DeviceName LIKE '%' + @DriverSearch + '%' OR D.DriverProviderName LIKE '%' + @DriverSearch + '%'  OR D.DriverVersion LIKE '%' + @DriverSearch + '%'  OR @DriverSearch IS NULL)
AND (D.DriverProviderName = @Provider OR @Provider IS NULL)
GROUP BY I.InstanceID,
       I.Instance,
	   I.ConnectionID,
	   I.InstanceDisplayName,
       D.DeviceName,
       D.DriverProviderName,
       D.DriverVersion,
	   CD.SnapshotDate