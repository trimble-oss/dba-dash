CREATE FUNCTION dbo.GetInstanceMetadataTags(
	@InstanceID INT
)
RETURNS TABLE
AS
RETURN
WITH T AS (
SELECT	1 AS Type,
		UNPVT.TagName,
		UNPVT.TagValue
FROM (
	SELECT	CAST(M.Provider AS NVARCHAR(MAX)) AS Provider,
			CAST(VMSize AS NVARCHAR(MAX)) AS VMSize,
			CAST(Name AS NVARCHAR(MAX)) AS Name,
			CAST(SKU AS NVARCHAR(MAX)) AS SKU,
			CAST(ResourceGroup AS NVARCHAR(MAX)) AS ResourceGroup,
			CAST(AccountID AS NVARCHAR(MAX)) AS AccountID,
			CAST(SubscriptionID AS NVARCHAR(MAX)) AS SubscriptionID,
			CAST(Location AS NVARCHAR(MAX)) AS Location,
			CAST(AvailabilityZone AS NVARCHAR(MAX)) AS AvailabilityZone,
			CAST(ImageID AS NVARCHAR(MAX)) AS ImageID,
			CAST(AWSInstanceID AS NVARCHAR(MAX)) AS AWSInstanceID,
			CAST(IAMRole AS NVARCHAR(MAX)) AS IAMRole,
			CAST(VMHostName AS NVARCHAR(MAX)) AS VMHostName,
			CAST(BillingProducts AS NVARCHAR(MAX)) AS BillingProducts
	FROM dbo.InstanceMetadata M
	OUTER APPLY dbo.ParseInstanceMetadata(M.Metadata) P
	WHERE M.InstanceID = @InstanceID
	) MD
UNPIVOT(TagValue 
		FOR TagName IN(	[VMSize],
						[Name],
						[SKU],
						[ResourceGroup],
						[AccountID],
						[SubscriptionID],
						[Location],
						[AvailabilityZone],
						[ImageID],
						[AWSInstanceID],
						[Provider],
						[IAMRole],
						[VMHostName],
						[BillingProducts]
		)
		) AS UNPVT

UNION ALL
SELECT	2 AS Type,
		JSON_VALUE(tags.value, '$.name') COLLATE DATABASE_DEFAULT AS TagName,
		JSON_VALUE(tags.value, '$.value') COLLATE DATABASE_DEFAULT AS TagValue
FROM dbo.InstanceMetadata M
OUTER APPLY OPENJSON(M.Metadata, '$.compute.tagsList') AS tags 
WHERE M.InstanceID = @InstanceID
UNION ALL
SELECT	2 AS Type,
		tags.[key] AS TagName,
		tags.value AS TagValue
FROM dbo.InstanceMetadata M
OUTER APPLY OPENJSON(M.Metadata,'$.Tags') tags
WHERE M.InstanceID = @InstanceID
)
SELECT	Type,
		TagName,
		TagValue
FROM T 
WHERE TagName IS NOT NULL
AND TagValue IS NOT NULL