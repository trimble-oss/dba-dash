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
		MaxTotalCPU
	)
	SELECT InstanceID,
		CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,EventTime,120),0,14) + ':00',120) AS EventTime,
		SUM(SQLProcessCPU),
		SUM(SystemIdleCPU),
		COUNT(*) cnt,
		MAX(SQLProcessCPU),
		MAX(OtherCPU),
		MAX(TotalCPU)
	FROM dbo.CPU
	WHERE InstanceID = @InstanceID
	AND EventTime>=@MaxEventTimeHr
	GROUP BY InstanceID,
		CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,EventTime,120),0,14) + ':00',120) 

END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate

COMMIT