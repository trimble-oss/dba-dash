CREATE TYPE dbo.LatchStats AS TABLE (
    latch_class NVARCHAR (60) NOT NULL,
    waiting_requests_count BIGINT NOT NULL,
    wait_time_ms BIGINT NOT NULL,
    max_wait_time_ms BIGINT NOT NULL
);
