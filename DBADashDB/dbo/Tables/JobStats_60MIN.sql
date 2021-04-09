CREATE TABLE [dbo].[JobStats_60MIN] (
    [InstanceID]        INT              NOT NULL,
    [job_id]            UNIQUEIDENTIFIER NOT NULL,
    [step_id]           INT              NOT NULL,
    [RunDateTime]       DATETIME2 (2)    NOT NULL,
    [FailedCount]       INT              NOT NULL,
    [SucceededCount]    INT              NOT NULL,
    [RetryCount]        INT              NOT NULL,
    [RunDurationSec]    INT              NOT NULL,
    [MaxRunDurationSec] INT              NOT NULL,
    [MinRunDurationSec] INT              NOT NULL,
    CONSTRAINT [PK_JobStats_60MIN] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [job_id] ASC, [step_id] ASC, [RunDateTime] ASC)
) ON PS_JobStats_60MIN(RunDateTime)

