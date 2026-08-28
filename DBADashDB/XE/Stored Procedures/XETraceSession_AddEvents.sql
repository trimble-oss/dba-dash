CREATE PROC XE.XETraceSession_AddEvents(
    @XETraceSessionID BIGINT,
    @Events XE.XETraceEvents READONLY
)
AS
SET NOCOUNT ON

INSERT INTO XE.XETraceEvent(XETraceSessionID, event_type, timestamp, Fields)
SELECT @XETraceSessionID, event_type, timestamp, Fields
FROM @Events

UPDATE XE.XETraceSession
    SET TotalEvents = TotalEvents + (SELECT COUNT(*) FROM @Events)
WHERE XETraceSessionID = @XETraceSessionID
