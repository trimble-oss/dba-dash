CREATE PROC [dbo].[SysConfigHistory_Get](
	@InstanceIDs VARCHAR(MAX)=NULL,
	@FromDate DATETIME2=NULL,
	@ToDate DATETIME2=NULL
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
END
SELECT I.Instance,
	   I.ConnectionID,
       o.name,
       o.description,
       h.value,
       h.value_in_use,
       h.new_value,
       h.new_value_in_use,
       h.ValidFrom,
       h.ValidTo,
       o.is_dynamic,
       o.is_advanced,
       o.default_value,
       o.minimum,
       o.maximum
FROM dbo.SysConfigHistory h
    JOIN dbo.SysConfigOptions o ON o.configuration_id = h.configuration_id
    JOIN dbo.Instances I ON h.InstanceID = I.InstanceID
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
AND I.IsActive=1
AND (h.ValidTo>=@FromDate OR @FromDate IS NULL)
AND (h.ValidTo<=@ToDate OR @ToDate IS NULL)
ORDER BY h.ValidTo DESC;