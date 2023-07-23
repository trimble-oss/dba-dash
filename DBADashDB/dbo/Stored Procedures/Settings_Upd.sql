CREATE PROC dbo.Settings_Upd(
	@SettingName VARCHAR(50),
	@SettingValue SQL_VARIANT
)
AS
IF @SettingValue IS NULL
BEGIN
	DELETE dbo.Settings 
	WHERE SettingName = @SettingName
END
ELSE
BEGIN
	UPDATE dbo.Settings
		SET SettingValue = @SettingValue 
	WHERE SettingName = @SettingName

	IF @@ROWCOUNT=0
	BEGIN
		INSERT INTO dbo.Settings(SettingName,SettingValue)
		VALUES(@SettingName,@SettingValue)
	END
END