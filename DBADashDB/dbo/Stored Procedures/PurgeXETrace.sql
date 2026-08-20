CREATE PROC dbo.PurgeXETrace(
    @BatchSize INT = 100
)
AS
SET NOCOUNT ON

DECLARE @RetentionDays INT
SELECT @RetentionDays = ISNULL((SELECT RetentionDays
                                FROM dbo.DataRetention
                                WHERE TableName = 'XETraceSession' AND SchemaName = 'dbo'), 30)

/* Backstop for stale Running rows (service stopped without completing) - fail them so they can be purged and no
   longer block new traces.  XETraceSession_Start also does this per-instance at start time. */
UPDATE dbo.XETraceSession
    SET Status = 3,
        EndTime = SYSUTCDATETIME(),
        ErrorMessage = ISNULL(ErrorMessage, 'Trace did not complete (service stopped).')
WHERE Status = 0
AND StartTime < DATEADD(SECOND, -(MaxDurationSeconds + 300), GETUTCDATE())

/* Delete completed traces past retention.  XETraceEvent is partitioned by timestamp (event time) and the bulk of old event
   data is removed by metadata-only partition truncate/merge (dbo.PurgeData -> dbo.PartitionTable_Cleanup).  Because
   the FK no longer cascades (and a session past retention may still have events in a not-yet-truncated partition),
   delete any remaining events for the sessions being purged first, then delete the session rows. */
WHILE (1 = 1)
BEGIN
    DELETE TOP(@BatchSize) E
    FROM dbo.XETraceEvent E
    WHERE EXISTS(SELECT 1
                 FROM dbo.XETraceSession S
                 WHERE S.XETraceSessionID = E.XETraceSessionID
                 AND S.Status <> 0 /* never a running trace */
                 AND ISNULL(S.EndTime, S.StartTime) < DATEADD(DAY, -@RetentionDays, GETUTCDATE()))

    IF @@ROWCOUNT = 0
        BREAK
END

WHILE (1 = 1)
BEGIN
    DELETE TOP(@BatchSize)
    FROM dbo.XETraceSession
    WHERE Status <> 0 /* never a running trace */
    AND ISNULL(EndTime, StartTime) < DATEADD(DAY, -@RetentionDays, GETUTCDATE())
    AND NOT EXISTS(SELECT 1
                   FROM dbo.XETraceEvent E
                   WHERE E.XETraceSessionID = dbo.XETraceSession.XETraceSessionID)

    IF @@ROWCOUNT = 0
        BREAK
END
