CREATE PROC dbo.Instances_Get(	
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
	CASE WHEN I.EngineEdition = 5 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAzure,
	D.DatabaseID AS AzureDatabaseID,
	D.name AS AzureDBName,
	I.IsActive,
	I.EngineEdition
FROM dbo.InstancesMatchingTags(@TagIDs) I
LEFT JOIN dbo.Databases D ON D.InstanceID = I.InstanceID AND I.EngineEdition = 5 AND D.IsActive=1
WHERE (D.InstanceID IS NOT NULL OR I.EngineEdition <> 5 OR I.EngineEdition IS NULL)
' + CASE WHEN @IsActive IS NULL THEN '' ELSE 'AND I.IsActive=@IsActive' END + '
' + CASE WHEN @IsAzure=1 THEN 'AND I.EngineEdition = 5' WHEN @IsAzure=0 THEN 'AND I.EngineEdition <> 5' ELSE '' END + '
ORDER BY I.Instance,D.name'

EXEC sp_executesql @SQL,N'@TagIDs VARCHAR(MAX),@IsActive BIT',@TagIDs,@IsActive
