CREATE PROC XE.XETraceEvents_Get(
    @XETraceSessionID BIGINT
)
AS
/* All captured events for a trace session, in capture order (for the viewer).  Fields is a JSON object of the
   event-specific fields - the caller expands it (OPENJSON server-side, or parse client-side) for display.

   Ownership gate (mirrors XETraceSession_Get / XETraceSessionReport_Get so a direct/forged call can't read another
   user's captured query text): db_owner (admin) may read any session; everyone else only sessions they started,
   anchored on RequestedBy = SUSER_SNAME() (the login captured server-side at trace start).  Note this is defence in
   depth only - it does not protect against a role with direct SELECT on the underlying tables. */
DECLARE @IsAdmin BIT = IS_ROLEMEMBER('db_owner');
SELECT  E.XETraceEventID,
        E.event_type,
        E.timestamp,
        S.InstanceID,
        E.Fields
FROM XE.XETraceEvent E
JOIN XE.XETraceSession S ON S.XETraceSessionID = E.XETraceSessionID
WHERE E.XETraceSessionID = @XETraceSessionID
AND (@IsAdmin = 1 OR S.RequestedBy = SUSER_SNAME())
ORDER BY E.XETraceEventID
