CREATE PROC dbo.XETraceSession_Get(
    @InstanceIDs IDs READONLY,
    @Days INT = 7
)
AS
/* Trace history for the given instances (most recent first) for the QuickXETrace history dropdown, which only ever
   shows the current user their own recent traces.  Scoping is anchored on RequestedBy = SUSER_SNAME() (the login
   captured server-side at trace start), so it always returns exactly the caller's own traces and can't be widened.
   The full cross-user (admin) view is XETraceSessionReport_Get. */
SELECT  S.XETraceSessionID,
        S.InstanceID,
        I.InstanceGroupName,
        S.RequestedBy,
        S.EventTypes,
        S.TargetType,
        S.Status,
        S.StartTime,
        S.EndTime,
        S.MaxDurationSeconds,
        S.TotalEvents,
        S.MessageGroupID,
        S.RunGroupID,
        S.ErrorMessage,
        CAST(CASE WHEN S.XelData IS NULL THEN 0 ELSE 1 END AS BIT) AS HasXel
FROM dbo.XETraceSession S
JOIN dbo.Instances I ON S.InstanceID = I.InstanceID
WHERE EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = S.InstanceID)
AND S.StartTime >= DATEADD(DAY, -@Days, SYSUTCDATETIME())
AND S.RequestedBy = SUSER_SNAME() /* own traces only, anchored on the server-captured login (not spoofable) */
AND S.DeletedDate IS NULL /* hide soft-deleted traces */
ORDER BY S.StartTime DESC
