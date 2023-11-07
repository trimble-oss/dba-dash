CREATE PROC dbo.Partitions_Add
AS
DECLARE @SchemaName SYSNAME
DECLARE @TableName SYSNAME
DECLARE @PeriodType CHAR(1)
DECLARE @PeriodCount INT
DECLARE @ErrorMessages NVARCHAR(MAX)=''

DECLARE cDaily CURSOR FAST_FORWARD LOCAL FOR 
			SELECT	SchemaName,
					TableName,
					PeriodType,
					PeriodCount
			FROM dbo.PartitionConfiguration PC			
			WHERE NOT EXISTS(SELECT 1 
							FROM dbo.PartitionHelper PH
							WHERE (
									( DATEDIFF(d, GETUTCDATE(), PH.LowerBound) >= PC.PeriodCount AND PC.PeriodType='d')
									OR (DATEDIFF(m, GETUTCDATE(), PH.LowerBound) >= PC.PeriodCount AND PC.PeriodType='m')
								)
							AND PH.TableName = PC.TableName
							AND PH.SchemaName = PC.SchemaName
							)
											
			ORDER BY IsSystem DESC, TableName

OPEN cDaily
WHILE 1=1
BEGIN
	FETCH NEXT FROM cDaily INTO @SchemaName, @TableName,@PeriodType,@PeriodCount
	IF @@FETCH_STATUS <> 0 
		BREAK
	PRINT @TableName
	BEGIN TRY
		EXEC dbo.Partitions_Create @SchemaName=@SchemaName, @TableName = @TableName,@PeriodCount=@PeriodCount,@PeriodType=@PeriodType
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