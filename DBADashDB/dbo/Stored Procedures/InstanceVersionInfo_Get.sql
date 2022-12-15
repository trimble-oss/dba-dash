CREATE PROC dbo.InstanceVersionInfo_Get(
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
SELECT	I.ConnectionID,
		I.InstanceDisplayName,
		I.SQLVersion,
		I.ProductVersion,
		I.Edition,
		I.EngineEdition,
		I.EditionID,
		I.ProductLevel,
		I.ProductUpdateLevel,
		I.ProductUpdateReference,
		I.ProductMajorVersion,
		I.ProductBuildType,
		I.ProductBuild,
		I.ResourceVersion,
		I.ResourceLastUpdateDateTime,
		I.LicenseType,
		I.NumLicenses,
		I.WindowsCaption,
		I.WindowsRelease,
		I.WindowsSKU,
		H.ChangedDate AS PatchDate
FROM dbo.InstanceInfo I
OUTER APPLY(SELECT TOP(1) H.ChangedDate 
			FROM dbo.SQLPatchingHistory H 
			WHERE H.InstanceID = I.InstanceID 
			ORDER BY H.ChangedDate DESC
			) H
WHERE EXISTS(SELECT 1 
			FROM @Instances t 
			WHERE t.InstanceID = I.InstanceID
			)
AND I.IsActive=1
AND (I.ShowInSummary=1 OR @ShowHidden=1)