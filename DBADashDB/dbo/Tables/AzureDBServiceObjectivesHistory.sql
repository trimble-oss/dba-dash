CREATE TABLE [dbo].[AzureDBServiceObjectivesHistory] (
    [InstanceID]            INT            NOT NULL,
    [edition]               NVARCHAR (128) NOT NULL,
    [service_objective]     NVARCHAR (128) NOT NULL,
    [elastic_pool_name]     NVARCHAR (128) NULL,
    [new_edition]           NVARCHAR (128) NOT NULL,
    [new_service_objective] NVARCHAR (128) NOT NULL,
    [new_elastic_pool_name] NVARCHAR (128) NULL,
    [ValidFrom]             DATETIME2 (2)  NOT NULL,
    [ValidTo]               DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_AzureDBServiceObjectivesHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [ValidTo] ASC)
);

