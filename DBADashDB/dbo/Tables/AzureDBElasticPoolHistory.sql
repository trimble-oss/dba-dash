CREATE TABLE [dbo].[AzureDBElasticPoolHistory] (
    [PoolID]                     INT            NOT NULL,
    [elastic_pool_dtu_limit_old] INT            NULL,
    [elastic_pool_cpu_limit_old] DECIMAL (5, 2) NULL,
    [elastic_pool_dtu_limit_new] INT            NULL,
    [elastic_pool_cpu_limit_new] DECIMAL (5, 2) NULL,
    [ValidFrom]                  DATETIME2 (2)  NOT NULL,
    [ValidTo]                    DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_AzureDBElasticPoolHistory] PRIMARY KEY CLUSTERED ([PoolID] ASC, [ValidTo] ASC)
);

