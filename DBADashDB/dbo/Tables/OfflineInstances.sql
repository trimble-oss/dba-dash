CREATE TABLE dbo.OfflineInstances(
	InstanceID INT NOT NULL,
	FirstFail DATETIME2 NOT NULL,
	LastFail DATETIME2 NOT NULL,
	ClosedDate DATETIME2 NULL,
	IsCurrent BIT NOT NULL,
	FirstMessageID BIGINT NULL,
	LastMessageID BIGINT NULL,
	FailCount INT NOT NULL,
	CONSTRAINT PK_OfflineInstances PRIMARY KEY(InstanceID,FirstFail),
	CONSTRAINT FK_OfflineInstances_Instances FOREIGN KEY(InstanceID) REFERENCES dbo.Instances(InstanceID),
	CONSTRAINT FK_OfflineInstances_ErrorMessage_First FOREIGN KEY(FirstMessageID) REFERENCES dbo.ErrorMessage(ErrorMessageID),
	CONSTRAINT FK_OfflineInstances_ErrorMessage_Last FOREIGN KEY(LastMessageID) REFERENCES dbo.ErrorMessage(ErrorMessageID)
)
GO
CREATE UNIQUE NONCLUSTERED INDEX FIX_OfflineInstances_IsCurrent ON dbo.OfflineInstances(InstanceID)
INCLUDE(IsCurrent,FirstFail,LastFail,FailCount,FirstMessageID,LastMessageID,ClosedDate)
WHERE(IsCurrent=1)
GO
CREATE NONCLUSTERED INDEX IX_OfflineInstances_InstanceID_LastFail ON dbo.OfflineInstances(InstanceID,LastFail)
INCLUDE(ClosedDate,FirstMessageID,LastMessageID)