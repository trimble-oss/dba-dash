CREATE TABLE Alert.Rules(
	RuleID INT IDENTITY(1,1) NOT NULL,
	Type VARCHAR(50) NOT NULL,
	AlertKey NVARCHAR(256) NOT NULL,
	Priority TINYINT NOT NULL,
	ApplyToTagID INT NOT NULL,
	ApplyToInstanceID INT NULL,
	Threshold DECIMAL(28,9) NULL,
	EvaluationPeriodMins INT NULL,
	IsActive BIT NOT NULL,
	Details NVARCHAR(MAX) NULL,
	Notes NVARCHAR(MAX) NULL,
	ApplyToHidden BIT NOT NULL CONSTRAINT DF_Alert_Rules_ApplyToHidden DEFAULT(0),
	/* Sorted, comma-separated TagIDs when the rule applies to more than 1 tag (ApplyToTagID = -2).
	   Tags with different names must all match (AND).  Tags with the same name are alternatives (OR).  e.g. (Role:Production OR Role:UAT) AND Region:APAC
	   Resolved to instances once per alert run by Alert.ApplicableInstances_Upd */
	ApplyToTagIDs VARCHAR(800) NULL,
	RuleHash AS HASHBYTES('SHA2_256',CONCAT(Type,'|',AlertKey,'|',ApplyToTagID,'|',ApplyToInstanceID,'|',Threshold,'|',EvaluationPeriodMins,'|',IsActive,'|',Details,'|',ApplyToHidden,'|',ApplyToTagIDs)),
	GroupID INT NOT NULL CONSTRAINT DF_Alert_Rules_GroupID DEFAULT(0),
	CONSTRAINT PK_Alert_Rules PRIMARY KEY(RuleID),
	CONSTRAINT CK_Rule_Priority CHECK(Priority >= 0 AND Priority <= 41),
	CONSTRAINT CK_Rule_ApplyTo CHECK(NOT (ApplyToInstanceID IS NOT NULL AND (ApplyToTagID > 0 OR ApplyToTagID = -2))), /* Should be one or the other */
	CONSTRAINT CK_Rule_ApplyToTagIDs CHECK((ApplyToTagID = -2 AND ApplyToTagIDs IS NOT NULL) OR (ApplyToTagID <> -2 AND ApplyToTagIDs IS NULL)),
	CONSTRAINT FK_Alert_Rules_Instances FOREIGN KEY(ApplyToInstanceID) REFERENCES dbo.Instances(InstanceID)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Alert_Rules_GroupID_RuleHash ON Alert.Rules(GroupID,RuleHash)