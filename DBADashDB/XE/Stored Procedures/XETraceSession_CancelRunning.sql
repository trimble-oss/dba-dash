CREATE PROC XE.XETraceSession_CancelRunning(
    @InstanceID INT
)
AS
SET NOCOUNT ON
/* Marks any Running (Status = 0) trace(s) for the instance as Cancelled, releasing the one-running-per-instance
   unique index so a new trace can start.  Used by the GUI's "Cancel / Cleanup existing trace" action alongside
   XETraceStopMessage (which drops the abandoned session on the source instance). */
UPDATE XE.XETraceSession
    SET Status = 2, /* Cancelled */
        EndTime = SYSUTCDATETIME(),
        ErrorMessage = ISNULL(ErrorMessage, 'Cancelled / cleaned up by user.')
WHERE InstanceID = @InstanceID
AND Status = 0
