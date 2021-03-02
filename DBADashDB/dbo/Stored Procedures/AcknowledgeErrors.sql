CREATE PROC dbo.AcknowledgeErrors
AS
DELETE dbo.Settings
WHERE SettingName = 'ErrorAckDate'

INSERT INTO dbo.Settings(SettingName,SettingValue)
VALUES('ErrorAckDate',GETUTCDATE())