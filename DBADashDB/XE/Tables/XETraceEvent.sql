CREATE TABLE XE.XETraceEvent(
    XETraceEventID BIGINT IDENTITY(1,1) NOT NULL,
    XETraceSessionID BIGINT NOT NULL,
    event_type sysname NOT NULL,
    /* Event time (UTC).  Every XE event carries a non-nullable timestamp_utc (see sys.fn_xe_file_target_read_file),
       The table is partitioned by timestamp for efficient data removal. */
    timestamp DATETIME2(3) NOT NULL,
    /* The ad-hoc XE trace schema is not fixed - different events expose different fields - so each event's shredded
       fields are stored as a JSON object rather than fixed columns.  Query with JSON_VALUE / OPENJSON as needed. */
    Fields NVARCHAR(MAX) NULL,
    CONSTRAINT PK_XETraceEvent PRIMARY KEY CLUSTERED (XETraceSessionID ASC, XETraceEventID ASC, timestamp ASC)
        WITH (DATA_COMPRESSION = PAGE) ON [PS_XETraceEvent] (timestamp),
    CONSTRAINT FK_XETraceEvent_XETraceSession FOREIGN KEY (XETraceSessionID)
        REFERENCES XE.XETraceSession (XETraceSessionID)
) ON [PS_XETraceEvent] (timestamp);
