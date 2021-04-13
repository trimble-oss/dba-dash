CREATE VIEW CPU_Histogram
AS
SELECT InstanceID,
       EventTime,
       SQLProcessCPU,
       SystemIdleCPU,
       OtherCPU,
       TotalCPU,
       SampleCount,
       MaxSQLProcessCPU,
       MaxOtherProcessCPU,
       MaxTotalCPU,
       SumOtherCPU,
       SumTotalCPU,
       SumSQLProcessCPU,
	   HCPU.CPU10,
	   HCPU.CPU20,
	   HCPU.CPU30,
	   HCPU.CPU40,
	   HCPU.CPU50,
	   HCPU.CPU60,
	   HCPU.CPU70,
	   HCPU.CPU80,
	   HCPU.CPU90,
	   HCPU.CPU100
FROM dbo.CPU
CROSS APPLY dbo.Histogram_CPU(TotalCPU) HCPU