CREATE PROC dbo.SQLPatching_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ShowHidden BIT=1
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

SELECT	I.Instance,
		I.InstanceDisplayName,
		P.ChangedDate,
		P.OldVersion,
		NULLIF(P.NewVersion,P.OldVersion) NewVersion,
		P.OldProductLevel,
		NULLIF(P.NewProductLevel,P.OldProductLevel) NewProductLevel,
		P.OldProductUpdateLevel,
		NULLIF(P.NewProductUpdateLevel,P.OldProductUpdateLevel) NewProductUpdateLevel,
		P.OldEdition,
		NULLIF(P.NewEdition,P.OldEdition) NewEdition
FROM dbo.SQLPatchingHistory P
JOIN dbo.Instances I ON I.InstanceID = P.InstanceID
WHERE EXISTS(SELECT 1 FROM @Instances I WHERE I.InstanceID = P.InstanceID)
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY P.ChangedDate DESC