CREATE TABLE [dbo].[HostUpgradeHistory] (
    [InstanceID]             INT            NOT NULL,
    [ChangeDate]             DATETIME2 (2)  NOT NULL,
    [cores_per_socket_old]   INT            NULL,
    [cores_per_socket_new]   INT            NULL,
    [cpu_count_old]          INT            NULL,
    [cpu_count_new]          INT            NULL,
    [hyperthread_ratio_old]  INT            NULL,
    [hyperthread_ratio_new]  INT            NULL,
    [physical_memory_kb_old] BIGINT         NULL,
    [physical_memory_kb_new] BIGINT         NULL,
    [socket_count_old]       INT            NULL,
    [socket_count_new]       INT            NULL,
    [Processor_old]          NVARCHAR (512) NULL,
    [Processor_new]          NVARCHAR (512) NULL,
    [SystemManufacturerOld]  NVARCHAR (512) NULL,
    [SystemManufacturerNew]  NVARCHAR (512) NULL,
    [SystemProductNameOld]   NVARCHAR (512) NULL,
    [SystemProductNameNew]   NVARCHAR (512) NULL,
    CONSTRAINT [PK_HostUpgradeHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [ChangeDate] ASC),
    CONSTRAINT [FK_HostUpgradeHistory] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);

