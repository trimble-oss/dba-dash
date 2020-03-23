CREATE TYPE [dbo].[DBTuningOptions] AS TABLE (
    [database_id]        INT            NOT NULL,
    [name]               NVARCHAR (128) NOT NULL,
    [desired_state_desc] NVARCHAR (60)  NULL,
    [actual_state_desc]  NVARCHAR (60)  NULL,
    [reason_desc]        NVARCHAR (60)  NULL,
    PRIMARY KEY CLUSTERED ([database_id] ASC, [name] ASC));

