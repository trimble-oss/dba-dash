
CREATE PROC [dbo].[CPU_Upd](@CPU dbo.CPU READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
DECLARE @Ref VARCHAR(30)='CPU'

DECLARE @MaxEventTime DATETIME2(3)
SELECT TOP(1) @MaxEventTime= ISNULL(MAX(EventTime),'19000101') 
FROM dbo.CPU
WHERE InstanceID = @InstanceID

SET @MaxEventTime = DATEADD(mi,1,CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,@MaxEventTime,120),16),120))

INSERT INTO dbo.CPU(InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU)
SELECT @InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU 
FROM @CPU t
WHERE t.EventTime>=@MaxEventTime

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate