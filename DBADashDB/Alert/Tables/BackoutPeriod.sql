CREATE TABLE Alert.BlackoutPeriod(
	BlackoutPeriodID INT IDENTITY(1,1) CONSTRAINT PK_BlackoutPeriod PRIMARY KEY,
	ApplyToTagID INT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_ApplyToTagID DEFAULT(-1),
	ApplyToInstanceID INT NULL CONSTRAINT DF_Alert_BlackoutPeriod_ApplyToInstanceID DEFAULT(-1),
	StartDate DATETIME2 NULL,
	EndDate DATETIME2 NULL CONSTRAINT CK_Alert_BlackoutPeriod_EndDate CHECK(EndDate>StartDate),
	AlertKey NVARCHAR(128) NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_AlertKey DEFAULT('%'),
	TimeFrom TIME NULL,
	TimeTo TIME NULL,
	Monday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Monday DEFAULT(CONVERT(BIT,1)),
	Tuesday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Tuesday DEFAULT(CONVERT(BIT,1)),
	Wednesday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Wednesday DEFAULT(CONVERT(BIT,1)),
	Thursday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Thursday DEFAULT(CONVERT(BIT,1)),
	Friday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Friday DEFAULT(CONVERT(BIT,1)),
	Saturday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Saturday DEFAULT(CONVERT(BIT,1)),
	Sunday BIT NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_Sunday DEFAULT(CONVERT(BIT,1)),
	TimeZone NVARCHAR(128) NOT NULL CONSTRAINT DF_Alert_BlackoutPeriod_TimeZone DEFAULT('UTC'),
	Notes NVARCHAR(MAX) NULL,
	CONSTRAINT CK_Alert_BlackoutPeriod_ApplyTo CHECK(NOT (ApplyToInstanceID>0 AND ApplyToTagID>0)),
	CONSTRAINT CK_Alert_BlackoutPeriod_TimeTo CHECK(TimeTo>TimeFrom)
	
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Alert_BlackoutPeriod_InstanceID_StartDate_EndDate_AlertKey 
	ON Alert.BlackoutPeriod(ApplyToInstanceID,ApplyToTagID,StartDate,EndDate,AlertKey,TimeFrom,TimeTo,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday,TimeZone)