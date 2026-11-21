/* Elastic jobs that failed recently, as of the previous run of Alert.ElasticJobAlert_Upd - so a failure only raises an alert once */
CREATE TABLE Alert.ElasticJobSnapshot(
	InstanceID INT NOT NULL,
	job_id UNIQUEIDENTIFIER NOT NULL,
	LastFailed DATETIME2 NULL,
	LastSucceeded DATETIME2 NULL,
	CONSTRAINT PK_Alert_ElasticJobSnapshot PRIMARY KEY(InstanceID,job_id)
)
