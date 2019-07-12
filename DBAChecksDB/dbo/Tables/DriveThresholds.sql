CREATE TABLE [dbo].[DriveThresholds] (
    [InstanceID]             INT            NOT NULL,
    [DriveID]                INT            NOT NULL,
    [DriveWarningThreshold]  DECIMAL (9, 3) NULL,
    [DriveCriticalThreshold] DECIMAL (9, 3) NULL,
    [DriveCheckType]         CHAR (1)       NOT NULL,
    CONSTRAINT [PK_DriveThresholds] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DriveID] ASC)
);



