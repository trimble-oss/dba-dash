CREATE PROC Report.Instances_Get(
		@TagIDs NVARCHAR(MAX)=NULL,
		@InstanceName SYSNAME=NULL,
		@ExcludeAzure BIT=0,
		@AzureOnly BIT=0
)
AS
SELECT DISTINCT I.Instance,
				I.EditionID
FROM dbo.InstancesMatchingTags(@TagIDs) I
WHERE I.IsActive=1
AND (I.Instance LIKE + '%' + @InstanceName + '%' OR @InstanceName IS NULL)
AND (I.EngineEdition = 5 OR @AzureOnly=0)
AND (I.EngineEdition <> 5  OR @ExcludeAzure=0)