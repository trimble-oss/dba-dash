/*
	Overrides for when a collection's runs count as a warning or critical - see dbo.CollectionHealthStatus.

	InstanceID -1 is every instance, and Reference '' every collection.  The steps that follow some collections (Text, Plans...)
	use the collection's thresholds.  Each column is resolved on its own, so a row can override one threshold and inherit the
	rest: NULL means the next level up - this instance's row for every collection, then the collection's row for every instance,
	then the row for everything, then the built-in defaults (dbo.CollectionHealthThresholdDefaults).
*/
CREATE TABLE dbo.CollectionHealthThresholds(
	InstanceID INT NOT NULL,
	Reference VARCHAR(100) NOT NULL,
	WarningConsecutiveFailures INT NULL,
	WarningFailingMinutes INT NULL,
	CriticalConsecutiveFailures INT NULL,
	CriticalFailingMinutes INT NULL,
	WarningFailedPct DECIMAL(5,2) NULL,
	CriticalFailedPct DECIMAL(5,2) NULL,
	MinRunsForFailedPct INT NULL,
	Disabled BIT NULL,
	CONSTRAINT PK_CollectionHealthThresholds PRIMARY KEY CLUSTERED(InstanceID,Reference),
	CONSTRAINT CK_CollectionHealthThresholds_ConsecutiveFailures CHECK(WarningConsecutiveFailures >= 1 AND CriticalConsecutiveFailures >= 1),
	CONSTRAINT CK_CollectionHealthThresholds_FailingMinutes CHECK(WarningFailingMinutes >= 0 AND CriticalFailingMinutes >= 0),
	CONSTRAINT CK_CollectionHealthThresholds_FailedPct CHECK(WarningFailedPct BETWEEN 0 AND 100 AND CriticalFailedPct BETWEEN 0 AND 100),
	CONSTRAINT CK_CollectionHealthThresholds_MinRuns CHECK(MinRunsForFailedPct >= 1)
);
