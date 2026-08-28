CREATE PROC XE.XETraceSession_Notes_Upd(
    @XETraceSessionID BIGINT,
    @Notes NVARCHAR(1000)
)
AS
/* Sets or updates the free-text note on an ad-hoc XE trace.  Called from the Trace History report's editable Notes
   link so a trace can be annotated (or corrected) after it has run - e.g. "Capture for issue #1234".  A blank note is
   stored as NULL.

   Ownership mirrors XE.XETraceSession_Del: db_owner (admin) may edit any trace's note; everyone else may only edit
   notes on traces they started.  The owner test (RequestedBy = SUSER_SNAME()) is evaluated server-side against the
   connected login, so it can't be spoofed by the caller. */
SET NOCOUNT ON;

/* Non-admins may only edit notes on traces started under their own login (RequestedBy, captured server-side at start). */
IF IS_ROLEMEMBER('db_owner') = 0
    AND NOT EXISTS(SELECT 1 FROM XE.XETraceSession
                   WHERE XETraceSessionID = @XETraceSessionID
                   AND RequestedBy = SUSER_SNAME())
BEGIN
    RAISERROR('You can only edit notes on your own traces.', 16, 1);
    RETURN;
END

UPDATE XE.XETraceSession
    SET Notes = NULLIF(LTRIM(RTRIM(@Notes)), N'')
WHERE XETraceSessionID = @XETraceSessionID;
