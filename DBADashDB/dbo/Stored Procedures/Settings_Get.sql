CREATE PROC dbo.Settings_Get(
	@SettingName VARCHAR(50)=NULL
)
AS
IF @SettingName IS NULL
BEGIN
	SELECT SettingName, SettingValue 
	FROM dbo.Settings
END
ELSE
BEGIN
	SELECT SettingName, SettingValue 
	FROM dbo.Settings
	WHERE SettingName = @SettingName
END
