CREATE TABLE dbo.LogShippingPairs(
	PrimaryInstanceID INT NOT NULL,
	SecondaryInstanceID INT NOT NULL,
	CONSTRAINT PK_LogShippingPairs PRIMARY KEY CLUSTERED (PrimaryInstanceID, SecondaryInstanceID),
	CONSTRAINT FK_LogShippingPairs_PrimaryInstance FOREIGN KEY (PrimaryInstanceID) REFERENCES dbo.Instances (InstanceID),
	CONSTRAINT FK_LogShippingPairs_SecondaryInstance FOREIGN KEY (SecondaryInstanceID) REFERENCES dbo.Instances (InstanceID),
	CONSTRAINT CK_LogShippingPairs_DifferentInstances CHECK (PrimaryInstanceID <> SecondaryInstanceID)
)
GO
CREATE NONCLUSTERED INDEX IX_LogShippingPairs_SecondaryInstanceID ON dbo.LogShippingPairs(SecondaryInstanceID)
