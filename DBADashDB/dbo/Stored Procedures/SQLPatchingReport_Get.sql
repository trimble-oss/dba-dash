CREATE PROC dbo.SQLPatchingReport_Get(
		@InstanceIDs IDs READONLY,
		@ShowHidden BIT=1
)
AS
/*
	Combined report used by the SQL Patching tab (SQLPatchingView).
	Returns two result sets:
		0 - Version Info   (current SQL version information per instance)
		1 - Patch History  (version change history)
*/

DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF EXISTS(SELECT 1 FROM @InstanceIDs)
BEGIN
	INSERT INTO @Instances(InstanceID)
	SELECT ID FROM @InstanceIDs
END
ELSE
BEGIN
	INSERT INTO @Instances(InstanceID)
	SELECT InstanceID FROM dbo.Instances WHERE IsActive=1
END;

/* Result set 0: Version Info */
SELECT	I.InstanceID,
		I.ConnectionID,
		I.InstanceDisplayName,
		I.SQLVersion,
		I.ProductVersion,
		I.ProductMajorVersion,
		I.ProductMinorVersion,
		I.ProductBuild,
		I.ProductRevision,
		I.Edition,
		I.EngineEdition,
		I.EditionID,
		I.ProductLevel,
		I.ProductUpdateLevel,
		I.ProductUpdateReference,
		I.ProductBuildType,
		I.ResourceVersion,
		I.ResourceLastUpdateDateTime,
		I.LicenseType,
		I.NumLicenses,
		I.WindowsCaption,
		I.WindowsRelease,
		I.WindowsSKU,
		H.ChangedDate AS PatchDate,
		BR.SupportedUntil,
		BR.KBList,
		CASE WHEN BR.IsCurrentBuild=1 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsUpdateAvailable,
		BR.SPBehind,
		BR.CUBehind,
		BR.LatestVersion,
		BR.LatestVersionPatchLevel,
		BR.BuildReferenceVersion,
		BR.BuildReferenceUpdated,
		BR.MainstreamEndDate,
		BR.LifecycleUrl,
		DATEDIFF(d,GETUTCDATE(),SupportedUntil) AS DaysUntilSupportExpires,
        DATEDIFF(d,GETUTCDATE(),MainstreamEndDate) AS DaysUntilMainstreamSupportExpires,
		I.IsWindowsUpdate
FROM dbo.InstanceInfo I
OUTER APPLY(SELECT TOP(1) H.ChangedDate
			FROM dbo.SQLPatchingHistory H
			WHERE H.InstanceID = I.InstanceID
			ORDER BY H.ChangedDate DESC
			) H
OUTER APPLY (SELECT TOP(1)
						BR.SPBehind,
						BR.CUBehind,
						BR.LatestVersion,
						BR.KBList,
						BR.IsCurrentBuild,
						BR.LatestVersionPatchLevel,
						BR.SupportedUntil,
						BR.BuildReferenceVersion,
						BR.BuildReferenceUpdated,
						BR.MainstreamEndDate,
						BR.LifecycleUrl
			FROM dbo.BuildReference BR
			WHERE I.ProductMajorVersion = BR.Major
			AND I.ProductMinorVersion = BR.Minor
			AND I.ProductBuild = BR.Build
			AND I.EngineEdition <=4 /* Exclude Azure */
			ORDER BY CASE WHEN  I.ProductRevision = BR.Revision THEN 0 ELSE 1 END) BR
WHERE EXISTS(SELECT 1
			FROM @Instances t
			WHERE t.InstanceID = I.InstanceID
			)
AND I.IsActive=1
AND (I.ShowInSummary=1 OR @ShowHidden=1);

/* Result set 1: Patch History */
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
WHERE EXISTS(SELECT 1 FROM @Instances t WHERE t.InstanceID = P.InstanceID)
AND (I.ShowInSummary=1 OR @ShowHidden=1)
ORDER BY P.ChangedDate DESC
