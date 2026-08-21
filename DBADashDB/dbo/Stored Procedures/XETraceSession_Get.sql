CREATE PROC dbo.XETraceSession_Get(
    @InstanceIDs IDs READONLY,
    @Days INT = 7
)
AS
/* Trace history for the given instances (most recent first). */
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
ORDER BY S.StartTime DESC
