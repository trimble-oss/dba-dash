CREATE PROC XE.XETraceEvents_GetByRunGroup(
    @RunGroupID UNIQUEIDENTIFIER
)
AS
/* All captured events for every per-instance session of a single multi-instance (e.g. AG-wide) trace run, merged in
   time order.  Each event's source instance is already stored inside its Fields JSON (the "Instance" field written by
   the GUI for multi-instance runs), so the caller expands Fields exactly as for XETraceEvents_Get - no extra column.

   Ownership gate (mirrors XETraceSession_Get / XETraceSessionReport_Get so a direct/forged call can't read another
   user's captured query text): db_owner (admin) may read any run; everyone else only runs they started, anchored on
   RequestedBy = SUSER_SNAME() (the login captured server-side at trace start).  Note this is defence in depth only -
   it does not protect against a role with direct SELECT on the underlying tables. */
DECLARE @IsAdmin BIT = IS_ROLEMEMBER('db_owner');
SELECT  E.XETraceEventID,
        E.event_type,
        E.timestamp,
        E.Fields
FROM XE.XETraceEvent E
JOIN XE.XETraceSession S ON S.XETraceSessionID = E.XETraceSessionID
WHERE S.RunGroupID = @RunGroupID
AND (@IsAdmin = 1 OR S.RequestedBy = SUSER_SNAME())
ORDER BY E.timestamp, E.XETraceEventID
