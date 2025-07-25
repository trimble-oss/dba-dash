CREATE PROC dbo.Instances_Get(	
	@TagIDs VARCHAR(MAX)=NULL,
	@IsActive BIT=1,
	@IsAzure BIT=NULL,
	@SearchString NVARCHAR(128)=NULL,
	@GroupByTag NVARCHAR(50)=NULL,
	@ConnectionID NVARCHAR(128)=NULL,
	@Debug BIT =0
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
	I.EngineEdition,
	I.InstanceDisplayName,
	I.InstanceGroupName,
	I.ShowInSummary,
	~I.ShowInSummary AS IsHidden,
	' + CASE WHEN @GroupByTag IS NULL THEN 'CAST(NULL AS NVARCHAR(128)) as TagGroup' ELSE 'ISNULL(T.TagValue,''[N/A]'') AS TagGroup' END + ',
	SO.elastic_pool_name,
	I.CollectAgentID,
	I.ImportAgentID,
	IA.MessagingEnabled & CA.MessagingEnabled AS MessagingEnabled,
	I.ProductVersion,
	CASE WHEN M.InstanceID IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS HasInstanceMetadata
FROM dbo.InstancesMatchingTags(@TagIDs) I
LEFT JOIN dbo.Databases D ON D.InstanceID = I.InstanceID AND I.EngineEdition = 5 AND D.IsActive=1
LEFT JOIN dbo.AzureDBServiceObjectives SO ON I.InstanceID = SO.InstanceID
LEFT JOIN dbo.DBADashAgent IA ON I.ImportAgentID = IA.DBADashAgentID
LEFT JOIN dbo.DBADashAgent CA ON I.CollectAgentID = CA.DBADashAgentID
LEFT JOIN dbo.InstanceMetadata M ON I.InstanceID = M.InstanceID
' + CASE WHEN @GroupByTag IS NULL THEN '' ELSE 'OUTER APPLY dbo.TagValue(I.InstanceID,@GroupByTag,I.EngineEdition) T' END + '
WHERE (D.InstanceID IS NOT NULL OR I.EngineEdition <> 5 OR I.EngineEdition IS NULL)
' + CASE WHEN @IsActive IS NULL THEN '' ELSE 'AND I.IsActive=@IsActive' END + '
' + CASE WHEN @IsAzure=1 THEN 'AND I.EngineEdition = 5' WHEN @IsAzure=0 THEN 'AND I.EngineEdition <> 5' ELSE '' END + '
' + CASE WHEN @SearchString IS NULL THEN '' ELSE 'AND (I.ConnectionID LIKE @SearchString OR I.Alias LIKE @SearchString OR I.Instance LIKE @SearchString)' END + '
' + CASE WHEN @ConnectionID IS NULL THEN '' ELSE 'AND I.ConnectionID = @ConnectionID' END + '
ORDER BY TagGroup,I.InstanceDisplayName'

IF @Debug=1
BEGIN
	EXEC dbo.PrintMax @SQL
END

EXEC sp_executesql @SQL,N'@TagIDs VARCHAR(MAX),@IsActive BIT,@SearchString NVARCHAR(128),@GroupByTag NVARCHAR(50),@ConnectionID NVARCHAR(128)',@TagIDs,@IsActive,@SearchString,@GroupByTag,@ConnectionID

