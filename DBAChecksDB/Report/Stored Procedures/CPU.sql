CREATE PROC Report.CPU(@InstanceIDs VARCHAR(MAX)=NULL,@Mins INT=60)
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

SELECT I.Instance, CPU.InstanceID,
                  CPU.EventTime,
                  CPU.SQLProcessCPU,
                  CPU.SystemIdleCPU 
FROM dbo.CPU
JOIN dbo.Instances I ON I.InstanceID= CPU.InstanceID 
WHERE I.IsActive=1
AND CPU.EventTime >= DATEADD(mi,-@Mins,GETUTCDATE())
AND EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
OPTION(RECOMPILE)