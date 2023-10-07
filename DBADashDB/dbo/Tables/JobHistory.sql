CREATE TABLE [dbo].[JobHistory] (
    [InstanceID]          INT              NOT NULL,
    [instance_id]         INT              NOT NULL,
    [job_id]              UNIQUEIDENTIFIER NOT NULL,
    [step_id]             INT              NOT NULL,
    [step_name]           NVARCHAR (128)   NULL,
    [sql_message_id]      INT              NULL,
    [sql_severity]        INT              NULL,
    [message]             NVARCHAR (4000)  NULL,
    [run_status]          INT              NULL,
    [RunDateTime]         DATETIME2 (2)    NOT NULL,
    [RunDurationSec]      INT              NULL,
    [operator_id_emailed] INT              NULL,
    [operator_id_netsent] INT              NULL,
    [operator_id_paged]   INT              NULL,
    [retries_attempted]   INT              NULL,
    [server]              NVARCHAR (128)   NULL,
    FinishDateTime AS DATEADD(s,RunDurationSec,RunDateTime),
    CONSTRAINT [PK_JobHistory_InstanceID_instance_id] PRIMARY KEY NONCLUSTERED ([InstanceID] ASC, [instance_id] ASC, [RunDateTime] ASC),
    CONSTRAINT [FK_Instances] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
) ON PS_JobHistory(RunDateTime);
GO
CREATE CLUSTERED INDEX IX_JobHistory_InstanceID_job_id_step_id_RunDateTime
ON dbo.JobHistory (InstanceID,job_id,step_id,RunDateTime);