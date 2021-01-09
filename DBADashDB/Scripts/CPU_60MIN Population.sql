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
GROUP BY InstanceID,
	CONVERT(DATETIME,SUBSTRING(CONVERT(VARCHAR,EventTime,120),0,14) + ':00',120) 