CREATE PROC dbo.Configuration_Get(
	@InstanceIDs VARCHAR(MAX)=NULL,
	@ConfiguredOnly BIT=0,
	@ShowHidden BIT=1,
	@AdviceOnly BIT=0
)
AS
DECLARE @Instances TABLE(
	InstanceID INT PRIMARY KEY
)
IF @InstanceIDs IS NULL
BEGIN
	INSERT INTO @Instances
	(
	    InstanceID
	)
	SELECT InstanceID 
	FROM dbo.Instances 
	WHERE IsActive=1
END 
ELSE 
BEGIN
	INSERT INTO @Instances
	(
		InstanceID
	)
	SELECT value
	FROM STRING_SPLIT(@InstanceIDs,',')
END;
DECLARE @WarningLow INT = 6;
DECLARE @OK INT =4;
DECLARE @Warning INT = 2;
DECLARE @Critical INT = 1;
DECLARE @Information INT = 7;
DECLARE @NA INT = 3;

WITH T AS (
	SELECT I.Instance, 
			I.ConnectionID,
			I.InstanceDisplayName,
			SCO.configuration_id,
			SCO.name,
			SCO.default_value,
			SC.value,
			calc.IsDefault,
			MAX(CASE WHEN calc.IsDefault = 1 THEN 0 WHEN calc.IsDefault IS NULL THEN NULL ELSE 1 END) OVER(PARTITION BY SCO.configuration_id) AS ConfiguredForAny, /* Configured for any instance */
			calc2.ConfigurationStatus,
			calc2.ConfigurationNotice,
			MAX(CASE WHEN calc2.ConfigurationStatus NOT IN(@NA, @Information) OR calc.IsDefault=0 THEN 1 ELSE 0 END) OVER(PARTITION BY SCO.configuration_id) AS AdviceForAny /* Advice for any instance */
	FROM dbo.SysConfig SC
	JOIN dbo.InstanceInfo I ON SC.InstanceID=I.InstanceID
	JOIN dbo.SysConfigOptions SCO ON SC.configuration_id = SCO.configuration_id
	OUTER APPLY(SELECT 	 CASE 
						 --ADR cleaner retry timeout (min)
						 WHEN SC.configuration_id = 1591 AND SC.value IN(15,0,120) THEN CAST(1 AS BIT) 
						 --ADR Preallocation Factor
						 WHEN SC.configuration_id = 1592 AND SC.value IN(4,0) THEN CAST(1 AS BIT)
						 WHEN SC.value=SCO.default_value THEN CAST(1 AS BIT) 
						 WHEN SCO.default_value IS NULL THEN NULL 
						 ELSE CAST(0 AS BIT) END AS IsDefault
		
				) calc
	OUTER APPLY(
			SELECT CASE WHEN SC.configuration_id=1539 AND SC.value=0 AND I.cpu_count > I.MaxRecommendedMaxDOP THEN @Critical /* MAXDOP is reccomended to be changed from default value.  The auto value falls outside the range of recommended values. */
			WHEN SC.configuration_id=1539 AND SC.value=0  THEN @WarningLow /* MAXDOP is reccomended to be changed from default value.  The auto value falls within the range of recommended values. */
			WHEN SC.configuration_id=1539 AND SC.value = 1 AND I.MaxRecommendedMaxDOP>1 THEN @WarningLow /* MAXDOP has been set to 1.*/
			WHEN SC.configuration_id=1539 AND SC.value >= I.MinRecommendedMaxDOP AND SC.value <= I.MaxRecommendedMaxDOP THEN @OK /* MAXDOP configured & falls within recommended range */
			WHEN SC.configuration_id=1539 AND SC.value >= I.MaxRecommendedMaxDOP THEN @Warning /* MAXDOP configured outside the range of reccomended values */
			WHEN SC.configuration_id=1538 AND SC.value<=5 THEN @Warning /* Cost threshold for parallelism not configured */
			WHEN SC.configuration_id=1538 AND SC.value>5 THEN @Information /* Cost threshold for parallelism not configured */
			WHEN calc.IsDefault IS NULL THEN @NA /* No specified default, ignore. N/A */
			WHEN SC.configuration_id=1579 AND SC.value = 1 THEN @OK /* backup compression enabled */
			WHEN SC.configuration_id=1579 AND SC.value = 0 THEN @WarningLow /* backup compression NOT enabled */
			WHEN SC.configuration_id=1584 AND SC.value=1 THEN @OK /* Backup checksum default is enabled */
			WHEN SC.configuration_id=1584 AND SC.value=0 THEN @WarningLow /* Backup checksum default is NOT enabled */
			WHEN SC.configuration_id=1544 AND SC.value=2147483647 THEN @Warning /* Max Server memory should be set*/
			WHEN SC.configuration_id=1544 AND I.PctMemoryAllocatedToBufferPool>1 OR (I.MemoryNotAllocatedToBufferPoolGB<4 AND I.PhysicalMemoryGB>16) THEN @Warning /* Max Server memory set too high */
			WHEN SC.configuration_id=1544 THEN @Information
			WHEN SC.configuration_id=1576 AND SC.value=0 THEN @WarningLow /* Remote DAC not enabled */
			WHEN SC.configuration_id=1576 AND SC.value=1 THEN @OK /* Remote DAC enabled */
			WHEN calc.IsDefault = 1 THEN @NA /* default value used */
			ELSE @WarningLow END AS ConfigurationStatus, /* not using a default value */
			CASE WHEN SC.configuration_id=1539 AND SC.value=0 AND I.cpu_count > I.MaxRecommendedMaxDOP THEN CONCAT('MAXDOP is recommended to be changed from default value.  The auto value falls outside the range of recommended values (', I.MinRecommendedMaxDOP,'-', I.MaxRecommendedMaxDOP,' for this server)')
			WHEN SC.configuration_id=1539 AND SC.value=0  THEN CONCAT('MAXDOP is recommended to be changed from default value.  The auto value falls within the range of recommended values (', I.MinRecommendedMaxDOP,'-', I.MaxRecommendedMaxDOP,' for this server)')
			WHEN SC.configuration_id=1539 AND SC.value = 1 AND I.MaxRecommendedMaxDOP>1 THEN 'MAXDOP has been set to 1. This will prevent queries from going parallel.  This can help with concurrency in some cases but your queries won''t benefit from the performance advantages of parallel execution.  Instead of disabling parallelism with MAXDOP set to 1, consider increasing the cost threshold for parallelism to prevent inexpensive queries from getting a parallel plan.'
			WHEN SC.configuration_id=1539 AND SC.value >= I.MinRecommendedMaxDOP AND SC.value <= I.MaxRecommendedMaxDOP THEN CONCAT('MAXDOP setting falls within the range of recommended values (', I.MinRecommendedMaxDOP,'-', I.MaxRecommendedMaxDOP,' for this server)')
			WHEN SC.configuration_id=1539 AND SC.value >= I.MaxRecommendedMaxDOP THEN CONCAT('MAXDOP has been configured outside the range of recommended values (', I.MinRecommendedMaxDOP,'-', I.MaxRecommendedMaxDOP,' for this server)')
			WHEN SC.configuration_id=1538 AND SC.value<=5 THEN 'Cost threshold for parallelism not configured. The default value of 5 is low. A low threshold means inexpensive queries can get a parallel plan which can hurt concurrency.'
			WHEN SC.configuration_id=1538 AND SC.value>5 THEN 'Cost threshold for parallelism has been increased from the default value of 5. This can help prevent inexpensive queries getting a parallel plan and hurting concurrency.'
			WHEN calc.IsDefault IS NULL THEN 'No defaults considered for this configuration.'
			WHEN SC.configuration_id=1579 AND SC.value = 1 THEN 'Backup compression is enabled. This is recommended as it can reduce the size of your backups and make them faster.'
			WHEN SC.configuration_id=1579 AND SC.value = 0 THEN 'Backup compression is NOT enabled (default).  Backup compression can make your backups smaller and faster. Note: Backups might still be compressed if you explicitly request backup compression.'
			WHEN SC.configuration_id=1584 AND SC.value=1 THEN 'Backup checksum is enabled.  This is recommended to validate the integrity of your backups.'
			WHEN SC.configuration_id=1584 AND SC.value=0 THEN 'Backup checksum is NOT enabled (default). Consider enabling this to validate the integrity of your backups. Note: Backups might still use CHECKSUM if you explicitly request it.'
			WHEN SC.configuration_id=1544 AND SC.value=2147483647 THEN CONCAT('Max server memory has NOT been configured. SQL Server can consume 100% of the ',CONVERT(DECIMAL(10,1),I.PhysicalMemoryGB,1), 'GB memory.  Even on a dedicated SQL instance you should consider leaving ~10%/4GB for the O/S')
			WHEN SC.configuration_id=1544 AND (I.PctMemoryAllocatedToBufferPool>1 OR (I.MemoryNotAllocatedToBufferPoolGB<4 AND I.PhysicalMemoryGB>16)) THEN CONCAT('Max server memory has been configured. ',CONVERT(DECIMAL(10,1),I.BufferPoolMB/1024),'GB of ',CONVERT(DECIMAL(10,1),I.PhysicalMemoryGB,1), 'GB memory allocated to the buffer pool (',FORMAT(I.PctMemoryAllocatedToBufferPool,'P1'),').  Consider reducing max server memory.')
			WHEN SC.configuration_id=1544 THEN CONCAT('Max server memory has been configured. ',CONVERT(DECIMAL(10,1),I.BufferPoolMB/1024),'GB of ',CONVERT(DECIMAL(10,1),I.PhysicalMemoryGB,1), 'GB memory allocated to the buffer pool (',FORMAT(I.PctMemoryAllocatedToBufferPool,'P1'),')')
			WHEN SC.configuration_id=1576 AND SC.value=0 THEN 'Remote DAC (dedicated administrator connection) is disabled (default).  Consider enabling remote DAC as this can provide a way to access your SQL instance even when it''s experiencing serious performance issues.'
			WHEN SC.configuration_id=1576 AND SC.value=1 THEN 'Remote DAC (dedicated administrator connection) is enabled).  This can provide a way to access your SQL instance even when it''s experiencing serious performance issues.'
			WHEN calc.IsDefault = 1 THEN 'Using the default value' 
			ELSE 'Not using the default value.' END AS ConfigurationNotice 
			) calc2
	WHERE EXISTS(SELECT 1 FROM @Instances t WHERE I.InstanceID = t.InstanceID)
	AND I.IsActive=1
	AND (I.ShowInSummary=1 OR @ShowHidden=1)
)
SELECT T.Instance,
       T.ConnectionID,
	   T.InstanceDisplayName,
       T.configuration_id,
       T.name,
       T.default_value,
       T.value,
       T.IsDefault,
	   T.ConfigurationStatus,
	   T.ConfigurationNotice
FROM T
WHERE (T.ConfiguredForAny=1 OR @ConfiguredOnly=0)
AND (T.AdviceForAny=1 OR @AdviceOnly=0)
ORDER BY T.InstanceDisplayName,T.name