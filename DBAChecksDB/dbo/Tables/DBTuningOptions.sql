CREATE TABLE [dbo].[DBTuningOptions] (
    [InstanceID]         INT            NOT NULL,
    [DatabaseID]         INT            NOT NULL,
    [name]               NVARCHAR (128) NOT NULL,
    [desired_state_desc] NVARCHAR (60)  NULL,
    [actual_state_desc]  NVARCHAR (60)  NULL,
    [reason_desc]        NVARCHAR (60)  NULL,
    [ValidFrom]          DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_DBTuningOptions] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC, [name] ASC),
    CONSTRAINT [FK_DBTuningOptions_Database] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    CONSTRAINT [FK_DBTuningOptions_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

