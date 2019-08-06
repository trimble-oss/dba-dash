CREATE PROC Report.SQLPatching_Get(@InstanceIDs VARCHAR(MAX)=NULL)
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
SELECT I.Instance,P.ChangedDate,P.OldVersion,P.NewVersion,P.OldProductLevel,P.NewProductLevel,P.OldProductUpdateLevel,P.NewProductUpdateLevel 
FROM dbo.SQLPatchingHistory P
JOIN dbo.Instances I ON I.InstanceID = P.InstanceID
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = P.InstanceID)
ORDER BY P.ChangedDate DESC