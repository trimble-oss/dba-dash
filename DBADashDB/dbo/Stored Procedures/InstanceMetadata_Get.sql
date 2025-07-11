CREATE PROC dbo.InstanceMetadata_Get(
	@InstanceIDs IDs READONLY
)
AS
SELECT	I.InstanceDisplayName,
		M.InstanceID, 
		M.Provider, 
		M.Metadata, 
		P.Name,
		P.VMSize,
		P.SKU,
		P.ResourceGroup,
		P.PrivateIPs,
		P.PublicIPs,
		P.IPAddresses,
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
		M.SnapshotDate,
		CD.SnapshotDate AS LastSnapshotDate,
		M.ValidFrom, 
		M.ValidTo
FROM dbo.Instances I 
JOIN dbo.InstanceMetadata M ON I.InstanceID = M.InstanceID
LEFT JOIN dbo.CollectionDates CD ON CD.InstanceID = M.InstanceID AND CD.Reference = 'InstanceMetadata'
OUTER APPLY dbo.ParseInstanceMetadata(M.Metadata) P
WHERE EXISTS(SELECT 1 
			FROM @InstanceIDs IDs 
			WHERE IDs.ID = M.InstanceID
			)
AND I.IsActive=1