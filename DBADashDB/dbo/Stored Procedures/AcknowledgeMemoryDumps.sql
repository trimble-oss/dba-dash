CREATE PROC dbo.AcknowledgeMemoryDumps(
	@Clear BIT=0
)
AS
DELETE dbo.Settings
WHERE SettingName = 'MemoryDumpAckDate'

IF @Clear<>1
BEGIN
	INSERT INTO dbo.Settings(SettingName,SettingValue)
	VALUES('MemoryDumpAckDate',GETUTCDATE())
END