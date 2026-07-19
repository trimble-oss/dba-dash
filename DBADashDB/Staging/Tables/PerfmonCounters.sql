/*
	Holds the previous raw sample of each perfmon counter per instance, so dbo.PerfmonCounters_Upd
	can cook rate / timer / average values from the delta across collections.  Dedicated to perfmon
	(separate from Staging.PerformanceCounters) so the two collections never touch each other's rows.
*/
CREATE TABLE [Staging].[PerfmonCounters] (
    [InstanceID]    INT             NOT NULL,
    [object_name]   NCHAR (128)     NOT NULL,
    [counter_name]  NCHAR (128)     NOT NULL,
    [instance_name] NCHAR (128)     NOT NULL,
    [cntr_value]    DECIMAL (28, 9) NOT NULL,
    [cntr_type]     INT             NOT NULL,
    [timebase]      DECIMAL (28, 9) NOT NULL CONSTRAINT [DF_PerfmonCounters_Staging_timebase] DEFAULT (0),
    [SnapshotDate]  DATETIME2 (7)   NOT NULL,
    CONSTRAINT [PK_PerfmonCounters_Staging] PRIMARY KEY CLUSTERED ([InstanceID] ASC, [object_name] ASC, [counter_name] ASC, [instance_name] ASC)
);
