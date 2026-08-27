CREATE PROC dbo.XETraceSession_Del(
    @XETraceSessionID BIGINT
)
AS
/* Soft-deletes an ad-hoc XE trace: the bulk captured data is removed (the event rows and the stored .xel) to reclaim
   space, but the session row is retained - flagged with DeletedDate/DeletedBy - so a minimal audit trail of who ran
   (and deleted) what survives.  Deleted sessions are hidden from the reports/history; the row is hard-deleted later by
   retention (dbo.PurgeXETrace) along with any other aged session.  Called from the Trace History report's Delete link.

   Ownership: db_owner (admin) may delete any trace; everyone else may only delete traces they started.  Both the admin
   test (IS_ROLEMEMBER) and the owner test (RequestedBy = SUSER_SNAME()) are evaluated server-side against the connected
   login, so neither can be spoofed by the caller.  DeletedBy is likewise captured server-side for the audit trail. */
SET NOCOUNT ON;
/* Guarantee the DELETE + UPDATE below are atomic: any runtime error aborts and rolls back the whole transaction rather
   than committing a partial change (events removed but the session still flagged live) or leaving the tran open. */
SET XACT_ABORT ON;

/* Non-admins may only delete traces started under their own login (RequestedBy, captured server-side at start). */
IF IS_ROLEMEMBER('db_owner') = 0
    AND NOT EXISTS(SELECT 1 FROM dbo.XETraceSession
                   WHERE XETraceSessionID = @XETraceSessionID
                   AND RequestedBy = SUSER_SNAME())
BEGIN
    RAISERROR('You can only delete your own traces.', 16, 1);
    RETURN;
END

/* Both statements form one unit of work: never leave the captured events removed while the session still looks live
   (data intact, not flagged deleted). */
BEGIN TRAN;

/* Remove the captured event data (there is no FK cascade from XETraceEvent - see dbo.XETraceEvent). */
DELETE FROM dbo.XETraceEvent
WHERE XETraceSessionID = @XETraceSessionID;

/* Retain the session row for audit, but clear the captured .xel and record the deletion.  TotalEvents is left intact
   on purpose - it's useful to know a deleted trace had captured e.g. 1M rows. */
UPDATE dbo.XETraceSession
    SET XelData = NULL,
        DeletedDate = SYSUTCDATETIME(),
        DeletedBy = SUSER_SNAME()
WHERE XETraceSessionID = @XETraceSessionID
AND DeletedDate IS NULL; /* idempotent - don't overwrite the original deletion audit on a repeat */

COMMIT;
