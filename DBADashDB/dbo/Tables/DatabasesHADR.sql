CREATE TABLE [dbo].[DatabasesHADR] (
    [DatabaseID]             INT              NOT NULL,
    [group_database_id]      UNIQUEIDENTIFIER NOT NULL,
    [is_primary_replica]     BIT              NULL,
    [synchronization_state]  TINYINT          NULL,
    [synchronization_health] TINYINT          NULL,
    [is_suspended]           BIT              NULL,
    [suspend_reason]         TINYINT          NULL,
    CONSTRAINT [PK_DatabasesHADR] PRIMARY KEY CLUSTERED ([DatabaseID] ASC),
    CONSTRAINT [FK_DatabasesHADR] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID]),
    CONSTRAINT [FK_DatabasesHADR_Databases] FOREIGN KEY ([DatabaseID]) REFERENCES [dbo].[Databases] ([DatabaseID])
);




GO
CREATE NONCLUSTERED INDEX [IX_DatabasesHADR_group_database_id]
    ON [dbo].[DatabasesHADR]([group_database_id] ASC, [DatabaseID] ASC);

