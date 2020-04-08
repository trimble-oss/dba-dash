CREATE TABLE [dbo].[AzureDBServiceObjectives] (
    [InstanceID]        INT            NOT NULL,
    [edition]           NVARCHAR (128) NOT NULL,
    [service_objective] NVARCHAR (128) NOT NULL,
    [elastic_pool_name] NVARCHAR (128) NULL,
    [ValidFrom]         DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_AzureDBServiceObjectives] PRIMARY KEY CLUSTERED ([InstanceID] ASC),
    CONSTRAINT [FK_AzureDBServiceObjectives] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

