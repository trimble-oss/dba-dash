CREATE TABLE dbo.InstanceIDsTags(
	InstanceID INT NOT NULL,
	TagID INT NOT NULL,
	CONSTRAINT PK_InstanceIDsTags PRIMARY KEY CLUSTERED(InstanceID,TagID),
	CONSTRAINT FK_InstanceIDsTags_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX InstanceIDsTags_TagID_InstanceID ON InstanceIDsTags(InstanceID,TagID)