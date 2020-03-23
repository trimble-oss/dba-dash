CREATE TABLE [dbo].[DBTuningOptionsHistory] (
    [InstanceID]             INT            NOT NULL,
    [DatabaseID]             INT            NOT NULL,
    [name]                   NVARCHAR (128) NOT NULL,
    [desired_state_desc]     NVARCHAR (60)  NULL,
    [actual_state_desc]      NVARCHAR (60)  NULL,
    [reason_desc]            NVARCHAR (60)  NULL,
    [new_desired_state_desc] NVARCHAR (60)  NULL,
    [new_actual_state_desc]  NVARCHAR (60)  NULL,
    [new_reason_desc]        NVARCHAR (60)  NULL,
    [ValidFrom]              DATETIME2 (2)  NOT NULL,
    [ValidTo]                DATETIME2 (2)  NOT NULL,
    CONSTRAINT [PK_DBTuningOptionsHistory] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [DatabaseID] ASC, [name] ASC, [ValidTo] ASC)
);

