CREATE PROC dbo.XETraceSession_Start(
    @InstanceID INT,
    @MessageGroupID UNIQUEIDENTIFIER,
    @EventTypes VARCHAR(MAX),
    @MaxDurationSeconds INT,
    @FiltersJson NVARCHAR(MAX) = NULL,
    @RunGroupID UNIQUEIDENTIFIER = NULL, /* set (same GUID) for every instance of a multi-instance run; NULL for single-instance */
    @XETraceSessionID BIGINT OUT
)
AS
SET NOCOUNT ON
/* Called by the GUI (which always has a repository connection - the collecting service may not, e.g. S3/SQS relay
   topology) to open a trace session, mirroring how plan forcing is logged.  TargetType / GeneratedDDL are resolved
   by the service and written back by XETraceSession_Upd (early for the DDL/target, on completion for the rest). */

/* Free any stale Running rows for this instance (a trace whose service stopped without completing) so a genuine
   new trace isn't blocked by the one-running-per-instance unique index.  Grace = duration + 5 min for cleanup. */
UPDATE dbo.XETraceSession
    SET Status = 3,
        EndTime = SYSUTCDATETIME(),
        ErrorMessage = ISNULL(ErrorMessage, 'Trace did not complete (service stopped or superseded).')
WHERE InstanceID = @InstanceID
AND Status = 0
AND StartTime < DATEADD(SECOND, -(MaxDurationSeconds + 300), GETUTCDATE())

BEGIN TRY
    /* RequestedBy is captured here from the connected login (SUSER_SNAME()) - the authoritative, non-forgeable owner
       used by the delete/history authorization checks - rather than trusting a value from the caller. */
    INSERT INTO dbo.XETraceSession(InstanceID, MessageGroupID, RequestedBy, EventTypes, MaxDurationSeconds,
        FiltersJson, RunGroupID, Status)
    VALUES(@InstanceID, @MessageGroupID, SUSER_SNAME(), @EventTypes, @MaxDurationSeconds, @FiltersJson, @RunGroupID, 0)

    SET @XETraceSessionID = SCOPE_IDENTITY()
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() IN (2601, 2627) /* unique index - a trace is already running for this instance */
        THROW 50000, 'A trace is already running on this instance.', 1;
    ELSE
        THROW;
END CATCH
