CREATE VIEW dbo.PerformanceThresholds
AS
SELECT Pvt.CPUCriticalThreshold,
       Pvt.CPULowThreshold,
       Pvt.CPUWarningThreshold,
       Pvt.ReadLatencyCriticalThreshold,
       Pvt.ReadLatencyWarningThreshold,
       Pvt.ReadLatencyGoodThreshold,
       Pvt.WriteLatencyCriticalThreshold,
       Pvt.WriteLatencyWarningThreshold,
       Pvt.WriteLatencyGoodThreshold,
       Pvt.MinIOPsThreshold,
	   Pvt.CriticalWaitCriticalThreshold,
	   Pvt.CriticalWaitWarningThreshold
FROM dbo.Settings
PIVOT(
	MAX(SettingValue) 
	FOR SettingName IN(
						CPUCriticalThreshold,
						CPULowThreshold,
						CPUWarningThreshold,
						ReadLatencyCriticalThreshold,
						ReadLatencyWarningThreshold,
						ReadLatencyGoodThreshold,
						WriteLatencyCriticalThreshold,
						WriteLatencyWarningThreshold,
						WriteLatencyGoodThreshold,
						MinIOPsThreshold,
						CriticalWaitCriticalThreshold,
						CriticalWaitWarningThreshold
						)
	) Pvt