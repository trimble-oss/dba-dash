CREATE PROC dbo.XETraceEvents_Get(
    @XETraceSessionID BIGINT
)
AS
/* All captured events for a trace session, in capture order (for the viewer).  Fields is a JSON object of the
   event-specific fields - the caller expands it (OPENJSON server-side, or parse client-side) for display. */
SELECT  XETraceEventID,
        event_type,
        timestamp,
        Fields
FROM dbo.XETraceEvent
WHERE XETraceSessionID = @XETraceSessionID
ORDER BY XETraceEventID
