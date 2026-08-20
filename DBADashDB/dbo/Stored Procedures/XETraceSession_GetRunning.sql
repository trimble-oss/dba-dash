CREATE PROC dbo.XETraceSession_GetRunning(
    @InstanceIDs IDs READONLY
)
AS
/* Running traces for the given instances - used by the GUI to disable "Start trace" while one is in progress. */
SELECT  S.XETraceSessionID,
        S.InstanceID,
        I.InstanceGroupName,
        S.RequestedBy,
        S.EventTypes,
        S.TargetType,
        S.StartTime,
        S.MaxDurationSeconds,
        S.TotalEvents,
        S.MessageGroupID
FROM dbo.XETraceSession S
JOIN dbo.Instances I ON S.InstanceID = I.InstanceID
WHERE S.Status = 0
AND EXISTS(SELECT 1 FROM @InstanceIDs T WHERE T.ID = S.InstanceID)
ORDER BY S.StartTime DESC
