CREATE TYPE [dbo].[AzureDBServiceObjectives] AS TABLE (
    [edition]           NVARCHAR (128) NOT NULL,
    [service_objective] NVARCHAR (128) NOT NULL,
    [elastic_pool_name] NVARCHAR (128) NULL);

