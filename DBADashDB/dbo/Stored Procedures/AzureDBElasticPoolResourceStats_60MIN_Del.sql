CREATE PROC dbo.AzureDBElasticPoolResourceStats_60MIN_Del(
	@InstanceID INT,
	@DaysToKeep INT,
	@BatchSize INT = 500000
)
AS
/* 
	Partitioning switching is used to efficiently remove data from this table for all instances.  
	This proc provides an alternative way to remove data associated with a specific instance.
*/
SET NOCOUNT ON
DECLARE @MaxDate DATETIME2
SELECT @MaxDate = CASE WHEN @DaysToKeep  = 0 THEN '30000101' ELSE DATEADD(d,-@DaysToKeep,GETUTCDATE()) END

WHILE 1=1
BEGIN
	DELETE EPRS 
	FROM dbo.AzureDBElasticPoolResourceStats_60MIN EPRS
	WHERE EXISTS(SELECT 1 
				FROM dbo.AzureDBElasticPool EP
				WHERE EP.InstanceID = @InstanceID
				AND EP.PoolID = EPRS.PoolID
				)
	AND end_time < @MaxDate
	OPTION(RECOMPILE)

	IF @@ROWCOUNT < @BatchSize
		BREAK
END