CREATE PROC dbo.Instances_HardDelete(
	@HardDeleteThresholdDays INT
)
AS
/* 
	Removes SQL instances that have been soft deleted (IsActive=0) after a specified number of days (@HardDeleteThresholdDays)
	The number of days is based on the time since the last collection.
*/
IF (@HardDeleteThresholdDays<1 OR @HardDeleteThresholdDays IS NULL)
BEGIN
	RAISERROR('@HardDeleteThresholdDays must be >=1',11,1)
	RETURN
END
DECLARE @InstanceID INT
DECLARE @ConnectionID NVARCHAR(MAX)
DECLARE c1 CURSOR FAST_FORWARD LOCAL FOR
	SELECT InstanceID,ConnectionID
	FROM dbo.Instances I
	where IsActive=0
	AND NOT EXISTS(SELECT 1 
			FROM dbo.CollectionDates CD 
			WHERE CD.InstanceID = I.InstanceID
			AND CD.SnapshotDate >= DATEADD(d,-@HardDeleteThresholdDays,GETUTCDATE())
			)
OPEN c1
WHILE 1=1
BEGIN
	FETCH NEXT FROM c1 INTO @InstanceID,@ConnectionID
	IF @@FETCH_STATUS<>0
		BREAK
	PRINT CONCAT('Deleting ',@ConnectionID)
	EXEC dbo.Instance_Del @InstanceID=@InstanceID,@HardDelete=1
END
CLOSE c1
DEALLOCATE c1