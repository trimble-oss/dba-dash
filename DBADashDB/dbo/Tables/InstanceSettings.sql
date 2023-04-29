CREATE TABLE dbo.InstanceSettings(
	InstanceID INT NOT NULL CONSTRAINT FK_Instance_InstanceSettings FOREIGN KEY REFERENCES dbo.Instances(InstanceID),
	SettingName VARCHAR(50) NOT NULL,
	SettingValue SQL_VARIANT  NOT NULL,
	CONSTRAINT PK_InstanceSettings PRIMARY KEY(InstanceID,SettingName)
)
