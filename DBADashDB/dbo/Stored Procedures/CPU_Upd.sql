CREATE PROC [dbo].[CPU_Upd](@CPU dbo.CPU READONLY,@InstanceID INT,@SnapshotDate DATETIME2(2))
AS
SET XACT_ABORT ON
DECLARE @Ref VARCHAR(30)='CPU'

DECLARE @MaxEventTime DATETIME2(3)
DECLARE @MaxEventTimeHr DATETIME2(3)
SELECT TOP(1) @MaxEventTime= ISNULL(MAX(EventTime),'19000101') 
FROM dbo.CPU
WHERE InstanceID = @InstanceID

SET @MaxEventTime = DATEADD(mi,1,CONVERT(DATETIME,LEFT(CONVERT(VARCHAR,@MaxEventTime,120),16),120))
SET @MaxEventTimeHr = CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,@MaxEventTime,120),0,14) + ':00',120) 

BEGIN TRAN
INSERT INTO dbo.CPU(InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU)
SELECT @InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU 
FROM @CPU t
WHERE t.EventTime>=@MaxEventTime
AND SQLProcessCPU>=0 AND SQLProcessCPU<=100
AND SystemIdleCPU>=0 AND SystemIdleCPU<=100
AND (SystemIdleCPU+SQLProcessCPU)<=100

IF @@ROWCOUNT>0
BEGIN
	DELETE dbo.CPU_60MIN
	WHERE InstanceID = @InstanceID
	AND EventTime>=@MaxEventTimeHr

	INSERT INTO dbo.CPU_60MIN
	(
		InstanceID,
		EventTime,
		SumSQLProcessCPU,
		SumSystemIdleCPU,
		SampleCount,
		MaxSQLProcessCPU,
		MaxOtherProcessCPU,
		MaxTotalCPU,
		CPU10,
		CPU20,
		CPU30,
		CPU40,
		CPU50,
		CPU60,
		CPU70,
		CPU80,
		CPU90,
		CPU100
	)
	SELECT InstanceID,
		CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,EventTime,120),0,14) + ':00',120) AS EventTime,
		SUM(SQLProcessCPU),
		SUM(SystemIdleCPU),
		COUNT(*) cnt,
		MAX(SQLProcessCPU),
		MAX(OtherCPU),
		MAX(TotalCPU),
		SUM(CPU10) AS CPU10,
		SUM(CPU20) AS CPU20,
		SUM(CPU30) AS CPU30,
		SUM(CPU40) AS CPU40,
		SUM(CPU50) AS CPU50,
		SUM(CPU60) AS CPU60,
		SUM(CPU70) AS CPU70,
		SUM(CPU80) AS CPU80,
		SUM(CPU90) AS CPU90,
		SUM(CPU100) AS CPU100
	FROM dbo.CPU_Histogram
	WHERE InstanceID = @InstanceID
	AND EventTime>=@MaxEventTimeHr
	GROUP BY InstanceID,
		CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,EventTime,120),0,14) + ':00',120) 

END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate

COMMIT