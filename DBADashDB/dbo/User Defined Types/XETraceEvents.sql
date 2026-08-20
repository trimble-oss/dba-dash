/* Batch of shredded events sent from the GUI.  The schema is not fixed - event-specific fields are carried in the
   Fields JSON object built by XETraceShredder, with event_type / timestamp promoted for indexing and display. */
CREATE TYPE dbo.XETraceEvents AS TABLE (
    event_type sysname NOT NULL,
    timestamp DATETIME2(3) NOT NULL,
    Fields NVARCHAR(MAX) NULL
);
