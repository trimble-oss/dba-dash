CREATE PROC dbo.Partitions_Add
AS
DECLARE @TableName SYSNAME
DECLARE @PeriodType CHAR(1)
DECLARE @PeriodCount INT
DECLARE @DaysInFuture INT=14
DECLARE @ErrorMessages NVARCHAR(MAX)=''
DECLARE @PartitionedTables TABLE(
	TableName SYSNAME PRIMARY KEY,
	PeriodType CHAR(1),
	PeriodCount INT
)
INSERT INTO @PartitionedTables
VALUES -- Daily Partitions
	('Waits','d',14),
	('CPU','d',14),
	('BlockingSnapshot','d',14),
	('AzureDBResourceStats','d',14),
	('AzureDBElasticPoolResourceStats','d',14),
	('ObjectExecutionStats','d',14),
	('DBIOStats','d',14),
	('SlowQueries','d',14),
	('CustomChecksHistory','d',14),
	('PerformanceCounters','d',14),
	('JobHistory','d',14),
	-- Monthly Partitions
	('AzureDBElasticPoolResourceStats_60MIN','m',3),
	('AzureDBResourceStats_60MIN','m',3),
	('CPU_60MIN','m',3),
	('DBIOStats_60MIN','m',3),
	('Waits_60MIN','m',3),
	('ObjectExecutionStats_60MIN','m',3),
	('PerformanceCounters_60MIN','m',3),
	('JobStats_60MIN','m',3)

DECLARE cDaily CURSOR FAST_FORWARD LOCAL FOR 
			SELECT TableName,
					PeriodType,
					PeriodCount
			FROM @PartitionedTables
			WHERE NOT EXISTS(SELECT 1 
							FROM dbo.PartitionBoundaryHelper('PF_' + TableName,TableName) h
							WHERE (
									( DATEDIFF(d, GETUTCDATE(), lb) >= PeriodCount AND PeriodType='d')
									OR (DATEDIFF(m, GETUTCDATE(), lb) >= PeriodCount AND PeriodType='m')
								)
							)
			ORDER BY TableName

OPEN cDaily
WHILE 1=1
BEGIN
	FETCH NEXT FROM cDaily INTO @TableName,@PeriodType,@PeriodCount
	IF @@FETCH_STATUS <> 0 
		BREAK
	PRINT @TableName
	BEGIN TRY
		EXEC dbo.Partitions_Create @TableName = @TableName,@PeriodCount=@PeriodCount,@PeriodType=@PeriodType
	END TRY
	BEGIN CATCH
		IF @@TRANCOUNT>0
			ROLLBACK
		SET @ErrorMessages = CONCAT(@ErrorMessages,'Error managing partitions for table "',@TableName,'": ',ERROR_MESSAGE(),CHAR(13)+CHAR(10))
	END CATCH
END
CLOSE cDaily
DEALLOCATE cDaily
IF @ErrorMessages <> ''
BEGIN;
	THROW 509999,@ErrorMessages,1
END