/*
	TVP used to pass OS-level (perfmon) RAW performance counters collected from the host via WMI into
	dbo.PerfmonCounters_Upd.  Distinct from the DMV performance counters (dbo.PerformanceCounters).

	timebase carries the counter's own Timestamp_Sys100NS (100ns units) for 100ns-timer counters, so
	their % is cooked against the exact interval WMI sampled over rather than wall-clock time.  0 for
	other counter types (which don't need it).
*/
CREATE TYPE dbo.PerfmonCounters AS TABLE (
    [SnapshotDate]  DATETIME2 (7)   NOT NULL,
    [object_name]   NCHAR (128)     NOT NULL,
    [counter_name]  NCHAR (128)     NOT NULL,
    [instance_name] NCHAR (128)     NOT NULL,
    [cntr_value]    DECIMAL (28, 9) NOT NULL,
    [cntr_type]     INT             NOT NULL DEFAULT (65792),
    [timebase]      DECIMAL (28, 9) NOT NULL DEFAULT (0),
    /* Stable WMI identity - persisted on dbo.Counters so the app can key off it rather than the
       (object_name, counter_name) display names.  Base rows carry the base property here. */
    [WmiClass]      NVARCHAR (128)  NULL,
    [WmiProperty]   NVARCHAR (128)  NULL
);
