CREATE PROC [dbo].[CPU_Upd](
	@CPU dbo.CPU READONLY,
	@InstanceID INT,
	@SnapshotDate DATETIME2(2)
)
AS
SET XACT_ABORT ON
DECLARE @IsLinux BIT 
SELECT @IsLinux = CASE WHEN EXISTS(SELECT 1 
									FROM dbo.Instances
									WHERE InstanceID = @InstanceID
									AND host_platform = 'Linux') 
						THEN CAST(1 as BIT) ELSE CAST(0 as BIT) END
DECLARE @Ref VARCHAR(30)='CPU'

DECLARE @Inserted TABLE(
	[EventTime] [datetime2](3) NOT NULL PRIMARY KEY,
	[SQLProcessCPU] [tinyint] NOT NULL,
	[SystemIdleCPU] [tinyint] NOT NULL
)
DECLARE @60MIN TABLE(
	[EventTime] [datetime2](3) NOT NULL,
	[SumSQLProcessCPU] [int] NOT NULL,
	[SumSystemIdleCPU] [int] NOT NULL,
	[SampleCount] [smallint] NOT NULL,
	[MaxSQLProcessCPU] [tinyint] NOT NULL,
	[MaxOtherProcessCPU] [tinyint] NOT NULL,
	[MaxTotalCPU] [tinyint] NOT NULL,
	[CPU10] [smallint] NULL,
	[CPU20] [smallint] NULL,
	[CPU30] [smallint] NULL,
	[CPU40] [smallint] NULL,
	[CPU50] [smallint] NULL,
	[CPU60] [smallint] NULL,
	[CPU70] [smallint] NULL,
	[CPU80] [smallint] NULL,
	[CPU90] [smallint] NULL,
	[CPU100] [smallint] NULL
)

DECLARE @MaxEventTime DATETIME2(3)
DECLARE @MaxEventTimeHr DATETIME2(3)
SELECT TOP(1) @MaxEventTime= ISNULL(MAX(EventTime),'19000101') 
FROM dbo.CPU
WHERE InstanceID = @InstanceID

SELECT @MaxEventTime = DATEADD(mi,1,DG.DateGroup)
FROM dbo.DateGroupingMins(@MaxEventTime,1) DG

SELECT @MaxEventTimeHr = DG.DateGroup 
FROM dbo.DateGroupingMins(@MaxEventTime,60) DG

BEGIN TRAN
INSERT INTO dbo.CPU(InstanceID,EventTime,SQLProcessCPU,SystemIdleCPU)
OUTPUT INSERTED.EventTime,INSERTED.SQLProcessCPU,INSERTED.SystemIdleCPU INTO @Inserted(EventTime,SQLProcessCPU,SystemIdleCPU)
SELECT @InstanceID,EventTime,SQLProcessCPU,CASE WHEN @IsLinux=1 THEN 100-SQLProcessCPU ELSE SystemIdleCPU END
FROM @CPU t
WHERE t.EventTime>=@MaxEventTime
AND SQLProcessCPU>=0 AND SQLProcessCPU<=100
AND SystemIdleCPU>=0 AND SystemIdleCPU<=100
AND (SystemIdleCPU+SQLProcessCPU)<=100

IF @@ROWCOUNT>0
BEGIN
	INSERT INTO @60MIN(	
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
	SELECT 	DG.DateGroup AS EventTime,
			SUM(SQLProcessCPU) AS SumSQLProcessCPU,
			SUM(SystemIdleCPU) AS SumSystemIdleCPU,
			COUNT(*) AS SampleCount,
			MAX(SQLProcessCPU) MaxSQLProcessCPU,
			MAX(100-(SQLProcessCPU+SystemIdleCPU)) MaxOtherProcessCPU,
			MAX(100-SystemIdleCPU) as MaxTotalCPU,
			SUM(HCPU.CPU10) AS CPU10,
			SUM(HCPU.CPU20) AS CPU20,
			SUM(HCPU.CPU30) AS CPU30,
			SUM(HCPU.CPU40) AS CPU40,
			SUM(HCPU.CPU50) AS CPU50,
			SUM(HCPU.CPU60) AS CPU60,
			SUM(HCPU.CPU70) AS CPU70,
			SUM(HCPU.CPU80) AS CPU80,
			SUM(HCPU.CPU90) AS CPU90,
			SUM(HCPU.CPU100) AS CPU100
	FROM @Inserted I 
	CROSS APPLY dbo.DateGroupingMins(I.EventTime,60) DG
	CROSS APPLY dbo.Histogram_CPU(100-SystemIdleCPU) HCPU
	GROUP BY DG.DateGroup

	UPDATE T
		SET T.SumSQLProcessCPU+=S.SumSQLProcessCPU,
			T.SumSystemIdleCPU+=S.SumSystemIdleCPU,
			T.SampleCount+=S.SampleCount,
			T.MaxSQLProcessCPU = (SELECT MAX(V) FROM (VALUES(S.MaxSQLProcessCPU),(T.MaxSQLProcessCPU)) M(V)),
			T.MaxOtherProcessCPU = (SELECT MAX(V) FROM (VALUES(S.MaxOtherProcessCPU ),(T.MaxOtherProcessCPU )) M(V)),
			T.MaxTotalCPU = (SELECT MAX(V) FROM (VALUES(S.MaxTotalCPU),(T.MaxTotalCPU )) M(V)),
			T.CPU10 += S.CPU10,
			T.CPU20 += S.CPU20,
			T.CPU30 += S.CPU30,
			T.CPU40 += S.CPU40,
			T.CPU50 += S.CPU50,
			T.CPU60 += S.CPU60,
			T.CPU70 += S.CPU70,
			T.CPU80 += S.CPU80,
			T.CPU90 += S.CPU90,
			T.CPU100 += S.CPU100
	FROM dbo.CPU_60MIN T 
	JOIN @60MIN S ON S.EventTime = T.EventTime
	WHERE T.InstanceID= @InstanceID

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
	SELECT @InstanceID,
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
	FROM @60MIN T
	WHERE NOT EXISTS(SELECT 1 FROM dbo.CPU_60MIN CPU WHERE T.EventTime = CPU.EventTime AND CPU.InstanceID = @InstanceID)

END

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID,  
									 @Reference = @Ref,
									 @SnapshotDate = @SnapshotDate

COMMIT