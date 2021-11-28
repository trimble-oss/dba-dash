CREATE PROC dbo.MemoryDumpThresholds_Upd(
	@MemoryDumpWarningThresholdHrs INT,
	@MemoryDumpCriticalThresholdHrs INT
)
AS
UPDATE dbo.Settings 
SET SettingValue = @MemoryDumpWarningThresholdHrs 
WHERE SettingName = 'MemoryDumpWarningThresholdHrs';

UPDATE dbo.Settings 
SET SettingValue = @MemoryDumpCriticalThresholdHrs
WHERE SettingName = 'MemoryDumpCriticalThresholdHrs';