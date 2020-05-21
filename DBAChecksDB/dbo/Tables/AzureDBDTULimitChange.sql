CREATE TABLE [dbo].[AzureDBDTULimitChange] (
    [InstanceID]    INT           NOT NULL,
    [ChangeDate]    DATETIME2 (3) NOT NULL,
    [dtu_limit_new] INT           NOT NULL,
    [dtu_limit_old] INT           NOT NULL,
    CONSTRAINT [PK_AzureDBDTULimitChange] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [ChangeDate] ASC),
    CONSTRAINT [FK_AzureDBDTULimitChange_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

