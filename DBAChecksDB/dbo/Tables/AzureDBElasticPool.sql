CREATE TABLE [dbo].[AzureDBElasticPool] (
    [PoolID]                 INT            IDENTITY (1, 1) NOT NULL,
    [InstanceID]             INT            NOT NULL,
    [elastic_pool_name]      NVARCHAR (128) NOT NULL,
    [elastic_pool_dtu_limit] INT            NULL,
    [elastic_pool_cpu_limit] DECIMAL (5, 2) NULL,
    [ValidFrom]              DATETIME2 (2)  NULL,
    CONSTRAINT [PK_AzureDBElasticPool] PRIMARY KEY CLUSTERED ([PoolID] ASC),
    CONSTRAINT [IX_InstanceID_elastic_pool_name] UNIQUE NONCLUSTERED ([InstanceID] ASC, [elastic_pool_name] ASC)
);



