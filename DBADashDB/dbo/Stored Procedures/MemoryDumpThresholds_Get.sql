CREATE PROC dbo.MemoryDumpThresholds_Get(
	@MemoryDumpWarningThresholdHrs INT=NULL OUT,
	@MemoryDumpCriticalThresholdHrs INT=NULL OUT,
	@MemoryDumpAckDate DATETIME=NULL OUT
)
AS
SELECT @MemoryDumpWarningThresholdHrs = CONVERT(INT,SettingValue)
FROM dbo.Settings 
WHERE SettingName = 'MemoryDumpWarningThresholdHrs';

SELECT @MemoryDumpCriticalThresholdHrs = CONVERT(INT,SettingValue)
FROM dbo.Settings 
WHERE SettingName = 'MemoryDumpCriticalThresholdHrs';

SELECT @MemoryDumpAckDate = CONVERT(DATETIME,SettingValue)
FROM dbo.Settings 
WHERE SettingName = 'MemoryDumpAckDate';