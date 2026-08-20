CREATE PROC dbo.XETraceSession_AddEvents(
    @XETraceSessionID BIGINT,
    @Events dbo.XETraceEvents READONLY
)
AS
SET NOCOUNT ON

INSERT INTO dbo.XETraceEvent(XETraceSessionID, event_type, timestamp, Fields)
SELECT @XETraceSessionID, event_type, timestamp, Fields
FROM @Events

UPDATE dbo.XETraceSession
    SET TotalEvents = TotalEvents + (SELECT COUNT(*) FROM @Events)
WHERE XETraceSessionID = @XETraceSessionID
