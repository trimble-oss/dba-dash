CREATE PROC XE.XETraceSession_GetXel(
    @XETraceSessionID BIGINT
)
AS
/* Returns the captured .xel bytes for a trace (event_file target, best-effort) so the GUI can offer a download.

   Ownership gate (mirrors XETraceSession_Get / XETraceSessionReport_Get so a direct/forged call can't download
   another user's capture): db_owner (admin) may read any session; everyone else only sessions they started, anchored
   on RequestedBy = SUSER_SNAME() (the login captured server-side at trace start).  Note this is defence in depth only
   - it does not protect against a role with direct SELECT on the underlying tables. */
DECLARE @IsAdmin BIT = IS_ROLEMEMBER('db_owner');
SELECT XelData
FROM XE.XETraceSession
WHERE XETraceSessionID = @XETraceSessionID
AND (@IsAdmin = 1 OR RequestedBy = SUSER_SNAME())
