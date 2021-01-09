CREATE TYPE [dbo].[DatabasesHADR] AS TABLE (
    [database_id]            INT              NOT NULL,
    [group_database_id]      UNIQUEIDENTIFIER NOT NULL,
    [is_primary_replica]     BIT              NULL,
    [synchronization_state]  TINYINT          NULL,
    [synchronization_health] TINYINT          NULL,
    [is_suspended]           BIT              NULL,
    [suspend_reason]         TINYINT          NULL);

