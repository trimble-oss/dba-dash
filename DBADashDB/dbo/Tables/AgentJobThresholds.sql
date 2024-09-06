CREATE TABLE dbo.AgentJobThresholds(
    InstanceID INT NOT NULL,
    job_id UNIQUEIDENTIFIER NOT NULL,
    TimeSinceLastFailureWarning INT NULL,
    TimeSinceLastFailureCritical INT NULL,
    TimeSinceLastSucceededWarning INT NULL,
    TimeSinceLastSucceededCritical INT NULL,
    FailCount24HrsWarning INT NULL,
    FailCount24HrsCritical INT NULL,
    FailCount7DaysCritical INT NULL,
    FailCount7DaysWarning INT NULL,
    JobStepFails24HrsWarning INT NULL,
    JobStepFails24HrsCritical INT NULL,
    JobStepFails7DaysWarning INT NULL,
    JobStepFails7DaysCritical INT NULL,
    LastFailIsCritical BIT NOT NULL,
    LastFailIsWarning BIT NOT NULL,
    AgentIsRunningCheck BIT NOT NULL CONSTRAINT DF_AgentIsRunningCheck DEFAULT (1),
    CONSTRAINT PK_AgentJobThresholds PRIMARY KEY CLUSTERED (InstanceID ASC, job_id ASC)
);


