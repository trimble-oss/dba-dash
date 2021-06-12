CREATE TYPE dbo.ResourceGovernorConfiguration AS TABLE(
	is_enabled BIT NOT NULL,
	classifier_function NVARCHAR(261) NOT NULL,
	reconfiguration_error BIT NOT NULL,
	reconfiguration_pending BIT NOT NULL,
	max_outstanding_io_per_volume INT NOT NULL,
	script NVARCHAR(MAX) NOT NULL
)