CREATE TABLE dbo.AvailabilityGroupMetricsConfig(
	InstanceID INT NOT NULL,
	MetricName NVARCHAR(128) NOT NULL,
	IsAggregate BIT NOT NULL,
	IsEnabled BIT NOT NULL,
	CONSTRAINT PK_AvailabilityGroupMetricsConfig PRIMARY KEY CLUSTERED(InstanceID,MetricName)
)
