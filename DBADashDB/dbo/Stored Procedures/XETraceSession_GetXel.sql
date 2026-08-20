CREATE PROC dbo.XETraceSession_GetXel(
    @XETraceSessionID BIGINT
)
AS
/* Returns the captured .xel bytes for a trace (event_file target, best-effort) so the GUI can offer a download. */
SELECT XelData
FROM dbo.XETraceSession
WHERE XETraceSessionID = @XETraceSessionID
