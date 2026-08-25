CREATE PROC dbo.XETraceSession_Upd(
    @XETraceSessionID BIGINT,
    @Status TINYINT = NULL, /* NULL = definition-only update (trace still running); else terminal: 1 Completed, 2 Cancelled, 3 Error */
    @TargetType TINYINT = NULL, /* resolved target echoed back by the service (1 event_file, 2 ring_buffer) */
    @GeneratedDDL NVARCHAR(MAX) = NULL, /* service-generated CREATE EVENT SESSION DDL */
    @XelData VARBINARY(MAX) = NULL, /* captured .xel bytes, known only at completion */
    @ErrorMessage NVARCHAR(MAX) = NULL
)
AS
SET NOCOUNT ON
/* Single update proc for a trace row after XETraceSession_Start opens it.  Two uses:
     - Definition update (@Status NULL): the service confirmed the session started, so record the DDL / resolved
       target straight away while the row is still Running - the completion below can be lost to the Status guard
       when Stop force-cancels the row first, and never arrives at all if the trace is abandoned.
     - Completion (@Status set): move the trace to a terminal state.
   Status only ever moves away from 0 (Running), and only when a terminal @Status is supplied - so a definition
   update never terminates the trace, and a completion arriving after the row was already force-cancelled (e.g. via
   XETraceSession_CancelRunning during a Stop/cleanup) can't overwrite that terminal status or its EndTime/ErrorMessage.
   The audit values (TargetType / GeneratedDDL / XelData) are set-once and only known now, so they're backfilled
   regardless of status; COALESCE keeps any value already present. */
UPDATE dbo.XETraceSession
    SET Status = CASE WHEN Status = 0 AND @Status IS NOT NULL THEN @Status ELSE Status END,
        EndTime = CASE WHEN Status = 0 AND @Status IS NOT NULL THEN SYSUTCDATETIME() ELSE EndTime END,
        ErrorMessage = CASE WHEN Status = 0 AND @Status IS NOT NULL THEN COALESCE(@ErrorMessage, ErrorMessage) ELSE ErrorMessage END,
        TargetType = COALESCE(@TargetType, TargetType),
        GeneratedDDL = COALESCE(@GeneratedDDL, GeneratedDDL),
        XelData = COALESCE(@XelData, XelData)
WHERE XETraceSessionID = @XETraceSessionID
