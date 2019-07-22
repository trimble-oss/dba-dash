CREATE PROC [dbo].[ServerProperties_Upd](@ServerProperties ServerProperties READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
IF NOT EXISTS(SELECT 1 FROM dbo.SnapshotDates WHERE ServerPropertiesDate>=@SnapshotDate AND InstanceID=@InstanceID)
BEGIN
	UPDATE I
	   SET [BuildClrVersion] = T.BuildClrVersion
		  ,[Collation] = T.Collation
		  ,[CollationID] = T.CollationID
		  ,[ComparisonStyle] = T.ComparisonStyle
		  ,[ComputerNamePhysicalNetBIOS] = T.ComputerNamePhysicalNetBIOS
		  ,[Edition] = T.Edition
		  ,[EditionID] = T.EditionID
		  ,[EngineEdition] = T.EngineEdition
		  ,[FileStreamConfiguredLevel] = T.FileStreamConfiguredLevel
		  ,[FileStreamEffectiveLevel] = T.FileStreamEffectiveLevel
		  ,[FileStreamShareName] = T.FileStreamShareName
		  ,[HadrManagerStatus] = T.HadrManagerStatus
		  ,[InstanceDefaultDataPath] = T.InstanceDefaultDataPath
		  ,[InstanceDefaultLogPath] = T.InstanceDefaultLogPath
		  ,[InstanceName] = T.InstanceName
		  ,[IsAdvancedAnalyticsInstalled] = T.IsAdvancedAnalyticsInstalled
		  ,[IsClustered] = T.IsClustered
		  ,[IsFullTextInstalled] = T.IsFullTextInstalled
		  ,[IsHadrEnabled] = T.IsHadrEnabled
		  ,[IsIntegratedSecurityOnly] = T.IsIntegratedSecurityOnly
		  ,[IsLocalDB] = T.IsLocalDB
		  ,[IsPolybaseInstalled] = T.IsPolybaseInstalled
		  ,[IsXTPSupported] = T.IsXTPSupported
		  ,[LCID] = T.LCID
		  ,[LicenseType] = T.LicenseType
		  ,[MachineName] = T.MachineName
		  ,[NumLicenses] = T.NumLicenses
		  ,[ProductBuild] = T.ProductBuild
		  ,[ProductBuildType] = T.ProductBuildType
		  ,[ProductLevel] = T.ProductLevel
		  ,[ProductMajorVersion] = T.ProductMajorVersion
		  ,[ProductUpdateLevel] = T.ProductUpdateLevel
		  ,[ProductUpdateReference] = T.ProductUpdateReference
		  ,[ProductVersion] = T.ProductVersion
		  ,[ResourceLastUpdateDateTime] = T.ResourceLastUpdateDateTime
		  ,[ResourceVersion] = T.ResourceVersion
		  ,[ServerName] = T.ServerName
		  ,[SqlCharSet] = T.SqlCharSet
		  ,[SqlCharSetName] = T.SqlCharSetName
		  ,[SqlSortOrder] = T.SqlSortOrder
		  ,[SqlSortOrderName] = T.SqlSortOrderName
	FROM dbo.Instances I
	CROSS JOIN @ServerProperties T
	WHERE I.InstanceID = @InstanceID

	UPDATE dbo.SnapshotDates
	SET ServerPropertiesDate=@SnapshotDate
	WHERE InstanceID=@InstanceID

END