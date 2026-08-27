CREATE PROC dbo.XETraceSessionReport_Get(
    @InstanceIDs IDs READONLY,
    @Days INT = 7,
    @AllUsers BIT = 0, /* 1 = show all users' traces (admin-only "All users" toggle); 0 = only the caller's own */
    @IncludeDeleted BIT = 0 /* 1 = also return soft-deleted traces (admin-only "Show deleted" toggle) */
)
AS
/* Ad-hoc XE trace history for the given instances (most recent first).  Backs the "Trace History" system report
   (XETraceSessionsView).  Access to the proc is gated by the AdhocXE role.

   Server-side enforcement (the report UI mirrors it, but it is applied here so a direct/forged call can't bypass it):
   db_owner (admin) may see every requester (@AllUsers = 1) and toggle @IncludeDeleted; everyone else is restricted to the
   traces they started and never sees soft-deleted rows.  "Own" is anchored on RequestedBy = SUSER_SNAME() (the login
   captured server-side at trace start), and @AllUsers/@IncludeDeleted are forced off for non-admins - so a non-admin
   cannot widen their view however they call the proc. */
DECLARE @IsAdmin BIT = IS_ROLEMEMBER('db_owner');
IF @IsAdmin = 0 /* non-admins: own, non-deleted traces only, whatever they pass */
BEGIN
    SET @AllUsers = 0;
    SET @IncludeDeleted = 0;
END

SELECT  S.XETraceSessionID,
        S.InstanceID,
        I.InstanceGroupName,
        /* For a multi-instance run (shared RunGroupID) list every instance traced together, so the row makes clear it
           was part of an AG-wide / multi-instance trace and which replicas participated.  NULL for a single run. */
        CASE WHEN S.RunGroupID IS NULL THEN NULL
             ELSE STUFF((SELECT ', ' + I2.InstanceGroupName
                         FROM dbo.XETraceSession S2
                         JOIN dbo.Instances I2 ON S2.InstanceID = I2.InstanceID
                         WHERE S2.RunGroupID = S.RunGroupID
                         AND (@IncludeDeleted = 1 OR S2.DeletedDate IS NULL) /* match the row visibility of the outer query */
                         ORDER BY I2.InstanceGroupName
                         FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
             END AS RunInstances,
        S.RequestedBy,
        S.EventTypes,
        S.StartTime,
        S.EndTime,
        S.MaxDurationSeconds,
        S.TotalEvents,
        CASE S.Status WHEN 0 THEN 'Running'
                      WHEN 1 THEN 'Completed'
                      WHEN 2 THEN 'Cancelled'
                      WHEN 3 THEN 'Error'
                      ELSE 'Unknown' END AS StatusDescription,
        /* DBADashStatusEnum: 1 Critical, 2 Warning, 3 NA, 4 OK, 7 Information */
        CASE S.Status WHEN 0 THEN 7  /* Running   -> Information */
                      WHEN 1 THEN 4  /* Completed -> OK          */
                      WHEN 2 THEN 3  /* Cancelled -> NA          */
                      WHEN 3 THEN 1  /* Error     -> Critical    */
                      ELSE 3 END AS StatusColor,
        CASE S.TargetType WHEN 1 THEN 'Event File'
                          WHEN 2 THEN 'Ring Buffer'
                          ELSE '' END AS TargetTypeDescription,
        S.ErrorMessage,
        S.DeletedDate,
        S.DeletedBy,
        S.RunGroupID,
        /* Placeholder so the report's DDL link always opens a code window with feedback rather than appearing to do
           nothing (no DDL is recorded for older traces, or ones that never reached the "running" reply). */
        ISNULL(S.GeneratedDDL, '-- No DDL was recorded for this trace.') AS GeneratedDDL,
        /* Link text for the .xel download - only rows that captured a file get a clickable link (NULL renders blank). */
        CASE WHEN S.XelData IS NULL THEN NULL ELSE 'Download .xel' END AS Xel
FROM dbo.XETraceSession S
JOIN dbo.Instances I ON S.InstanceID = I.InstanceID
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = S.InstanceID)
AND S.StartTime >= DATEADD(DAY, -@Days, SYSUTCDATETIME())
AND (@IncludeDeleted = 1 OR S.DeletedDate IS NULL) /* hide soft-deleted traces unless the admin toggle is on */
/* Ownership: with @AllUsers set (admins only) show every requester; otherwise restrict to traces started under the
   caller's own login.  @AllUsers is already forced to 0 for non-admins above. */
AND (@AllUsers = 1 OR S.RequestedBy = SUSER_SNAME())
ORDER BY S.StartTime DESC
