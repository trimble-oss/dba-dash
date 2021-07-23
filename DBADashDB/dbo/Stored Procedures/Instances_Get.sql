CREATE PROC [dbo].[Instances_Get](	
	@TagIDs VARCHAR(MAX)=NULL,
	@IsActive BIT=1,
	@IsAzure BIT=NULL
)
AS
DECLARE @SQL NVARCHAR(MAX)
SET @SQL = N'
SELECT  I.InstanceID,
	I.ConnectionID,
	I.Instance,
	CASE WHEN I.EditionID = 1674378470 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAzure,
	D.DatabaseID AS AzureDatabaseID,
	D.name AS AzureDBName,
	I.IsActive
FROM dbo.InstancesMatchingTags(@TagIDs) I
LEFT JOIN dbo.Databases D ON D.InstanceID = I.InstanceID AND I.EditionID = 1674378470 AND D.IsActive=1
WHERE (D.InstanceID IS NOT NULL OR I.EditionID <> 1674378470)
' + CASE WHEN @IsActive IS NULL THEN '' ELSE 'AND I.IsActive=@IsActive' END + '
' + CASE WHEN @IsAzure=1 THEN 'AND I.EditionID = 1674378470' WHEN @IsAzure=0 THEN 'AND I.EditionID <> 1674378470' ELSE '' END + '
ORDER BY I.Instance,D.name'

EXEC sp_executesql @SQL,N'@TagIDs VARCHAR(MAX),@IsActive BIT',@TagIDs,@IsActive
