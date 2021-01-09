CREATE TABLE [dbo].[Drives] (
    [DriveID]    INT            IDENTITY (1, 1) NOT NULL,
    [InstanceID] INT            NOT NULL,
    [Name]       NVARCHAR (256) NOT NULL,
    [Capacity]   BIGINT         NOT NULL,
    [FreeSpace]  BIGINT         NOT NULL,
    [UsedSpace]  AS             ([Capacity]-[FreeSpace]),
    [Label]      NVARCHAR (256) NULL,
    [IsActive]   BIT            NOT NULL,
    CONSTRAINT [PK_Drives] PRIMARY KEY CLUSTERED ([DriveID] ASC),
    CONSTRAINT [FK_Drives_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID]),
    CONSTRAINT [FK_Drives_SQLInstance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Drives_InstanceID_Name]
    ON [dbo].[Drives]([InstanceID] ASC, [Name] ASC);

