CREATE TABLE [dbo].[AzureDBElasticPool] (
    [PoolID]            INT            IDENTITY (1, 1) NOT NULL,
    [InstanceID]        INT            NOT NULL,
    [elastic_pool_name] NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_AzureDBElasticPool] PRIMARY KEY CLUSTERED ([PoolID] ASC),
    CONSTRAINT [IX_InstanceID_elastic_pool_name] UNIQUE NONCLUSTERED ([InstanceID] ASC, [elastic_pool_name] ASC)
);

