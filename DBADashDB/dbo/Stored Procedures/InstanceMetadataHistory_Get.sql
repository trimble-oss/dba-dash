CREATE PROC dbo.InstanceMetadataHistory_Get(
	@InstanceID INT,
	@Top INT = 100
)
AS
SELECT TOP(@Top)	
		M.InstanceID, 
		M.Provider, 
		M.Metadata, 
		P.Name,
		P.VMSize,
		P.SKU,
		P.ResourceGroup,
		P.PrivateIPs,
		P.PublicIPs,
		P.Tags,
		P.AccountID,
		P.SubscriptionID,
		P.Location,
		P.AvailabilityZone,
		P.ImageID,
		P.AWSInstanceID,
		P.VMHostName,
		P.IAMRole,
		P.SecurityGroups,
		P.LocalHostname,
		P.BillingProducts,
		P.ClusterIP,
		P.ClusterName,
		M.SnapshotDate,
		ISNULL(CD.SnapshotDate,LAG(M.PreviousVersionLastSnapshotDate) OVER (PARTITION BY M.InstanceID ORDER BY M.ValidFrom DESC)) AS LastSnapshotDate,
		M.ValidFrom, 
		M.ValidTo
FROM dbo.InstanceMetadata 
	FOR SYSTEM_TIME BETWEEN '19000101' AND '9999-12-31 23:59:59.9999999' AS M
LEFT JOIN dbo.CollectionDates CD ON CD.InstanceID = M.InstanceID AND CD.Reference = 'InstanceMetadata' AND M.ValidTo >= '99991231'
OUTER APPLY dbo.ParseInstanceMetadata(M.Metadata) P
WHERE M.InstanceID = @InstanceID
ORDER BY M.ValidFrom DESC


