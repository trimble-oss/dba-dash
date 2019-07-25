CREATE TABLE [dbo].[AgentJobs] (
    [InstanceID]                 INT              NOT NULL,
    [job_id]                     UNIQUEIDENTIFIER NOT NULL,
    [name]                       [sysname]        NOT NULL,
    [LastFail]                   DATETIME         NULL,
    [LastSucceed]                DATETIME         NULL,
    [FailCount24Hrs]             INT              NOT NULL,
    [SucceedCount24Hrs]          INT              NOT NULL,
    [FailCount7Days]             INT              NOT NULL,
    [SucceedCount7Days]          INT              NOT NULL,
    [JobStepFails7Days]          INT              NOT NULL,
    [JobStepFails24Hrs]          INT              NOT NULL,
    [enabled]                    TINYINT          NOT NULL,
    [MaxDurationSec]             INT              NULL,
    [AvgDurationSec]             INT              NULL,
    [IsLastFail]                 BIT              NOT NULL,
    [start_step_id]              INT              NOT NULL,
    [category_id]                INT              NOT NULL,
    [owner_sid]                  VARBINARY (85)   NOT NULL,
    [notify_email_operator_id]   INT              NOT NULL,
    [notify_netsend_operator_id] INT              NOT NULL,
    [notify_page_operator_id]    INT              NOT NULL,
    [notify_level_eventlog]      INT              NOT NULL,
    [notify_level_email]         INT              NOT NULL,
    [notify_level_netsend]       INT              NOT NULL,
    [notify_level_page]          INT              NOT NULL,
    [date_created]               DATETIME         NOT NULL,
    [date_modified]              DATETIME         NOT NULL,
    [Description]                NVARCHAR (512)   NULL,
    [delete_level]               INT              NOT NULL,
    [version_number]             INT              NOT NULL,
    CONSTRAINT [PK_AgentJobs] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [job_id] ASC),
    CONSTRAINT [FK_AgentJobs_Instance] FOREIGN KEY ([InstanceID]) REFERENCES [dbo].[Instances] ([InstanceID])
);





