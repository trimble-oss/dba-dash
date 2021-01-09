CREATE TABLE [dbo].[AgentJobThresholds] (
    [InstanceId]                     INT              NOT NULL,
    [job_id]                         UNIQUEIDENTIFIER NOT NULL,
    [TimeSinceLastFailureWarning]    INT              NULL,
    [TimeSinceLastFailureCritical]   INT              NULL,
    [TimeSinceLastSucceededWarning]  INT              NULL,
    [TimeSinceLastSucceededCritical] INT              NULL,
    [FailCount24HrsWarning]          INT              NULL,
    [FailCount24HrsCritical]         INT              NULL,
    [FailCount7DaysCritical]         INT              NULL,
    [FailCount7DaysWarning]          INT              NULL,
    [JobStepFails24HrsWarning]       INT              NULL,
    [JobStepFails24HrsCritical]      INT              NULL,
    [JobStepFails7DaysWarning]       INT              NULL,
    [JobStepFails7DaysCritical]      INT              NULL,
    [LastFailIsCritical]             BIT              NOT NULL,
    [LastFailIsWarning]              BIT              NOT NULL,
    CONSTRAINT [PK_AgentJobThresholds] PRIMARY KEY CLUSTERED ([InstanceId] ASC, [job_id] ASC)
);



