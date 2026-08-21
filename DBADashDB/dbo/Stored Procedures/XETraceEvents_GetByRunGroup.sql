CREATE PROC dbo.XETraceEvents_GetByRunGroup(
    @RunGroupID UNIQUEIDENTIFIER
)
AS
/* All captured events for every per-instance session of a single multi-instance (e.g. AG-wide) trace run, merged in
   time order.  Each event's source instance is already stored inside its Fields JSON (the "Instance" field written by
   the GUI for multi-instance runs), so the caller expands Fields exactly as for XETraceEvents_Get - no extra column. */
SELECT  E.XETraceEventID,
        E.event_type,
        E.timestamp,
        E.Fields
FROM dbo.XETraceEvent E
JOIN dbo.XETraceSession S ON S.XETraceSessionID = E.XETraceSessionID
WHERE S.RunGroupID = @RunGroupID
ORDER BY E.timestamp, E.XETraceEventID
