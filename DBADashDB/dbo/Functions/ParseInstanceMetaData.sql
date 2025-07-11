CREATE FUNCTION dbo.ParseInstanceMetadata(
	@Metadata NVARCHAR(MAX)
)
RETURNS TABLE
AS
RETURN 
SELECT 	JSON_VALUE(@Metadata,'$.compute.name') AS Name,
		ISNULL(JSON_VALUE(@Metadata,'$.compute.vmSize'),JSON_VALUE(@Metadata,'$.instanceType')) AS VMSize,
		JSON_VALUE(@Metadata,'$.compute.sku') AS SKU,
		JSON_VALUE(@Metadata,'$.compute.resourceGroupName') AS ResourceGroup,
		ISNULL(JSON_VALUE(@Metadata,'$.privateIp'), STUFF((SELECT ',' + NULLIF(JSON_VALUE(ipConfig.value, '$.privateIpAddress'),'') 
			FROM OPENJSON(@Metadata, '$.network.interface') AS networkInterface 
			CROSS APPLY OPENJSON(networkInterface.value, '$.ipv4.ipAddress') AS ipConfig
			FOR XML PATH(''),TYPE
   			).value('.','NVARCHAR(MAX)'),1,1,'')) AS PrivateIPs,
		STUFF((SELECT ',' + NULLIF(JSON_VALUE(ipConfig.value, '$.publicIpAddress'),'') 
			FROM OPENJSON(@Metadata, '$.network.interface') AS networkInterface 
			CROSS APPLY OPENJSON(networkInterface.value, '$.ipv4.ipAddress') AS ipConfig
			FOR XML PATH(''),TYPE
   			).value('.','NVARCHAR(MAX)'),1,1,'') AS PublicIPs,
		STUFF((SELECT  ', ' + IPs.value
			FROM OPENJSON(@Metadata,'$.IPAddresses') AS IPs
			FOR XML PATH(''),TYPE
			).value('.','NVARCHAR(MAX)'),1,2,'') IPAddresses,
		ISNULL(JSON_VALUE(@Metadata,'$.compute.tags'),
			STUFF((SELECT ', ' +  [key] +':' + value
			FROM OPENJSON(@Metadata,'$.Tags') tags
			FOR XML PATH(''),TYPE).value('.','NVARCHAR(MAX)'),1,2,'')
		) AS Tags,
		JSON_VALUE(@Metadata,'$.accountId') AS AccountID,
		JSON_VALUE(@Metadata,'$.compute.subscriptionId') AS SubscriptionID,
		ISNULL(JSON_VALUE(@Metadata,'$.compute.location'),JSON_VALUE(@Metadata,'$.region')) AS Location,
		JSON_VALUE(@Metadata,'$.availabilityZone') AS AvailabilityZone,
		JSON_VALUE(@Metadata,'$.imageId') AS ImageID,
		JSON_VALUE(@Metadata,'$.instanceId') AS AWSInstanceID,
		JSON_VALUE(@Metadata,'$.VMHostName') AS VMHostName,
		JSON_VALUE(@Metadata,'$.IamRole') AS IAMRole,
		STUFF((SELECT  ', ' + G.value
			FROM OPENJSON(@Metadata,'$.SecurityGroups') AS G
			FOR XML PATH(''),TYPE
			).value('.','NVARCHAR(MAX)'),1,2,'') SecurityGroups,
		JSON_VALUE(@Metadata,'$.LocalHostname') AS LocalHostname,
				STUFF((SELECT  ', ' + BP.value
			FROM OPENJSON(@Metadata,'$.billingProducts') AS BP
			FOR XML PATH(''),TYPE
			).value('.','NVARCHAR(MAX)'),1,2,'') BillingProducts
		