CREATE TABLE dbo.RepositoryMetricsConfig(
	InstanceID INT NOT NULL,
	MetricName NVARCHAR(128) NOT NULL,
	IsAggregate BIT NOT NULL,
	IsEnabled BIT NOT NULL,
	MetricType VARCHAR(20) NOT NULL CONSTRAINT DF_RepositoryMetricsConfig_MetricType DEFAULT('AG'),
	CONSTRAINT PK_RepositoryMetricsConfig PRIMARY KEY CLUSTERED(InstanceID,MetricType,MetricName)
)
