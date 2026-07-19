/*
	Imports OS-level (perfmon) performance counters collected from the host via WMI - the RAW
	(Win32_PerfRawData_*) classes.  Raw accumulators are cooked here over the collection interval
	(delta vs the previous sample in Staging.PerfmonCounters), which is more accurate than the cooked
	Win32_PerfFormattedData classes (true interval average vs an aliased point sample) and preserves
	fractional values such as disk latency.

	The collector normalises each WMI CounterType to one of the types handled below and, for
	fraction / average-timer counters, emits an extra "<counter> Base" row (cntr_type 1073939712):

	  65792      PERF_COUNTER_LARGE_RAWCOUNT   -> value as-is (queue lengths etc.)
	  272696576  PERF_COUNTER_BULK_COUNT       -> (Δvalue)/Δseconds (rates)
	  537003264  PERF_LARGE_RAW_FRACTION       -> value*100/base (% usage)
	  542180608  PERF_100NSEC_TIMER            -> 100*Δvalue/Δtimebase (% privileged time)
	  558957824  PERF_100NSEC_TIMER_INV        -> 100*(1 - Δvalue/Δtimebase) (% processor time)
	                                              (timebase = the counter's Timestamp_Sys100NS, 100ns units)
	  805438464  PERF_AVERAGE_TIMER            -> (Δvalue/1e7)/Δbase, value pre-scaled to 100ns units
	                                              by the collector (disk latency, seconds)

	Values land in the shared dbo.PerformanceCounters / _60MIN tables (unified reporting).  Collection
	dates are recorded under a distinct reference.
*/
CREATE PROC dbo.PerfmonCounters_Upd (
    @InstanceID INT,
    @SnapshotDate DATETIME2 (7),
    @PerfmonCounters dbo.PerfmonCounters READONLY
)
AS
SET XACT_ABORT ON;
DECLARE @Ref NVARCHAR(128) = N'PerfmonCounters';

/* Metric counter types (i.e. not the base rows) */
DECLARE @MetricTypes TABLE (cntr_type INT PRIMARY KEY);
INSERT INTO @MetricTypes VALUES (65792), (272696576), (537003264), (542180608), (558957824), (805438464);

/* --- Register any new counters / instance associations (serialised via app lock; see
       PerformanceCounters_Upd for the rationale).  Base rows are excluded - they are not metrics. --- */
/* Including WmiClass / WmiProperty in the EXCEPT means this fires both for a brand new counter and for an
   existing row whose WMI identity doesn't yet match (e.g. NULL on rows created before the columns existed) -
   so no separate probe is needed.  Read-only on the hot path; the writes below only run when this is true. */
IF EXISTS (
        SELECT RTRIM(object_name), RTRIM(counter_name), RTRIM(instance_name), WmiClass, WmiProperty
        FROM @PerfmonCounters WHERE cntr_type <> 1073939712
        EXCEPT
        SELECT object_name, counter_name, instance_name, WmiClass, WmiProperty FROM dbo.Counters
    )
    OR EXISTS (
        SELECT @InstanceID, C.CounterID
        FROM @PerfmonCounters pc
        JOIN dbo.Counters C ON C.counter_name = pc.counter_name AND C.instance_name = pc.instance_name AND C.object_name = pc.object_name
        WHERE pc.cntr_type <> 1073939712
        EXCEPT
        SELECT InstanceID, CounterID FROM dbo.InstanceCounters WHERE InstanceID = @InstanceID
    )
BEGIN
    DECLARE @LockResult INT;
    BEGIN TRAN;
        EXEC @LockResult = sp_getapplock @Resource = 'DBADash_CounterRegistration',
                                         @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
        IF @LockResult >= 0
        BEGIN
            /* New counters carry their stable WMI identity immediately (deduped by name only, so a differing
               WMI value never creates a duplicate-name row). */
            INSERT INTO dbo.Counters (object_name, counter_name, instance_name, WmiClass, WmiProperty)
            SELECT DISTINCT RTRIM(pc.object_name), RTRIM(pc.counter_name), RTRIM(pc.instance_name), pc.WmiClass, pc.WmiProperty
            FROM @PerfmonCounters pc
            WHERE pc.cntr_type <> 1073939712
            AND NOT EXISTS (SELECT 1 FROM dbo.Counters C
                            WHERE C.object_name = RTRIM(pc.object_name)
                              AND C.counter_name = RTRIM(pc.counter_name)
                              AND C.instance_name = RTRIM(pc.instance_name));

            INSERT INTO dbo.InstanceCounters (InstanceID, CounterID)
            SELECT @InstanceID, C.CounterID
            FROM @PerfmonCounters pc
            JOIN dbo.Counters C ON C.counter_name = pc.counter_name AND C.instance_name = pc.instance_name AND C.object_name = pc.object_name
            WHERE pc.cntr_type <> 1073939712
            EXCEPT
            SELECT InstanceID, CounterID FROM dbo.InstanceCounters WHERE InstanceID = @InstanceID;

            /* Backfill / repair the WMI identity on rows the INSERT didn't create: those that predate the
               WmiClass / WmiProperty columns (NULL), or any drift.  Matches what the EXCEPT guard above
               detects, so once resolved the guard stops firing.  The app keys off this identity rather than
               the (object_name, counter_name) display names. */
            UPDATE C
                SET C.WmiClass = pc.WmiClass, C.WmiProperty = pc.WmiProperty
            FROM dbo.Counters C
            JOIN @PerfmonCounters pc ON C.object_name = RTRIM(pc.object_name)
                                    AND C.counter_name = RTRIM(pc.counter_name)
                                    AND C.instance_name = RTRIM(pc.instance_name)
            WHERE pc.cntr_type <> 1073939712 AND pc.WmiClass IS NOT NULL
            AND (C.WmiClass IS NULL OR C.WmiProperty IS NULL OR C.WmiClass <> pc.WmiClass OR C.WmiProperty <> pc.WmiProperty);
        END
    COMMIT;
END

UPDATE IC
SET IC.UpdatedDate = @SnapshotDate
FROM dbo.InstanceCounters IC
WHERE IC.InstanceID = @InstanceID
AND EXISTS (SELECT 1 FROM @PerfmonCounters pc
            JOIN dbo.Counters C ON C.counter_name = pc.counter_name AND C.instance_name = pc.instance_name AND C.object_name = pc.object_name
            WHERE C.CounterID = IC.CounterID AND pc.cntr_type <> 1073939712);

/* --- Cook the values from the current sample (B) and the previous sample (A = Staging) --- */
DECLARE @PC TABLE (
    InstanceID     INT           NOT NULL,
    CounterID      INT           NOT NULL,
    SnapshotDate   DATETIME2 (2) NOT NULL,
    SnapshotDate60 DATETIME2 (2) NOT NULL,
    Value          DECIMAL (28, 9) NULL,
    PRIMARY KEY (InstanceID, CounterID, SnapshotDate)
);

INSERT INTO @PC (InstanceID, CounterID, SnapshotDate, SnapshotDate60, Value)
SELECT @InstanceID,
       C.CounterID,
       B.SnapshotDate,
       DG.DateGroup,
       CASE
           WHEN B.cntr_type = 65792
               THEN B.cntr_value
           WHEN B.cntr_type = 272696576 AND B.cntr_value >= A.cntr_value
               THEN (B.cntr_value - A.cntr_value) / NULLIF(DATEDIFF_BIG(ms, A.SnapshotDate, B.SnapshotDate) / 1000.0, 0)
           WHEN B.cntr_type = 537003264
               THEN B.cntr_value * 100.0 / NULLIF(Bbase.cntr_value, 0)
           WHEN B.cntr_type = 542180608 AND B.cntr_value >= A.cntr_value
               THEN 100.0 * (B.cntr_value - A.cntr_value) / NULLIF(B.timebase - A.timebase, 0)
           WHEN B.cntr_type = 558957824 AND B.cntr_value >= A.cntr_value
               THEN 100.0 * (1 - (B.cntr_value - A.cntr_value) / NULLIF(B.timebase - A.timebase, 0))
           WHEN B.cntr_type = 805438464 AND B.cntr_value >= A.cntr_value AND Bbase.cntr_value >= Abase.cntr_value
               THEN ((B.cntr_value - A.cntr_value) / 10000000.0) / NULLIF(Bbase.cntr_value - Abase.cntr_value, 0)
           ELSE NULL
       END AS Value
FROM @PerfmonCounters B
CROSS APPLY dbo.DateGroupingMins(B.SnapshotDate, 60) DG
JOIN dbo.Counters C ON C.counter_name = B.counter_name AND C.object_name = B.object_name AND C.instance_name = B.instance_name
/* previous sample of this counter */
LEFT JOIN Staging.PerfmonCounters A ON A.InstanceID = @InstanceID
                                    AND A.object_name = B.object_name AND A.counter_name = B.counter_name AND A.instance_name = B.instance_name
                                    AND A.cntr_type = B.cntr_type AND A.SnapshotDate < B.SnapshotDate
/* current base (fraction / average-timer) */
LEFT JOIN @PerfmonCounters Bbase ON Bbase.object_name = B.object_name AND Bbase.instance_name = B.instance_name
                                 AND Bbase.counter_name = RTRIM(B.counter_name) + ' Base' AND Bbase.cntr_type = 1073939712
/* previous base (average-timer) */
LEFT JOIN Staging.PerfmonCounters Abase ON Abase.InstanceID = @InstanceID
                                        AND Abase.object_name = B.object_name AND Abase.instance_name = B.instance_name
                                        AND Abase.counter_name = RTRIM(B.counter_name) + ' Base' AND Abase.cntr_type = 1073939712
                                        AND Abase.SnapshotDate < B.SnapshotDate
WHERE B.cntr_type IN (SELECT cntr_type FROM @MetricTypes)
AND NOT EXISTS (SELECT 1 FROM dbo.PerformanceCounters PC
                WHERE PC.SnapshotDate = CAST(B.SnapshotDate AS DATETIME2(2)) AND PC.InstanceID = @InstanceID AND PC.CounterID = C.CounterID)
AND (DATEDIFF(mi, A.SnapshotDate, B.SnapshotDate) < 1440 /* skip if the gap is more than a day */
     OR A.SnapshotDate IS NULL /* first sample: only produces a value for types that don't need a delta (65792 / fraction) */
    );

BEGIN TRAN;

INSERT INTO dbo.PerformanceCounters (InstanceID, CounterID, SnapshotDate, Value)
SELECT InstanceID, CounterID, SnapshotDate, Value FROM @PC WHERE Value IS NOT NULL;

/* 60 minute rollup (mirrors PerformanceCounters_Upd) */
WITH T AS (
    SELECT InstanceID, CounterID, SnapshotDate60,
           SUM(Value) AS Value_Total, MIN(Value) AS Value_Min, MAX(Value) AS Value_Max, COUNT(*) AS SampleCount
    FROM @PC WHERE Value IS NOT NULL
    GROUP BY InstanceID, CounterID, SnapshotDate60
)
UPDATE PC60
    SET PC60.Value_Total += T.Value_Total,
        PC60.Value_Min = CASE WHEN T.Value_Min < PC60.Value_Min THEN T.Value_Min ELSE PC60.Value_Min END,
        PC60.Value_Max = CASE WHEN T.Value_Max > PC60.Value_Max THEN T.Value_Max ELSE PC60.Value_Max END,
        PC60.SampleCount += T.SampleCount
FROM dbo.PerformanceCounters_60MIN PC60
JOIN T ON T.InstanceID = PC60.InstanceID AND T.CounterID = PC60.CounterID AND T.SnapshotDate60 = PC60.SnapshotDate;

INSERT INTO dbo.PerformanceCounters_60MIN (InstanceID, CounterID, SnapshotDate, Value_Total, Value_Min, Value_Max, SampleCount)
SELECT InstanceID, CounterID, SnapshotDate60, SUM(Value), MIN(Value), MAX(Value), COUNT(*)
FROM @PC PC
WHERE Value IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.PerformanceCounters_60MIN PC60
               WHERE PC60.InstanceID = PC.InstanceID AND PC60.CounterID = PC.CounterID AND PC60.SnapshotDate = PC.SnapshotDate60)
GROUP BY InstanceID, CounterID, SnapshotDate60;

/* Refresh this instance's previous-sample staging (own table, so a full delete is safe).
   Only rows a later sample needs as its "previous" are kept: rate / timer metrics and the base rows
   average-timer cooking reads (Abase).  Skipped as they never need a prior sample:
     - 65792     RAWCOUNT  - stored as-is
     - 537003264 fraction  - cooked from the current sample only (value*100/base)
   Fraction base rows share the base marker (1073939712) with average-timer bases and can't be told
   apart by type, so all base rows are retained - harmless. */
DELETE Staging.PerfmonCounters WHERE InstanceID = @InstanceID;

INSERT INTO Staging.PerfmonCounters (InstanceID, SnapshotDate, object_name, counter_name, instance_name, cntr_value, cntr_type, timebase)
SELECT @InstanceID, SnapshotDate, object_name, counter_name, instance_name, cntr_value, cntr_type, timebase
FROM @PerfmonCounters
WHERE cntr_type NOT IN (65792, 537003264);

COMMIT;

EXEC dbo.CollectionDates_Upd @InstanceID = @InstanceID, @Reference = @Ref, @SnapshotDate = @SnapshotDate;
