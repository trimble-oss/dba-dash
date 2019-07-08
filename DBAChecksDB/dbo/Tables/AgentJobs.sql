CREATE TABLE [dbo].[AgentJobs] (
    [InstanceID]        INT              NOT NULL,
    [job_id]            UNIQUEIDENTIFIER NOT NULL,
    [name]              [sysname]        NOT NULL,
    [LastFail]          DATETIME         NULL,
    [LastSucceed]       DATETIME         NULL,
    [FailCount24Hrs]    INT              NOT NULL,
    [SucceedCount24Hrs] INT              NOT NULL,
    [FailCount7Days]    INT              NOT NULL,
    [SucceedCount7Days] INT              NOT NULL,
    [JobStepFails7Days] INT              NOT NULL,
    [JobStepFails24Hrs] INT              NOT NULL,
    [enabled]           TINYINT          NOT NULL,
    [MaxDurationSec]    INT              NULL,
    [AvgDurationSec]    INT              NULL,
    [IsLastFail]        BIT              NOT NULL
);

