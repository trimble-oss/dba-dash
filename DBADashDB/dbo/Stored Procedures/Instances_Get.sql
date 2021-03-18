CREATE PROC [dbo].[Instances_Get](@TagIDs VARCHAR(MAX)=NULL,@IsActive BIT=1)
AS
SELECT  I.InstanceID,I.ConnectionID,I.Instance,CASE WHEN I.EditionID = 1674378470 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAzure,D.DatabaseID AS AzureDatabaseID,D.name AS AzureDBName,I.IsActive
FROM dbo.InstancesMatchingTags(@TagIDs) I
LEFT JOIN dbo.Databases D ON D.InstanceID = I.InstanceID AND I.EditionID = 1674378470 AND D.IsActive=1
WHERE (I.IsActive=@IsActive OR @IsActive IS NULL)
ORDER BY I.Instance,D.name