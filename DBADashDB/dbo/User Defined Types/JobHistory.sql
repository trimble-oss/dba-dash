CREATE TYPE [dbo].[JobHistory] AS TABLE (
    [instance_id]         INT              NULL,
    [job_id]              UNIQUEIDENTIFIER NULL,
    [step_id]             INT              NULL,
    [step_name]           NVARCHAR (128)   NULL,
    [sql_message_id]      INT              NULL,
    [sql_severity]        INT              NULL,
    [message]             NVARCHAR (4000)  NULL,
    [run_status]          INT              NULL,
    [run_date]            INT              NULL,
    [run_time]            INT              NULL,
    [run_duration]        INT              NULL,
    [operator_id_emailed] INT              NULL,
    [operator_id_netsent] INT              NULL,
    [operator_id_paged]   INT              NULL,
    [retries_attempted]   INT              NULL,
    [server]              NVARCHAR (128)   NULL
);


