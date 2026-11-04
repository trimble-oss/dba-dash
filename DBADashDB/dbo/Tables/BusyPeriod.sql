CREATE TABLE dbo.BusyPeriod(
	BusyPeriodID INT IDENTITY(1,1) CONSTRAINT PK_BusyPeriod PRIMARY KEY,
	ApplyToInstanceID INT NOT NULL CONSTRAINT DF_BusyPeriod_ApplyToInstanceID DEFAULT(-1),
	ApplyToTagID INT NOT NULL CONSTRAINT DF_BusyPeriod_ApplyToTagID DEFAULT(-1),
	Monday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Monday DEFAULT(CONVERT(BIT,1)),
	Tuesday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Tuesday DEFAULT(CONVERT(BIT,1)),
	Wednesday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Wednesday DEFAULT(CONVERT(BIT,1)),
	Thursday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Thursday DEFAULT(CONVERT(BIT,1)),
	Friday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Friday DEFAULT(CONVERT(BIT,1)),
	Saturday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Saturday DEFAULT(CONVERT(BIT,1)),
	Sunday BIT NOT NULL CONSTRAINT DF_BusyPeriod_Sunday DEFAULT(CONVERT(BIT,1)),
	TimeFrom TIME NOT NULL,
	TimeTo TIME NOT NULL,
	TimeZone NVARCHAR(128) NOT NULL CONSTRAINT DF_BusyPeriod_TimeZone DEFAULT('UTC'),
	Description NVARCHAR(256) NULL,
	/* Foreign key target for ApplyToInstanceID. NULL unless the period applies to one instance, so the -1 "all" value doesn't need an Instances row. */
	ApplyToInstanceIDRef AS CASE WHEN ApplyToInstanceID > 0 THEN ApplyToInstanceID END PERSISTED,
	CONSTRAINT CK_BusyPeriod_ApplyTo CHECK(NOT (ApplyToInstanceID > 0 AND ApplyToTagID > 0)),
	/* TimeTo earlier than TimeFrom means the period crosses midnight. Equal times would be an empty period. */
	CONSTRAINT CK_BusyPeriod_Times CHECK(TimeFrom <> TimeTo),
	CONSTRAINT FK_BusyPeriod_Instances FOREIGN KEY (ApplyToInstanceIDRef) REFERENCES dbo.Instances(InstanceID),
	CONSTRAINT FK_BusyPeriod_Tags FOREIGN KEY (ApplyToTagID) REFERENCES dbo.Tags(TagID)
)
