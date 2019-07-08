CREATE TABLE [dbo].[DriveSnapshot] (
    [SnapshotDate] DATETIME2 (2) NOT NULL,
    [DriveID]      INT           NOT NULL,
    [Capacity]     BIGINT        NOT NULL,
    [FreeSpace]    BIGINT        NOT NULL,
    [UsedSpace]    AS            ([Capacity]-[FreeSpace]),
    CONSTRAINT [PK_DriveSnapshot] PRIMARY KEY CLUSTERED ([SnapshotDate] ASC, [DriveID] ASC),
    CONSTRAINT [FK_DriveSnapshot_Drive] FOREIGN KEY ([DriveID]) REFERENCES [dbo].[Drives] ([DriveID])
);

