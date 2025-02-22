CREATE TABLE Alert.Rules(
	RuleID INT IDENTITY(1,1) NOT NULL,
	Type VARCHAR(50) NOT NULL,
	AlertKey NVARCHAR(140) NOT NULL,
	Priority TINYINT NOT NULL,
	ApplyToTagID INT NOT NULL,
	ApplyToInstanceID INT NULL,
	Threshold DECIMAL(28,9) NULL,
	EvaluationPeriodMins INT NULL,
	IsActive BIT NOT NULL,
	Details NVARCHAR(MAX) NULL,
	Notes NVARCHAR(MAX) NULL,
	ApplyToHidden BIT NOT NULL CONSTRAINT DF_Alert_Rules_ApplyToHidden DEFAULT(0),
	RuleHash AS HASHBYTES('SHA2_256',CONCAT(Type,'|',AlertKey,'|',ApplyToTagID,'|',ApplyToInstanceID,'|',Threshold,'|',EvaluationPeriodMins,'|',IsActive,'|',Details,'|',ApplyToHidden)),
	CONSTRAINT PK_Alert_Rules PRIMARY KEY(RuleID),
	CONSTRAINT CK_Rule_Priority CHECK(Priority BETWEEN 0 AND 41),
	CONSTRAINT CK_Rule_ApplyTo CHECK(NOT (ApplyToInstanceID IS NOT NULL AND ApplyToTagID > 0)), /* Should be one or the other */
	CONSTRAINT FK_Alert_Rules_Instances FOREIGN KEY(ApplyToInstanceID) REFERENCES dbo.Instances(InstanceID)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Alert_Rules_RuleHash ON Alert.Rules(RuleHash)