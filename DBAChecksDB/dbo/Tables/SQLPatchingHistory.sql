CREATE TABLE [dbo].[SQLPatchingHistory] (
    [InstanceID]            INT            NOT NULL,
    [ChangedDate]           DATETIME2 (2)  NOT NULL,
    [OldVersion]            NVARCHAR (128) NULL,
    [NewVersion]            NVARCHAR (128) NULL,
    [OldProductLevel]       NVARCHAR (128) NULL,
    [NewProductLevel]       NVARCHAR (128) NULL,
    [OldProductUpdateLevel] NVARCHAR (128) NULL,
    [NewProductUpdateLevel] NVARCHAR (128) NULL,
    CONSTRAINT [PK_SQLPatchingHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [ChangedDate] ASC),
    CONSTRAINT [FK_SQLVersionHistory_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

