CREATE TABLE dbo.ResourceGovernorConfigurationHistory(
	InstanceID INT NOT NULL,
	is_enabled BIT NOT NULL,
	classifier_function NVARCHAR(261) NOT NULL,
	reconfiguration_error BIT NOT NULL,
	reconfiguration_pending BIT NOT NULL,
	max_outstanding_io_per_volume INT NOT NULL,
	script NVARCHAR(MAX) NOT NULL,
	ValidFrom DATETIME2(2) NOT NULL,
	ValidTo DATETIME2(2) NOT NULL,
	CONSTRAINT PK_ResourceGovernorConfiguration PRIMARY KEY(InstanceID,ValidTo),
	CONSTRAINT FK_ResourceGovernorConfiguration_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
GO